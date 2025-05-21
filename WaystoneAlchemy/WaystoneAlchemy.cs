using ExileCore2;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.Shared.Enums;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Windows.Forms;
using System.Numerics;
using System.Drawing;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.Elements.InventoryElements;
using System.Collections.Generic;


// ..

namespace WaystoneAlchemy
{
    public class WaystoneAlchemyPlugin : BaseSettingsPlugin<WaystoneAlchemySettings>
    {
        private volatile bool _shouldStop = false;
        private const Keys ActivationKey = Keys.F2;
        private bool _previousKeyState;
        private bool _prevParanoiaHotkey;
        private bool _prevParanoiaHotkeyState;
        private bool _previousAlchemyHotkey;
        private bool _prevCorruptHotkey;
        private bool _prevCorruptHotkeyState;
        private Vector2 _clickWindowOffset;
        private int _visibleStashIndex = -1;
        private List<string> _stashTabNames = new List<string>();


        public override bool Initialise()
        {
            LogMessage("WaystoneAlchemy Plugin Initialized Successfully.", 3);
            // Initialize the window offset
            _clickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;
            
            if (Settings.DebugMode.Value)
                LogMessage("Item click position detection updated to use window offset for accuracy.", 5);
                
            return true;
        }

        public override void AreaChange(AreaInstance area)
        {
            // Reset the emergency stop flag when changing areas
            _shouldStop = false;
            
            // Update stash tab information when entering a hideout or town
            if (area.IsHideout || area.IsTown)
            {
                if (Settings.DebugMode.Value)
                    LogMessage("Entering hideout/town, updating stash information", 5);
                    
                UpdateStashTabNames();
            }
        }

        public override void Tick()
        {
            // Check if the emergency stop hotkey is pressed
            if (EmergencyStopHotkeyPressed())
            {
                // If the emergency stop is activated, exit early
                return;
            }
            
            // Update window offset on each tick
            _clickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;
            
            // Only continue if stash and inventory are both visible
            if (!StashingRequirementsMet())
                return;
                
            var currentlyPressed = Input.IsKeyDown(Settings.AlchemyHotkey.Value);
            if (currentlyPressed && !_previousKeyState)
            {
                ProcessAlchemyOnWaystones();

                if (Settings.ApplyExaltedOrbsToRareWaystone)
                {
                    ApplyExaltedOrbsToRareWaystone();
                }
                // return;
            }
            _previousKeyState = currentlyPressed;
            
            // Distilled Paranoia logic explicitly added clearly:
            var paranoiaPressed = Input.IsKeyDown(Settings.ParanoiaHotkey.Value);
            if (paranoiaPressed && !_prevParanoiaHotkeyState && Settings.EnableParanoiaOnRareWaystones && !_shouldStop)
            {
                ApplyDistilledParanoiaClearly();
            }
            _prevParanoiaHotkeyState = paranoiaPressed;

            var corruptPressed = Input.IsKeyDown(Settings.CorruptHotkey.Value);
            if (corruptPressed && !_previousKeyState && Settings.CorruptRareWaystone)
            {
                CorruptWaystones();
            }

            _prevCorruptHotkeyState = corruptPressed;

            return;
        }
        
        // Similar to Stashie's StashingRequirementsMet
        private bool StashingRequirementsMet()
        {
            return GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible &&
                   GameController.Game.IngameState.IngameUi.StashElement.IsVisibleLocal;
        }
        
        // Get the current visible stash tab index
        private int GetIndexOfCurrentVisibleTab()
        {
            return GameController.Game.IngameState.IngameUi.StashElement.IndexVisibleStash;
        }
        
        // Get the names of all stash tabs
        private void UpdateStashTabNames()
        {
            try
            {
                var stashElement = GameController.Game.IngameState.IngameUi.StashElement;
                if (stashElement != null)
                {
                    var names = stashElement.AllStashNames;
                    _stashTabNames = new List<string>(names);
                    
                    if (Settings.DebugMode.Value)
                        LogMessage($"Found {_stashTabNames.Count} stash tabs", 5);
                }
            }
            catch (System.Exception e)
            {
                LogError($"Error updating stash tab names: {e.Message}");
            }
        }
        
