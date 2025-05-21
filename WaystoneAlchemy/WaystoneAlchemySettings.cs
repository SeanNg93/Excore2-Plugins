﻿using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using System.Windows.Forms;

namespace WaystoneAlchemy
{
    public class WaystoneAlchemySettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(false);

        [Menu("Use Regal on Magic Waystones")]
        public ToggleNode UseRegalOnMagicWaystones { get; set; } = new ToggleNode(false);

        [Menu("Use Exalted on Rare Waystones")]
        public ToggleNode ApplyExaltedOrbsToRareWaystone { get; set; } = new ToggleNode(false);

        [Menu("Corrupt Rare Waystones")]
        public ToggleNode CorruptRareWaystone { get; set; } = new ToggleNode(false);

        [Menu("Enable Distilled Paranoia on Rare Waystones")]
        public ToggleNode EnableParanoiaOnRareWaystones { get; set; } = new ToggleNode(false);

        [Menu("Hotkey for Alchemy Waystone")]
        public HotkeyNode AlchemyHotkey { get; set; } = new HotkeyNode(Keys.F3);

        [Menu("Hotkey for Distilled Paranoia")]
        public HotkeyNode ParanoiaHotkey { get; set; } = new HotkeyNode(Keys.F4);

        [Menu("Hotkey for Corrupt Waystones")]
        public HotkeyNode CorruptHotkey { get; set; } = new HotkeyNode(Keys.F5);

        [Menu("Emergency Stop Hotkey")]
        public HotkeyNode EmergencyStopHotkey { get; set; } = new HotkeyNode(Keys.Pause);
        
        [Menu("Debug Mode")]
        public ToggleNode DebugMode { get; set; } = new ToggleNode(false);
        
        [Menu("Extra Delay", "Delay between actions in milliseconds")]
        public RangeNode<int> ExtraDelay { get; set; } = new RangeNode<int>(50, 0, 500);
    }
}