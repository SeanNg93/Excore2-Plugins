# WaystoneAlchemy Plugin for ExileCore2

A plugin for Path of Exile that automates various currency operations on Waystones.

## Features

- **Auto-Alchemy**: Apply Alchemy Orbs to Normal Waystones
- **Auto-Regal**: Apply Regal Orbs to Magic Waystones (optional)
- **Auto-Exalt**: Apply Exalted Orbs to Rare Waystones (optional)
- **Auto-Corrupt**: Corrupt Rare Waystones (optional)
- **Distilled Paranoia**: Apply Distilled Paranoia to Rare Waystones (optional)
- **Dynamic UI Element Detection**: Automatically detects stash, inventory, vendor UI, and item positions

## Installation

1. Copy this folder to your ExileCore2 Plugins directory
2. Enable the plugin in the ExileCore2 interface

## Hotkeys

Configure these in the plugin settings:

- **Alchemy Hotkey** (default: F3): Alchemize waystones in your inventory
- **Paranoia Hotkey** (default: F4): Apply Distilled Paranoia to rare waystones
- **Corrupt Hotkey** (default: F5): Corrupt waystones in your inventory
- **Emergency Stop** (default: Pause): Immediately stop all operations

## Settings

- **Use Regal on Magic Waystones**: Toggle automatic application of Regal Orbs
- **Use Exalted on Rare Waystones**: Toggle automatic application of Exalted Orbs
- **Corrupt Rare Waystones**: Toggle automatic corruption of waystones
- **Enable Distilled Paranoia on Rare Waystones**: Toggle automatic application of Distilled Paranoia
- **Debug Mode**: Show detailed position information for UI elements
- **Extra Delay**: Adjust time between operations for stability

## Dynamic UI Detection

The plugin now automatically detects:
- Stash and inventory status
- Stash tab names and positions
- Item positions in inventory and stash
- Vendor UI positions for Distilled Paranoia application
- Resulting items from vendor operations

This means that the plugin will work regardless of your UI scale or screen resolution, and does not rely on hardcoded positions anymore.

## How to Use

1. Open your stash
2. Have the appropriate currency in your inventory
3. Place Waystones in your inventory
4. Press the configured hotkeys to perform the desired operations

## Requirements

- ExileCore2
- Path of Exile (latest version)
- Appropriate currency items in inventory

## Changes

- **1.1.0**: Added dynamic UI element detection, removed hardcoded positions
- **1.0.0**: Initial release

## Contributors

- Original plugin author
- Updated UI detection by Claude AI