        // Get stash tab name by index
        private string GetStashTabName(int index)
        {
            if (_stashTabNames.Count == 0)
                UpdateStashTabNames();
                
            if (index >= 0 && index < _stashTabNames.Count)
                return _stashTabNames[index];
                
            return "Unknown";
        }

        private IList<NormalInventoryItem> GetVisibleInventoryItems()
        {
            return GameController.Game.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems;
        }
        
        private IList<NormalInventoryItem> GetVisibleStashItems()
        {
            _visibleStashIndex = GetIndexOfCurrentVisibleTab();
            if (_visibleStashIndex < 0)
                return new List<NormalInventoryItem>();
                
            try
            {
                var stashItems = GameController.Game.IngameState.IngameUi.StashElement.VisibleStash?.VisibleInventoryItems;
                return stashItems ?? new List<NormalInventoryItem>();
            }
            catch
            {
                return new List<NormalInventoryItem>();
            }
        }
        
        private async Task<bool> Delay(int ms = 0)
        {
            await Task.Delay(Settings.ExtraDelay.Value + ms);
            return true;
        }
        
        // Utility method to identify UI position via debug mode
        private void LogUIPosition(string elementName, Vector2 position)
        {
            if (Settings.DebugMode.Value)
                LogMessage($"{elementName} position: {position}", 5);
        }


        private bool EmergencyStopHotkeyPressed()
        {
            if (Input.IsKeyDown(Settings.EmergencyStopHotkey.Value))
            {
                DebugWindow.LogMsg("🚫 Emergency Stop activated.", 3, Color.Orange);
                _shouldStop = true;
            }
            return _shouldStop;
        }

        private NormalInventoryItem GetCurrencyItem(string currencyPath)
        {
            var inventoryItems = GetVisibleInventoryItems();
            return inventoryItems
                .Where(x => x.Item.Path.Contains(currencyPath) && x.Item.HasComponent<Stack>())
                .FirstOrDefault(x => x.Item.GetComponent<Stack>().Size > 0);
        }

        private bool UseCurrencyOnItem(NormalInventoryItem currency, NormalInventoryItem target)
        {
            // Get the center position of the currency item in screen coordinates
            var currencyPos = currency.GetClientRect().Center + _clickWindowOffset;
            
            if (Settings.DebugMode.Value)
                LogMessage($"Clicking currency at position: {currencyPos}", 5);
            
            Input.SetCursorPos(currencyPos);
            Thread.Sleep(100);
            Input.Click(MouseButtons.Right);
            Thread.Sleep(150);

            // Get the center position of the target item in screen coordinates
            var targetPos = target.GetClientRect().Center + _clickWindowOffset;
            
            if (Settings.DebugMode.Value)
                LogMessage($"Applying to target at position: {targetPos}", 5);
            
            Input.SetCursorPos(targetPos);
            Thread.Sleep(100);
            Input.Click(MouseButtons.Left);
            Thread.Sleep(250);
            return true;
        }

        private bool IdentifyItem(NormalInventoryItem item)
        {
            var mods = item.Item.GetComponent<Mods>();
            if (mods == null || mods.Identified) return true; // already identified

            var wisdom = GetCurrencyItem("CurrencyIdentification");
            if (wisdom == null)
            {
                DebugWindow.LogMsg("No Wisdom scroll found!", 2, Color.Red);
                return false;
            }

            return UseCurrencyOnItem(wisdom, item);
        }

        private bool UseAlchemy(NormalInventoryItem item)
        {
            var alch = GetCurrencyItem("CurrencyUpgradeToRare");
            if (alch == null)
            {
                DebugWindow.LogMsg("No Alchemy Orb found!", 2, Color.Red);
                return false;
            }
            return UseCurrencyOnItem(alch, item);
        }

