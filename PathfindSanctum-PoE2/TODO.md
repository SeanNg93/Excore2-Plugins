# TODO

## Sanctum State Tracker
- Test SanctumStateTracker when you acquire a smoke affliction

## Features
- Find a way to read active afflictions
    - Account for active afflictions (if we have +1 affliction, it makes getting afflictions significantly worse)
- Find a way to read active relic bonuses
    - [If 100% reduced trap damage taken -> 0 honour taken] -> set to 0 a bunch of afflictions (can reduce them dynamically by a %, but I don't think it's a high priority)

## Weights
- Add more dynamic weights
- Check if weights are actually accurate / useful
- Swap to RoomsByLayer once it's available
- Replace the static dictionaries with e.g. files.SanctumPersistentEffects so it has less maintenance costs as poe2 updates
- Should we do boons?
- Don't prioritize fountains if you nuke your sacred water every floor end via affliction :KEKW: 
- Only prioritize merchant reward if you have more than idk, 360 sacred water - reduced cost relic %

## Settings
- Expose ProfileContent weights
- Expose Dynamic Weights
- Add confirmation to Reset Profile button

