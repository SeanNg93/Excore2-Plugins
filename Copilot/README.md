# Copilot
This plugin is a fork of the original [copilot](https://github.com/totalschaden/copilot), updated to work with PoE2.

Although this plugin originates from the original, it has been rewritten with changes to its logic handling.
Additionally, some features from the original have not been implemented and are not planned for future updates.

It also incorporates some base code from [FollowLad](https://github.com/AlphaCaster/FollowLad).

## Features
- [x] Follow the player
- [x] Pass through portals
- [x] Use blink to catch up (if the target gets too far away)
- [x] Close UIs (such as chests, crafting tables, etc.) if clicked accidentally
- [x] Auto-respawn (will attempt to respawn, unless another player is on the map)
- [x] Pick-up items
- [ ] Use skills

## Settings
The default settings are configured to suit **my** needs and may not work for you.
Please **do not** open an issue if the plugin isn't working due to your settings.

## Tips
- **DO NOT** use checkpoints.
- When the target passes through a portal, they should wait nearby so the bot can detect them upon loading.
- If you're moving too fast, it's your fault the bot isn't keeping up, not the plugin's.
- It's helpful to zoom out using the [WheresMyZoomAt](https://github.com/doubleespressobro/WheresMyZoomAt-PoE2) plugin. (But not too much)
- I recommend placing the Waypoint and Map Device close to each other.
- Since the bot picks up everything, it's best to use a [good loot filter](https://www.filterblade.xyz/?game=Poe2) for the pick-up items feature.

## Known bugs
- May experience issues in "ARENA".

## Donations
<a href="https://www.paypal.com/donate/?hosted_button_id=NX4PVU9B2YFDU">
  <img src="./assets/donate.png" width="200">
</a>