        private bool UseRegal(NormalInventoryItem item)
        {
            var regal = GetCurrencyItem("CurrencyUpgradeMagicToRare");
            if (regal == null)
            {
                DebugWindow.LogMsg("No Regal Orb found!", 2, Color.Red);
                return false;
            }
            return UseCurrencyOnItem(regal, item);
        }

        private void HandleWaystone(NormalInventoryItem waystone)
        {
            var mods = waystone.Item.GetComponent<Mods>();
            if (mods == null) return;

            switch (mods.ItemRarity)
            {
                case ItemRarity.Normal:
                    UseAlchemy(waystone);
                    break;

                case ItemRarity.Magic:
                    if (!Settings.UseRegalOnMagicWaystones) 
                        break;
                    
                    if (!mods.Identified)
                    {
                        if (!IdentifyItem(waystone))
                            return; // failed to identify.
                        Thread.Sleep(250);
                    }

                    UseRegal(waystone);
                    break;

                default:
                    break;
            }
        }



        private void ProcessAlchemyOnWaystones()
        {
            var inventoryItems = GetVisibleInventoryItems();
            var waystones = inventoryItems.Where(x => x.Item.GetComponent<Base>()?.Name.Contains("Waystone") ?? false).ToList();

            if (!waystones.Any())
            {
                DebugWindow.LogMsg("No Waystones found in inventory.", 2, Color.Yellow);
                return;
            }

            foreach (var waystone in waystones)
                HandleWaystone(waystone);
        }

        // -------------------------------------------------------------------------------------------------------------------------------------------------------
        // Applying distilled items on waystones...

        private void ApplyDistilledParanoiaClearly()
        {
            var items = GetVisibleInventoryItems();

            var paranoia = items.FirstOrDefault(x => x.Item.GetComponent<Base>()?.Name == "Distilled Paranoia" && x.Item.GetComponent<Stack>()?.Size >= 3);
            if (paranoia == null)
            {
                DebugWindow.LogMsg("❌ No sufficient Distilled Paranoia found.", 3, Color.Red);
                return;
            }

            var rareWaystones = items.Where(x =>
                x.Item.GetComponent<Base>()?.Name.Contains("Waystone") == true &&
                x.Item.GetComponent<Mods>()?.ItemRarity == ItemRarity.Rare).ToList();

            if (!rareWaystones.Any())
            {
                DebugWindow.LogMsg("No rare Waystones found in inventory.", 2, Color.Yellow);
                return;
            }

            foreach (var waystone in rareWaystones)
            {
                if (Settings.DebugMode.Value)
                    LogMessage($"Applying Distilled Paranoia to Waystone at {waystone.GetClientRect().Center}", 5);

                UseItemRightClick(paranoia); Thread.Sleep(500);

                // explicitly transfer 3 Paranoias
                for (int i = 0; i < 3; i++)
                {
                    CtrlClickItem(paranoia); Thread.Sleep(250);
                }

                // explicitly add rare Waystone to distilled UI
                CtrlClickItem(waystone); Thread.Sleep(250);

                ClickInstillButton(); Thread.Sleep(1000);

                CtrlClickResultingWaystoneFromDistillUI(); Thread.Sleep(500);
            }
        }

        private void UseItemRightClick(NormalInventoryItem item)
        {
            // Get the center position of the item in screen coordinates
            var itemPos = item.GetClientRect().Center + _clickWindowOffset;
            
            if (Settings.DebugMode.Value)
                LogMessage($"Right-clicking item at position: {itemPos}", 5);
            
            Input.SetCursorPos(itemPos);
            Thread.Sleep(100);
            Input.Click(MouseButtons.Right);
            Thread.Sleep(100);
        }

