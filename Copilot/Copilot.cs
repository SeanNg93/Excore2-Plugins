using System;
using System.Linq;
using System.Drawing;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;
using ImGuiNET;

using ExileCore2;
using ExileCore2.PoEMemory;
using ExileCore2.PoEMemory.Elements;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Enums;
using System.Collections.Generic;

// TODO: ghost follow
// - debug to see like a crosshair
// TODO: circle people
// TODO: pots
// TODO: Threads - this will be a big one

namespace Copilot
{
    public class Copilot : BaseSettingsPlugin<CopilotSettings>
    {
        private Entity _followTarget;
        private DateTime _nextAllowedActionTime = DateTime.Now; // Cooldown timer
        private DateTime _nextAllowedBlinkTime = DateTime.Now;
        private DateTime _nextAllowedShockTime = DateTime.Now;
        private Vector3 lastTargetPosition = Vector3.Zero;

        private IngameUIElements IngameUi => GameController.IngameState.IngameUi;
        private Element UIRoot => GameController.IngameState.UIRoot;
        private Camera Camera => GameController.Game.IngameState.Camera;
        private AreaInstance CurrentArea => GameController.Area.CurrentArea;
        private List<Entity> EntityList => GameController.EntityListWrapper.OnlyValidEntities;
        private Vector3 PlayerPos => GameController.Player.Pos;

        public override bool Initialise()
        {
            // Initialize plugin
            Name = "Copilot";
            lastTargetPosition = Vector3.Zero;
            return base.Initialise();
        }

        public override void DrawSettings()
        {
            try {
                if (ImGui.Button("Get Party List")) GetPartyList();

                ImGui.SameLine();
                ImGui.TextDisabled("(?)");
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Get the list of party members");

                // draw the party list
                ImGui.Text("Party List:");
                var i = 0;
                foreach (var playerName in Settings.PartyElements) {
                    if (string.IsNullOrEmpty(playerName)) continue;
                    if (i > 0) ImGui.SameLine();
                    i++;
                    if (ImGui.Button("Set " + playerName + " as target"))
                        Settings.TargetPlayerName.Value = playerName;
                }
                if (i == 0) ImGui.Text("No party members found");
            }
            catch (Exception) { /* Handle exceptions silently */ }
            base.DrawSettings();
        }

        public override void AreaChange(AreaInstance area)
        {
            lastTargetPosition = Vector3.Zero;
            _followTarget = null;
            base.AreaChange(area);
        }

        public override void Render()
        {
            if (!GameController.Window.IsForeground()) return;

            // Handle pause/unpause toggle
            if (Settings.TogglePauseHotkey.PressedOnce()) Settings.IsPaused.Value = !Settings.IsPaused.Value;

            // If paused, disabled, or not ready for the next action, do nothing
            if (!Settings.Enable.Value || Settings.IsPaused.Value || DateTime.Now < _nextAllowedActionTime || GameController.Player == null || GameController.IsLoading) return;

            try
            {
                // Check if there are any UI elements blocking the player
                var checkpoint = UIRoot.Children?[1]?.Children?[64];
                var market = UIRoot.Children?[1]?.Children?[27];
                var leftPanel = IngameUi.OpenLeftPanel;
                var rightPanel = IngameUi.OpenRightPanel;
                var worldMap = IngameUi.WorldMap;
                var npcDialog = IngameUi.NpcDialog;
                if ((checkpoint?.IsVisible != null && (bool)checkpoint?.IsVisible) ||
                    (leftPanel?.IsVisible != null && (bool)leftPanel?.IsVisible) ||
                    (rightPanel?.IsVisible != null && (bool)rightPanel?.IsVisible) ||
                    (worldMap?.IsVisible != null && (bool)worldMap?.IsVisible) ||
                    (npcDialog?.IsVisible != null && (bool)npcDialog?.IsVisible) ||
                    (market?.IsVisible != null && (bool)market?.IsVisible))
                { 
                    Keyboard.KeyPress(Keys.Space);
                    _nextAllowedActionTime = DateTime.Now.AddMilliseconds(Settings.ActionCooldown.Value);
                    return;
                }

                var resurrectPanel = IngameUi.ResurrectPanel;
                if (resurrectPanel != null && resurrectPanel.IsVisible) {
                    var inTown = resurrectPanel?.ResurrectInTown;
                    var atCheckpoint = resurrectPanel?.ResurrectAtCheckpoint;
                    var btn = atCheckpoint ?? inTown; // if inTown is null, use atCheckpoint
                    if (btn != null && btn.IsVisible) {
                        var screenPoint = new Point((int)btn.GetClientRectCache.Center.X, (int)btn.GetClientRectCache.Center.Y);
                        Mouse.LeftClick(screenPoint, 300);
                    }
                    _nextAllowedActionTime = DateTime.Now.AddMilliseconds(1000);
                    return;
                }

                FollowTarget();
            } 
            catch (Exception) { /* Handle exceptions silently */ }
        }

