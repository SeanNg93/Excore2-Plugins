using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AutoMyAim.Structs;
using ExileCore2;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared;
using ExileCore2.Shared.Nodes;
#if DEBUG || true
using ImGuiNET;
using System.Windows.Forms;
using System.Diagnostics;
#endif

namespace AutoMyAim;

public class AutoMyAim : BaseSettingsPlugin<AutoMyAimSettings>
{
    public static AutoMyAim Main;
    private readonly ClusterManager _clusterManager;
    private readonly EntityScanner _entityScanner;
    private readonly InputHandler _inputHandler;
    internal readonly RayCaster RayCaster;
    private readonly AimRenderer _renderer;
    private readonly TargetWeightCalculator _weightCalculator;
    private TrackedEntity _currentTarget;
    private HotkeyManager _hotkeyManager;
    private Func<bool> _pickitIsActive;
    public Vector2 TopLeftScreen;
    public RectangleF GetWindowRectangleNormalized;

#if DEBUG || true
    private string _toggleMessage = null;
    private double _toggleMessageUntil = 0;
    private string _walkableTerrainToggleMessage = null;
    private double _walkableTerrainToggleMessageUntil = 0;
#endif

    public AutoMyAim()
    {
        Name = "Auto My Aim";
        RayCaster = new RayCaster();
        _clusterManager = new ClusterManager();
        _weightCalculator = new TargetWeightCalculator();
        _entityScanner = new EntityScanner(_weightCalculator, _clusterManager);
        _inputHandler = new InputHandler();
        _renderer = new AimRenderer(_clusterManager);
    }