        private void CtrlClickItem(NormalInventoryItem item)
        {
            // Get the center position of the item in screen coordinates
            var itemPos = item.GetClientRect().Center + _clickWindowOffset;
            
            if (Settings.DebugMode.Value)
                LogMessage($"Ctrl+clicking item at position: {itemPos}", 5);

            Input.KeyDown(Keys.ControlKey);
            Thread.Sleep(60);
            Input.SetCursorPos(itemPos);
            Thread.Sleep(100);
            Input.Click(MouseButtons.Left);
            Thread.Sleep(80);
            Input.KeyUp(Keys.ControlKey);
            Thread.Sleep(60);
        }

        private void ClickInstillButton()
        {
            try
            {
                // In ExileCore2, we need to access the trade UI
                var tradeUi = GameController.Game.IngameState.IngameUi.TradeWindow;
                if (tradeUi == null || !tradeUi.IsVisible)
                {
                    LogError("Trade UI is not visible");
                    return;
                }
                
                // Try to find the accept button in the trade UI
                var acceptButton = tradeUi.AcceptButton;
                if (acceptButton != null && acceptButton.IsVisible)
                {
                    var buttonRect = acceptButton.GetClientRect();
                    Vector2 buttonPos = buttonRect.Center + _clickWindowOffset; // Add window offset
                    
                    if (Settings.DebugMode.Value)
                        LogMessage($"Found accept button at: {buttonPos}", 5);
                    
                    Input.SetCursorPos(buttonPos);
                    Thread.Sleep(80);
                    Input.Click(MouseButtons.Left);
                    Thread.Sleep(100);
                    return;
                }
                
                // Fallback to using a calculated position if we couldn't find the button
                var windowRectangle = GameController.Window.GetWindowRectangle();
                // Button is typically near the bottom of the trade UI
                var instillButtonPos = windowRectangle.TopLeft + new Vector2(windowRectangle.Width * 0.5f, windowRectangle.Height * 0.75f);
                
                if (Settings.DebugMode.Value)
                    LogMessage($"Using fallback position for accept button: {instillButtonPos}", 5);
                
                Input.SetCursorPos(instillButtonPos);
                Thread.Sleep(80);
                Input.Click(MouseButtons.Left);
                Thread.Sleep(100);
            }
            catch (System.Exception e)
            {
                LogError($"Error clicking accept button: {e.Message}");
            }
        }

        private void CtrlClickResultingWaystoneFromDistillUI()
        {
            try
            {
                // In ExileCore2, we need to access the trade UI
                var tradeUi = GameController.Game.IngameState.IngameUi.TradeWindow;
                if (tradeUi == null || !tradeUi.IsVisible)
                {
                    LogError("Trade UI is not visible");
                    return;
                }
                
                // Try to find the item in the trade's item slots - first check YourOffer
                var tradeItems = tradeUi.YourOffer;
                NormalInventoryItem waystoneItem = null;
                
                if (tradeItems != null && tradeItems.Count > 0)
                {
                    waystoneItem = tradeItems.FirstOrDefault(item => 
                        item?.Item?.GetComponent<Base>()?.Name?.Contains("Waystone") == true);
                }
                
                // If not found in YourOffer, try TheOtherOffer 
                if (waystoneItem == null)
                {
                    var otherItems = tradeUi.OtherOffer;
                    if (otherItems != null && otherItems.Count > 0)
                    {
                        waystoneItem = otherItems.FirstOrDefault(item => 
                            item?.Item?.GetComponent<Base>()?.Name?.Contains("Waystone") == true);
                    }
                }
                
                if (waystoneItem != null)
                {
                    var itemRect = waystoneItem.GetClientRect();
                    Vector2 itemPos = itemRect.Center + _clickWindowOffset; // Add window offset
                    
                    if (Settings.DebugMode.Value)
                        LogMessage($"Found Waystone in trade UI at: {itemPos}", 5);
                    
                    Input.KeyDown(Keys.ControlKey);
                    Thread.Sleep(100);
                    Input.SetCursorPos(itemPos);
                    Thread.Sleep(80);
                    Input.Click(MouseButtons.Left);
                    Thread.Sleep(80);
                    Input.KeyUp(Keys.ControlKey);
                    Thread.Sleep(60);
                    return;
                }
                
                // Fallback to using a calculated position if we couldn't find the item
                var windowRectangle = GameController.Window.GetWindowRectangle();
                // Check whether we're looking at the left or right side of the trade window
                
                // For Distilled item distillation, the result should be on the right side (OtherOffer area)
                var uiWaystonePos = windowRectangle.TopLeft + new Vector2(windowRectangle.Width * 0.75f, windowRectangle.Height * 0.5f);
                
                if (Settings.DebugMode.Value)
                    LogMessage($"Using fallback position for waystone in trade UI: {uiWaystonePos}", 5);
                
                Input.KeyDown(Keys.ControlKey);
                Thread.Sleep(100);
                Input.SetCursorPos(uiWaystonePos);
                Thread.Sleep(80);
                Input.Click(MouseButtons.Left);
                Thread.Sleep(80);
                Input.KeyUp(Keys.ControlKey);
                Thread.Sleep(60);
            }
            catch (System.Exception e)
            {
                LogError($"Error retrieving waystone from trade UI: {e.Message}");
            }
        }