        private void FollowTarget()
        {
            try
            {
                _followTarget = GetFollowingTarget();
                var leaderPE = GetLeaderPartyElement();

                // If the target is not found, or the player is not in the same zone
                if (_followTarget == null) {
                    if (!leaderPE.ZoneName.Equals(CurrentArea.DisplayName))
                        FollowUsingPortalOrTpButton(leaderPE);
                    return;
                }
                if (CurrentArea.IsTown) return;

                var targetPos = _followTarget.Pos;
                if (lastTargetPosition == Vector3.Zero) lastTargetPosition = targetPos;
                var distanceToTarget = Vector3.Distance(PlayerPos, targetPos);

                //* Shock Bot
                if (Settings.ShockBot.Enable.Value && DateTime.Now > _nextAllowedShockTime && ShockBotCode()) return;

                //* Pickup
                if (Settings.Pickup.Enable.Value && distanceToTarget <= Settings.Pickup.RangeToIgnore.Value && PickUpItem()) return;

                // If within the follow distance, do nothing
                if (distanceToTarget <= Settings.FollowDistance.Value) return;

                // Quick check for "bugged" (shouldn't be open) confirmation tp
                if (leaderPE?.TpButton != null && GetTpConfirmation() != null) Keyboard.KeyPress(Keys.Escape);

                // check if there is areatransition in the area and boss
                // var thereIsBossNear = GameController.Entities.Any(e => e.Type == EntityType.Monster && e.IsAlive && e.Rarity == MonsterRarity.Unique && Vector3.Distance(myPos, e.Pos) < 2000);

                // check if the distance of the target changed significantly from the last position OR if there is a boss near and the distance is less than 2000
                if (distanceToTarget > 3000 /* || (thereIsBossNear && distanceToTarget < 2000) */) // TODO: fix this arena
                {
                    ClickBestPortal();
                    _nextAllowedActionTime = DateTime.Now.AddMilliseconds(1000);
                    return;
                }
                else if (Settings.Blink.Enable.Value && DateTime.Now > _nextAllowedBlinkTime && distanceToTarget > Settings.Blink.Range.Value)
                {
                    MoveToward(targetPos);
                    Thread.Sleep(50);
                    Keyboard.KeyPress(Keys.Space);
                    _nextAllowedBlinkTime = DateTime.Now.AddMilliseconds(Settings.Blink.Cooldown.Value);
                }
                else
                {
                    MoveToward(targetPos);
                }

                // Set the cooldown for the next allowed action
                _nextAllowedActionTime = DateTime.Now.AddMilliseconds(Settings.ActionCooldown.Value);
            }
            catch (Exception) { /* Handle exceptions silently */ }
        }

        private void FollowUsingPortalOrTpButton(PartyElementWindow leaderPE)
        {
            try
            {
                var portal = GetBestPortalLabel();
                const int threshold = 1000;
                var distanceToPortal = portal != null ? Vector3.Distance(PlayerPos, portal.ItemOnGround.Pos) : threshold + 1;
                if (
                    (CurrentArea.IsHideout ||
                        (CurrentArea.Name.Equals("The Temple of Chaos") && leaderPE.ZoneName.Equals("The Trial of Chaos")
                    )) && distanceToPortal <= threshold)
                { // if in hideout and near the portal
                    var screenPos = Camera.WorldToScreen(portal.ItemOnGround.Pos);
                    var screenPoint = new Point((int)screenPos.X, (int)screenPos.Y);
                    Mouse.LeftClick(screenPoint, 500);
                    if (leaderPE?.TpButton != null && GetTpConfirmation() != null) Keyboard.KeyPress(Keys.Escape);
                }
                else if (leaderPE?.TpButton != null)
                {
                    var screenPoint = GetTpButton(leaderPE);
                    Mouse.LeftClick(screenPoint, 100);

                    if (leaderPE.TpButton != null)
                    { // check if the tp confirmation is open
                        var tpConfirmation = GetTpConfirmation();
                        if (tpConfirmation != null)
                        {
                            screenPoint = new Point((int)tpConfirmation.GetClientRectCache.Center.X, (int)tpConfirmation.GetClientRectCache.Center.Y);
                            Mouse.LeftClick(screenPoint, 100);
                        }
                    }
                }
                _nextAllowedActionTime = DateTime.Now.AddMilliseconds(500);
            }
            catch (Exception) { /* Handle exceptions silently */ }
        }