    public override bool Initialise()
    {
        Main = this;
        if (Settings.AimKeys == null)
            Settings.AimKeys = new List<HotkeyNode>();
        if (Settings.AimToggleKeys == null)
            Settings.AimToggleKeys = new List<HotkeyNode>();
        if (Settings.AimKeys.Count == 0)
            Settings.AimKeys.Add(new HotkeyNode(Keys.None));
        if (Settings.AimToggleKeys.Count == 0)
            Settings.AimToggleKeys.Add(new HotkeyNode(Keys.None));
        _hotkeyManager = new HotkeyManager(Settings.AimKeys, Settings.AimToggleKeys);
        _hotkeyManager.OnToggleStateChanged += isToggled => {
#if DEBUG || true
            _toggleMessage = isToggled ? "Auto Aim ON" : "Auto Aim OFF";
            _toggleMessageUntil = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0) + 1.5;
#endif
            Debug.WriteLine(isToggled ? "Auto Aim ON" : "Auto Aim OFF");
        };
        Settings.UseWalkableTerrainInsteadOfTargetTerrain.OnValueChanged += (_, _) => { RayCaster.UpdateArea(); };
        Input.RegisterKey(Settings.ToggleWalkableTerrainHotkey);
        return true;
    }

    public override void AreaChange(AreaInstance area)
    {
        RayCaster.UpdateArea();
        _entityScanner.ClearEntities();
        _clusterManager.ClearRenderState();
        _currentTarget = null;
        if (Settings.ResetToggleOnAreaChange) _hotkeyManager.ResetToggleState();
        _pickitIsActive = GameController.PluginBridge.GetMethod<Func<bool>>("PickIt.IsActive");
    }

    public override void Tick()
    {
        if (Settings.ToggleWalkableTerrainHotkey.PressedOnce())
        {
            // Check conditions before toggling
            var gameUi = GameController.IngameState.IngameUi;
            var isChatOpen = gameUi?.ChatTitlePanel?.IsVisible ?? false; // Adjusted to check ChatTitlePanel
            var isWindowFocused = GameController.Window.IsForeground();
            var isInHideout = GameController.Area.CurrentArea.IsHideout;

            if (!isChatOpen && isWindowFocused && !isInHideout)
            {
                Settings.UseWalkableTerrainInsteadOfTargetTerrain.Value = !Settings.UseWalkableTerrainInsteadOfTargetTerrain.Value;
#if DEBUG || true
                _walkableTerrainToggleMessage = Settings.UseWalkableTerrainInsteadOfTargetTerrain.Value ? "Walkable Terrain ON" : "Walkable Terrain OFF";
                _walkableTerrainToggleMessageUntil = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0) + 1.5;
#endif
                Debug.WriteLine(Settings.UseWalkableTerrainInsteadOfTargetTerrain.Value ? "Walkable Terrain ON" : "Walkable Terrain OFF");
            }
        }

        _hotkeyManager.Update();
        _hotkeyManager.Update();
        TopLeftScreen = GameController.Window.GetWindowRectangleTimeCache.TopLeft;
        var windowRect = GameController.Window.GetWindowRectangleReal();
        GetWindowRectangleNormalized = new RectangleF(
            windowRect.X - TopLeftScreen.X,
            windowRect.Y - TopLeftScreen.Y,
            windowRect.Width,
            windowRect.Height);
        if (!ShouldProcess()) return;
        if (!_hotkeyManager.IsToggled && !_hotkeyManager.IsManualAimActive()) return;
        var player = GameController?.Player;
        if (player == null) return;
        ProcessAiming(player);
    }

    private void ProcessAiming(Entity player)
    {
        var currentPos = player.GridPos;

        var potentialTargets = _entityScanner.ScanForInRangeEntities(currentPos, GameController);

        RayCaster.UpdateObserver(currentPos, potentialTargets);

        _entityScanner.ProcessVisibleEntities(currentPos);
        _entityScanner.UpdateEntityWeights(currentPos);

        var sortedEntities = _entityScanner.GetTrackedEntities();
        if (!sortedEntities.Any()) return;

        var (targetEntity, rawPosToAim) = GetTargetEntityAndPosition(sortedEntities);
        if (targetEntity == null) return;

        _currentTarget = targetEntity;
        if (Settings.UsePluginBridgeWarnings)
        {
            if (_pickitIsActive?.Invoke() ?? false)
                return;
        }
        UpdateCursorPosition(rawPosToAim);
    }

    private (TrackedEntity entity, Vector2 position) GetTargetEntityAndPosition(List<TrackedEntity> sortedEntities)
    {
        if (!Settings.Targeting.PointToOffscreenTargetsOtherwiseFindNextTargetInBounds)
        {
            foreach (var entity in sortedEntities)
            {
                var pos = GameController.IngameState.Camera.WorldToScreen(entity.Entity.Pos);
                if (pos != Vector2.Zero &&
                    _inputHandler.IsValidClickPosition(pos, GetWindowRectangleNormalized))
                    return (entity, pos);
            }

            return (null, Vector2.Zero);
        }

        var targetEntity = sortedEntities.First();
        var rawPosToAim = GameController.IngameState.Camera.WorldToScreen(targetEntity.Entity.Pos);
        return rawPosToAim == Vector2.Zero ? (null, Vector2.Zero) : (targetEntity, rawPosToAim);
    }

    private void UpdateCursorPosition(Vector2 rawPosToAim)
    {
        if (_currentTarget == null) return;
        var safePosToAim = _inputHandler.GetSafeAimPosition(rawPosToAim, GetWindowRectangleNormalized);

        if (Settings.Render.Cursor.ConfineCursorToCircle)
        {
            var playerScreenPos = GameController.IngameState.Camera.WorldToScreen(GameController.Player.Pos);
            var circleRadius = Settings.Render.Cursor.CursorCircleRadius;

            var toTarget = safePosToAim - playerScreenPos;
            var distance = toTarget.Length();

            if (distance > circleRadius) safePosToAim = playerScreenPos + Vector2.Normalize(toTarget) * circleRadius;
        }

        if (_inputHandler.IsValidClickPosition(safePosToAim, GetWindowRectangleNormalized))
        {
            var randomizedPos = _inputHandler.GetRandomizedAimPosition(safePosToAim, GetWindowRectangleNormalized);
            if (_inputHandler.IsValidClickPosition(randomizedPos, GetWindowRectangleNormalized))
                Input.SetCursorPos(randomizedPos + TopLeftScreen);
        }
    }

    public override void Render()
    {
        _renderer.Render(GameController, _currentTarget, _entityScanner.GetTrackedEntities());
#if DEBUG || true
        if (!string.IsNullOrEmpty(_toggleMessage) && (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0) < _toggleMessageUntil)
        {
            ImGui.SetNextWindowBgAlpha(0.5f); 
            var io = ImGui.GetIO();
            var windowWidth = io.DisplaySize.X;
            var textWidth = ImGui.CalcTextSize(_toggleMessage).X;
            var pos = new Vector2((windowWidth - textWidth) / 2, 50);
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
            ImGui.Begin("##AutoAimToggleMsg", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
            ImGui.PushStyleColor(ImGuiCol.Text, _hotkeyManager.IsToggled ? new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1));
            ImGui.Text(_toggleMessage);
            ImGui.PopStyleColor();
            ImGui.End();
        }

        if (!string.IsNullOrEmpty(_walkableTerrainToggleMessage) && (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0) < _walkableTerrainToggleMessageUntil)
        {
            ImGui.SetNextWindowBgAlpha(0.5f); 
            var io = ImGui.GetIO();
            var windowWidth = io.DisplaySize.X;
            var textWidth = ImGui.CalcTextSize(_walkableTerrainToggleMessage).X;
            var pos = new Vector2((windowWidth - textWidth) / 2, 80); // Position below the other message
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
            ImGui.Begin("##WalkableTerrainToggleMsg", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
            ImGui.PushStyleColor(ImGuiCol.Text, Settings.UseWalkableTerrainInsteadOfTargetTerrain.Value ? new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1));
            ImGui.Text(_walkableTerrainToggleMessage);
            ImGui.PopStyleColor();
            ImGui.End();
        }
#endif
    }

    private bool ShouldProcess()
    {
        if (!Settings.Enable) return false;
        if (GameController is not { InGame: true, Player: not null }) return false;
        return !GameController.Settings.CoreSettings.Enable &&
               AreUiElementsVisible(GameController?.IngameState.IngameUi);
    }

    private bool AreUiElementsVisible(IngameUIElements ingameUi)
    {
        if (ingameUi == null) return false;
        if (!Settings.Render.Panels.RenderAndWorkOnFullPanels &&
            ingameUi.FullscreenPanels.Any(x => x.IsVisible))
            return false;
        if (!Settings.Render.Panels.RenderAndWorkOnleftPanels && ingameUi.OpenLeftPanel.IsVisible)
            return false;
        return Settings.Render.Panels.RenderAndWorkOnRightPanels || !ingameUi.OpenRightPanel.IsVisible;
    }

    public override void DrawSettings()
    {
#if DEBUG || true
        _hotkeyManager.RenderHotkeySettings();
#endif
        base.DrawSettings();
    }

