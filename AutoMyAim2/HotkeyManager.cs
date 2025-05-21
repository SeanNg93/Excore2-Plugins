using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ExileCore2;
using ExileCore2.Shared.Nodes;
using ImGuiNET;

namespace AutoMyAim
{
    public class HotkeyManager
    {
        private readonly List<HotkeyNode> _manualAimKeys;
        private readonly List<HotkeyNode> _toggleAimKeys;
        
        private int _editingManualKeyIndex = -1;
        private int _editingToggleKeyIndex = -1;
        
        public event Action<bool> OnToggleStateChanged;
        private bool _isToggled;
        
        public bool IsToggled => _isToggled;
        
        public HotkeyManager(List<HotkeyNode> manualAimKeys, List<HotkeyNode> toggleAimKeys)
        {
            _manualAimKeys = manualAimKeys;
            _toggleAimKeys = toggleAimKeys;
            
            // Register all keys
            RegisterAllKeys();
        }
        
        public void RegisterAllKeys()
        {
            foreach (var key in _manualAimKeys)
            {
                Input.RegisterKey(key);
            }
            
            foreach (var key in _toggleAimKeys)
            {
                Input.RegisterKey(key);
            }
        }
        
        public bool IsManualAimActive()
        {
            return _manualAimKeys.Exists(k => Input.GetKeyState(k.Value));
        }
        
        public void Update()
        {
            // Check for toggle key presses
            foreach (var key in _toggleAimKeys)
            {
                if (key.PressedOnce())
                {
                    _isToggled = !_isToggled;
                    OnToggleStateChanged?.Invoke(_isToggled);
                    break;
                }
            }
        }
        
        public void ResetToggleState()
        {
            if (_isToggled)
            {
                _isToggled = false;
                OnToggleStateChanged?.Invoke(false);
            }
        }
        
        public void RenderHotkeySettings()
        {
            RenderKeyList("Manual Aim Keys:", _manualAimKeys, ref _editingManualKeyIndex, ref _editingToggleKeyIndex);
            ImGui.Spacing();
            RenderKeyList("Toggle Aim Keys:", _toggleAimKeys, ref _editingToggleKeyIndex, ref _editingManualKeyIndex);
            ImGui.Spacing();
            ImGui.Separator();
        }
        
        private void RenderKeyList(string title, List<HotkeyNode> keys, ref int editingIndex, ref int otherEditingIndex)
        {
            ImGui.Text(title);
            
            for (int i = 0; i < keys.Count; i++)
            {
                ImGui.PushID($"{title}_{i}");
                ImGui.Text($"Key {i + 1}: ");
                ImGui.SameLine();
                
                string btnLabel = editingIndex == i ? "[Press a key or mouse button]" : keys[i].Value.ToString();
                if (ImGui.Button(btnLabel))
                {
                    editingIndex = i;
                    otherEditingIndex = -1;
                }
                
                ImGui.SameLine();
                if (ImGui.Button("X"))
                {
                    if (i >= 0 && i < keys.Count && keys.Count > 1)
                    {
                        keys.RemoveAt(i);
                        editingIndex = -1;
                    }
                    ImGui.PopID();
                    return;
                }
                ImGui.PopID();
            }
            
            if (ImGui.Button($"+ Add {(title.Contains("Manual") ? "Manual" : "Toggle")} Key"))
            {
                keys.Add(new HotkeyNode(Keys.None));
            }
            
            if (editingIndex >= 0 && editingIndex < keys.Count)
            {
                HandleKeyAssignment(keys, editingIndex, ref editingIndex);
            }
        }
        
        private void HandleKeyAssignment(List<HotkeyNode> keys, int index, ref int editingIndex)
        {
            ImGui.Text("Press any key or mouse button to assign, or ESC to clear.");
            bool assigned = false;
            
            // Check keyboard keys
            foreach (ImGuiKey imguiKey in Enum.GetValues(typeof(ImGuiKey)))
            {
                if (ImGui.IsKeyPressed(imguiKey))
                {
                    var winKey = ImGuiKeyToWinFormsKey(imguiKey);
                    if (winKey == Keys.Escape)
                    {
                        keys[index].Value = Keys.None;
                        assigned = true;
                    }
                    else if (winKey != Keys.None)
                    {
                        keys[index].Value = winKey;
                        assigned = true;
                    }
                    break;
                }
            }
            
            // Check mouse buttons
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
                            keys[index].Value = mouseKey;
                            assigned = true;
                        }
                        break;
                    }
                }
            }
            
            if (assigned)
            {
                editingIndex = -1;
                // Re-register the key
                Input.RegisterKey(keys[index]);
            }
        }
        
        public static Keys ImGuiKeyToWinFormsKey(ImGuiKey key)
        {
            if (key >= ImGuiKey.A && key <= ImGuiKey.Z)
                return Keys.A + (key - ImGuiKey.A);
            if (key >= ImGuiKey._0 && key <= ImGuiKey._9)
                return Keys.D0 + (key - ImGuiKey._0);
            if (key >= ImGuiKey.F1 && key <= ImGuiKey.F12)
                return Keys.F1 + (key - ImGuiKey.F1);
            if (key == ImGuiKey.Space) return Keys.Space;
            if (key == ImGuiKey.Enter) return Keys.Enter;
            if (key == ImGuiKey.Escape) return Keys.Escape;
            if (key == ImGuiKey.Tab) return Keys.Tab;
            if (key == ImGuiKey.LeftCtrl) return Keys.LControlKey;
            if (key == ImGuiKey.RightCtrl) return Keys.RControlKey;
            if (key == ImGuiKey.LeftShift) return Keys.LShiftKey;
            if (key == ImGuiKey.RightShift) return Keys.RShiftKey;
            if (key == ImGuiKey.LeftAlt) return Keys.LMenu;
            if (key == ImGuiKey.RightAlt) return Keys.RMenu;
            return Keys.None;
        }
    }
}