        private bool ShockBotCode()
        {
            var monster = EntityList
                .Where(e => e.Type == EntityType.Monster && e.IsAlive && (e.Rarity == MonsterRarity.Rare || e.Rarity == MonsterRarity.Unique))
                .OrderBy(e => Vector3.Distance(PlayerPos, e.Pos))
                .FirstOrDefault();
            if (monster != null)
            {
                var distanceToMonster = Vector3.Distance(PlayerPos, monster.Pos);
                if (distanceToMonster <= Settings.ShockBot.Range)
                {
                    var screenPos = Camera.WorldToScreen(monster.Pos);
                    var screenPoint = new Point((int)screenPos.X, (int)screenPos.Y);
                    Mouse.SetCursorPosition(screenPoint);
                    Thread.Sleep(100);

                    Keyboard.KeyPress(Settings.ShockBot.BallLightningKey.Value);

                    // start tracking the balls
                    var ball = EntityList
                        .Where(e => e.IsDead && e.Metadata == "Metadata/Projectiles/BallLightningPlayer")
                        .OrderBy(e => Vector3.Distance(monster.Pos, e.Pos))
                        .FirstOrDefault();

                    if (ball != null && Vector3.Distance(monster.Pos, ball.Pos) <= Settings.ShockBot.RangeToUseLightningWarp.Value)
                    {
                        var ballScreenPos = Camera.WorldToScreen(ball.Pos);
                        var ballScreenPoint = new Point((int)ballScreenPos.X, (int)ballScreenPos.Y);
                        Mouse.SetCursorPosition(ballScreenPoint);
                        Thread.Sleep(100);
                        Keyboard.KeyPress(Settings.ShockBot.LightningWarpKey.Value);
                    }

                    _nextAllowedActionTime = DateTime.Now.AddMilliseconds(Settings.ActionCooldown.Value);
                    _nextAllowedShockTime = DateTime.Now.AddMilliseconds(Settings.ShockBot.ActionCooldown.Value);
                    return true;
                }
            }
            _nextAllowedShockTime = DateTime.Now.AddMilliseconds(Settings.ShockBot.ActionCooldown.Value);
            return false;
        }

        private bool PickUpItem()
        {
            var pos = Settings.Pickup.UseTargetPosition.Value ? _followTarget.Pos : PlayerPos;
            try
            {
                var items = IngameUi.ItemsOnGroundLabelsVisible;
                if (items != null)
                {
                    var filteredItems = Settings.Pickup.Filter.Value.Split(',');
                    var item = items?
                        .OrderBy(x => Vector3.Distance(pos, x.ItemOnGround.Pos))
                        .FirstOrDefault(x => filteredItems.Any(y => x.Label.Text != null && x.Label.Text.Contains(y)));
                    if (item == null) return false;

                    var distanceToItem = Vector3.Distance(pos, item.ItemOnGround.Pos);
                    if (distanceToItem <= Settings.Pickup.Range.Value)
                    {
                        var screenPos = Camera.WorldToScreen(item.ItemOnGround.Pos);
                        var screenPoint = new Point((int)screenPos.X, (int)screenPos.Y);
                        Mouse.LeftClick(screenPoint, 50);
                        _nextAllowedActionTime = DateTime.Now.AddMilliseconds(Settings.ActionCooldown.Value);
                        return true;
                    }
                }
            }
            catch (Exception) { /* Handle exceptions silently */ }
            return false;
        }