#if DEBUG || true
    private int _editingHotkeyIndex = -1; // Helper for single hotkey editing
    private string _currentEditingHotkeyId = null;

    private void DrawHotkeySetting(HotkeyNode hotkeyNode, string uniqueId)
    {
        ImGui.PushID(uniqueId);
        string btnLabel = (_currentEditingHotkeyId == uniqueId && _editingHotkeyIndex == 0) ? "[Press a key or mouse button]" : hotkeyNode.Value.ToString();
        if (ImGui.Button(btnLabel))
        {
            _currentEditingHotkeyId = uniqueId;
            _editingHotkeyIndex = 0;
        }

        if (_currentEditingHotkeyId == uniqueId && _editingHotkeyIndex == 0)
        {
            ImGui.Text("Press any key or mouse button to assign, or ESC to clear.");
            bool assigned = false;

            // Prioritize keyboard input
            foreach (ImGuiKey imguiKey in Enum.GetValues(typeof(ImGuiKey)))
            {
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(imguiKey))) // Use GetKeyIndex for more reliable key detection
                {
                    var winKey = HotkeyManager.ImGuiKeyToWinFormsKey(imguiKey);
                    if (winKey == Keys.Escape)
                    {
                        hotkeyNode.Value = Keys.None;
                        assigned = true;
                    }
                    else if (winKey != Keys.None)
                    {
                        hotkeyNode.Value = winKey;
                        assigned = true;
                    }
                    if (assigned) break;
                }
            }

            // Process mouse input if no keyboard key was pressed
            if (!assigned)
            {
                for (int mouseBtn = 0; mouseBtn < 5; mouseBtn++)
                {
                    if (ImGui.IsMouseClicked((ImGuiMouseButton)mouseBtn))
                    {
                        Keys mouseKey = Keys.None;
                        switch (mouseBtn)
                        {
                            case 0: mouseKey = Keys.LButton; break;
                            case 1: mouseKey = Keys.RButton; break;
                            case 2: mouseKey = Keys.MButton; break;
                            case 3: mouseKey = Keys.XButton1; break;
                            case 4: mouseKey = Keys.XButton2; break;
                        }

                        if (mouseKey != Keys.None)
                        {
                            hotkeyNode.Value = mouseKey;
                            assigned = true;
                        }
                        if (assigned) break;
                    }
                }
            }

            if (assigned)
            {
                _editingHotkeyIndex = -1;
                _currentEditingHotkeyId = null;
                Input.RegisterKey(hotkeyNode);
            }
        }
        ImGui.PopID();
    }
#endif
}