        // Corrupting Waystones

        private void CorruptWaystones()
        {
            var inventoryItems = GetVisibleInventoryItems();
            var waystones = inventoryItems.Where(x => x.Item.GetComponent<Base>()?.Name.Contains("Waystone") ?? false).ToList();
            
            if (!waystones.Any())
            {
                DebugWindow.LogMsg("No Waystones found in inventory.", 2, Color.Yellow);
                return;
            }
            
            var corruptionOrb = GetCurrencyItem("CurrencyCorrupt");
            if (corruptionOrb == null)
            {
                DebugWindow.LogMsg("No Corruption Orb found!", 2, Color.Red);
                return;
            }
            
            foreach (var waystone in waystones)
            {
                if (Settings.DebugMode.Value)
                    LogMessage($"Corrupting Waystone at {waystone.GetClientRect().Center}", 5);
                    
                UseCurrencyOnItem(corruptionOrb, waystone);
                Thread.Sleep(250); // Adjust delay as needed
            }
        }

        private void ApplyExaltedOrbsToRareWaystone()
        {
            if (!Settings.ApplyExaltedOrbsToRareWaystone) // Ensure the toggle is enabled
                return;
                
            var inventoryItems = GetVisibleInventoryItems();
            var rareWaystones = inventoryItems.Where(x =>
                x.Item.GetComponent<Base>()?.Name.Contains("Waystone") == true &&
                x.Item.GetComponent<Mods>()?.ItemRarity == ItemRarity.Rare).ToList();
                
            if (!rareWaystones.Any())
            {
                DebugWindow.LogMsg("No rare Waystones found in inventory.", 2, Color.Yellow);
                return;
            }
            
            var exaltedOrb = GetCurrencyItem("CurrencyAddModToRare");
            if (exaltedOrb == null || exaltedOrb.Item.GetComponent<Stack>().Size < 3)
            {
                DebugWindow.LogMsg("Not enough Exalted Orbs found!", 2, Color.Red);
                return;
            }
            
            foreach (var waystone in rareWaystones)
            {
                var mods = waystone.Item.GetComponent<Mods>();
                if (mods != null && !mods.Identified) // Check if the Waystone is identified
                {
                    if (!IdentifyItem(waystone)) // Attempt to identify the Waystone
                    {
                        DebugWindow.LogMsg("Failed to identify Waystone.", 2, Color.Red);
                        continue; // Skip this Waystone if identification fails
                    }
                    Thread.Sleep(250); // Add a small delay after identification
                }
                
                if (Settings.DebugMode.Value)
                    LogMessage($"Applying Exalted Orbs to Waystone at {waystone.GetClientRect().Center}", 5);
                    
                for (int i = 0; i < 3; i++) // Apply 3 Exalted Orbs
                {
                    UseCurrencyOnItem(exaltedOrb, waystone);
                    Thread.Sleep(250); // Adjust delay as needed
                }
            }
        }


    }
}