        private Entity GetFollowingTarget()
        {
            try
            {
                var leaderName = Settings.TargetPlayerName.Value.ToLower();
                var target = GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Player].FirstOrDefault(x => string.Equals(x.GetComponent<Player>()?.PlayerName.ToLower(), leaderName, StringComparison.OrdinalIgnoreCase));
                return target;
            }
            catch (Exception e)
            {
                LogError(e.Message);
                return null;
            }
        }

        private PartyElementWindow GetLeaderPartyElement()
        {
            try
            {
                var partyElementList = IngameUi.PartyElement.Children?[0]?.Children;
                var leader = partyElementList?.FirstOrDefault(partyElement => partyElement?.Children?[0]?.Children?[0]?.Text?.ToLower() == Settings.TargetPlayerName.Value.ToLower());
                var leaderPartyElement = new PartyElementWindow
                {
                    PlayerName = leader.Children?[0]?.Children?[0]?.Text,
                    TpButton = leader?.Children?[4],
                    ZoneName = leader.Children[3].Text ?? CurrentArea.DisplayName
                };
                return leaderPartyElement;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void GetPartyList()
        {
            // Settings
            var partyElements = new string[5];
            try
            {
                var partyElementList = IngameUi.PartyElement.Children?[0]?.Children;
                var i = 0;
                foreach (var partyElement in partyElementList)
                {
                    var playerName = partyElement?.Children?[0]?.Children?[0]?.Text;
                    partyElements[i] = playerName;
                    i++;
                }
            }
            catch (Exception) { /* Handle exceptions silently */ }

            Settings.PartyElements = partyElements;
        }

        private LabelOnGround GetBestPortalLabel()
        {
            try
            {
                var portalLabels =
                    IngameUi.ItemsOnGroundLabelsVisible?.Where(
                            x => x.ItemOnGround.Metadata.ToLower().Contains("areatransition")
                                || x.ItemOnGround.Metadata.ToLower().Contains("portal")
                                || x.ItemOnGround.Metadata.ToLower().EndsWith("ultimatumentrance")
                            )
                        .OrderBy(x => Vector3.Distance(lastTargetPosition, x.ItemOnGround.Pos)).ToList();

                var random = new Random();

                return CurrentArea?.IsHideout != null && CurrentArea.IsHideout
                    ? portalLabels?[random.Next(portalLabels.Count)]
                    : portalLabels?.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private void ClickBestPortal()
        {
            try
            {
                var portal = GetBestPortalLabel();
                if (portal != null)
                {
                    var screenPos = Camera.WorldToScreen(portal.ItemOnGround.Pos);
                    var screenPoint = new Point((int)screenPos.X, (int)screenPos.Y);
                    Mouse.LeftClick(screenPoint, 300);
                }
            }
            catch (Exception) { /* Handle exceptions silently */ }
        }

        private Point GetTpButton(PartyElementWindow leaderPE)
        {
            try
            {
                var windowOffset = GameController.Window.GetWindowRectangle().TopLeft;
                var elemCenter = (Vector2) leaderPE?.TpButton?.GetClientRectCache.Center;
                var finalPos = new Point((int) (elemCenter.X + windowOffset.X), (int) (elemCenter.Y + windowOffset.Y));

                return finalPos;
            }
            catch
            {
                return Point.Empty;
            }
        }

        private Element GetTpConfirmation()
        {
            try
            {
                var ui = IngameUi.PopUpWindow.Children[0].Children[0];

                if (ui.Children[0].Text.Equals("Are you sure you want to teleport to this player's location?"))
                    return ui.Children[3].Children[0];

                return null;
            }
            catch
            {
                return null;
            }
        }

        private void MoveToward(Vector3 targetPos)
        {
            try
            {
                var screenPos = Camera.WorldToScreen(targetPos);
                var screenPoint = new Point((int)screenPos.X, (int)screenPos.Y);

                Mouse.SetCursorPosition(screenPoint);
                if (Settings.Additional.UseMouse.Value)
                    Mouse.LeftClick(screenPoint, 20);
                else
                    Keyboard.KeyPress(Keys.T);
                lastTargetPosition = targetPos;
            }
            catch (Exception) { /* Handle exceptions silently */ }
        }
    }
}

public class PartyElementWindow
{
    public string PlayerName { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public Element TpButton { get; set; } = new Element();

    public override string ToString()
    {
        string not = TpButton != null ? TpButton.Text : "not ";
        return $"{PlayerName}, current zone: {ZoneName}, and does {not}have tp button";
    }
}