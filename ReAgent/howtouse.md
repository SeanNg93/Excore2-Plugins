# ReAgent Plugin - User Guide

## Overview

ReAgent is an ExileCore2 plugin for Path of Exile that allows you to create automated responses to game conditions through a rule-based system. It can automatically use flasks, skills, and perform other actions based on configurable rules that monitor your character's state, nearby monsters, and game environment.

## Core Concepts

### Profiles
- Multiple profiles can be created and switched between
- Each profile contains rule groups
- Only one profile can be active at a time

### Rule Groups
- Logical groupings of related rules
- Can be enabled/disabled for different environments (town, hideout, maps)
- Contains individual rules

### Rules
- Each rule has:
  - An action type (Key press, SingleSideEffect, MultipleSideEffects)
  - A key to press (for Key action type)
  - A rule source (condition expression)
  - Optional syntax version selection

## Rule Syntax

Rules are written as boolean expressions that evaluate to true or false. When a rule evaluates to true, the associated action is triggered.

### Basic Syntax Examples

```
// Use life flask when HP below 70%
Vitals.HP.Percent <= 70 && 
Flasks[0].CanBeUsed && 
!Flasks[0].Active

// Use mana flask when mana below 30%
Vitals.Mana.Percent <= 30 && 
Flasks[1].CanBeUsed && 
!Flasks[1].Active

// Use skill when monsters nearby and skill ready
MonsterCount(100) > 0 && 
Skills["SkillName"].CanBeUsed && 
SinceLastActivation(1.0)
```

### Ternary Syntax for Side Effects

For MultipleSideEffects, you can use the C# ternary operator syntax to conditionally return side effects:

```
// Condition ? side effects array : null
SinceLastActivation(2) && GetTimerValue("timer1") == 0 && MonsterCount(50) > 0
? new ISideEffect[] { PressKeySideEffect("Q"), StartTimerSideEffect("timer1") }
: null

// Example: ED+Contagion combo
SinceLastActivation(2) && 
GetTimerValue("e1") == 0 && GetTimerValue("ess") == 0 && 
Monsters().Any(m => m.DistanceToCursor <= 5 && m.Buffs.Has("contagion"))
? new ISideEffect[] { StartTimerSideEffect("e1"), PressKeySideEffect("LButton") }
: null

// Follow-up action after timer
GetTimerValue("e1") > 1.2
? new ISideEffect[] { PressKeySideEffect("F"), ResetTimerSideEffect("e1") }
: null
```

## Available Properties and Methods

### Character State

#### Vitals
- `Vitals.HP.Current` - Current health points
- `Vitals.HP.Max` - Maximum health points
- `Vitals.HP.Percent` - Health percentage (0-100)
- `Vitals.ES.Current` - Current energy shield
- `Vitals.ES.Max` - Maximum energy shield
- `Vitals.ES.Percent` - Energy shield percentage (0-100)
- `Vitals.Mana.Current` - Current mana
- `Vitals.Mana.Max` - Maximum mana
- `Vitals.Mana.Percent` - Mana percentage (0-100)

#### Flasks
- `Flasks[0]` through `Flasks[4]` - Access to flask slots (0-indexed)
- `Flasks[i].Active` - Whether flask effect is active
- `Flasks[i].CanBeUsed` - Whether flask has enough charges to use
- `Flasks[i].Charges` - Current charges
- `Flasks[i].MaxCharges` - Maximum charges
- `Flasks[i].ChargesPerUse` - Charges consumed per use
- `Flasks[i].Name` - Flask name

#### Skills
- `Skills["SkillName"]` - Access skill by name
- `Skills["SkillName"].CanBeUsed` - Whether skill can be used
- `Skills["SkillName"].Cooldown` - Skill cooldown
- `WeaponSwapSkills["SkillName"]` - Access skills on weapon swap

#### Buffs and Ailments
- `Buffs["BuffName"]` - Check if buff is active
- `Buffs["BuffName"].TimeLeft` - Time left on buff
- `Ailments` - Collection of active ailments

#### Character Status
- `IsMoving` - Whether character is moving
- `Animation` - Current animation
- `AnimationId` - Animation ID
- `AnimationStage` - Animation stage
- `ActiveWeaponSetIndex` - Active weapon set (0 or 1)

### Environment

#### Area Information
- `IsInHideout` - In hideout
- `IsInTown` - In town
- `IsInPeacefulArea` - In peaceful area (town or hideout)
- `IsInEscapeMenu` - Escape menu is open
- `AreaName` - Current area name

#### UI State
- `IsChatOpen` - Chat is open
- `IsLeftPanelOpen` - Left panel is open
- `IsRightPanelOpen` - Right panel is open
- `IsAnyFullscreenPanelOpen` - Any fullscreen panel is open
- `IsAnyLargePanelOpen` - Any large panel is open

#### Input
- `IsKeyPressed(Keys.X)` - Check if key is pressed

### Monsters and Entities

#### Monster Counting
- `MonsterCount()` - Count all monsters
- `MonsterCount(range)` - Count monsters within range
- `MonsterCount(range, MonsterRarity)` - Count monsters of specific rarity within range

#### Monster Access
- `Monsters()` - All monsters
- `Monsters(range)` - Monsters within range
- `Monsters(range, MonsterRarity)` - Monsters of specific rarity within range
- `FriendlyMonsters` - Friendly monsters
- `AllMonsters` - All monsters regardless of visibility
- `HiddenMonsters` - Hidden monsters
- `Corpses` - Dead monster corpses

#### Other Entities
- `MiscellaneousObjects` - Miscellaneous objects
- `NoneEntities` - Entities with no type
- `IngameIcons` - In-game icons
- `MiniMonoliths` - Mini monoliths
- `Effects` - Effect entities

### State Management

#### Flags
- `IsFlagSet("flagName")` - Check if flag is set

#### Timers
- `SinceLastActivation(seconds)` - Time since rule last activated
- `GetTimerValue("timerName")` - Get timer value
- `IsTimerRunning("timerName")` - Check if timer is running

#### Numbers
- `GetNumberValue("valueName")` - Get stored number value

## Side Effects

When using `SingleSideEffect` or `MultipleSideEffects` action types, you can return side effect objects:

### Key Press Side Effects
- `new PressKeySideEffect(Keys.X)` - Press a key
- `new StartKeyHoldSideEffect(Keys.X)` - Start holding a key
- `new ReleaseKeyHoldSideEffect(Keys.X)` - Release a held key

### State Management Side Effects
- `new SetFlagSideEffect("flagName", true/false)` - Set a flag
- `new ResetFlagSideEffect("flagName")` - Reset a flag
- `new SetNumberSideEffect("valueName", number)` - Set a number value
- `new ResetNumberSideEffect("valueName")` - Reset a number value
- `new StartTimerSideEffect("timerName")` - Start a timer
- `new StopTimerSideEffect("timerName")` - Stop a timer
- `new RestartTimerSideEffect("timerName")` - Restart a timer
- `new ResetTimerSideEffect("timerName")` - Reset a timer

### UI Side Effects
- `new DisplayTextSideEffect("text", "color")` - Display text
- `new DisplayGraphicSideEffect("path", x, y, width, height)` - Display graphic
- `new ProgressBarSideEffect(value, max, x, y, width, height, "color")` - Display progress bar

## Common Rule Patterns

### Flask Usage
```
// Life flask at low health
Vitals.HP.Percent <= 40 && Flasks[0].CanBeUsed && !Flasks[0].Active

// Mana flask at low mana
Vitals.Mana.Percent <= 20 && Flasks[1].CanBeUsed && !Flasks[1].Active

// Quicksilver flask when moving and not already active
IsMoving && Flasks[2].CanBeUsed && !Flasks[2].Active && SinceLastActivation(5.0)
```

### Skill Usage
```
// Use skill when monsters nearby
MonsterCount(100) > 0 && Skills["SkillName"].CanBeUsed && SinceLastActivation(1.0)

// Use skill when health low
Vitals.HP.Percent <= 30 && Skills["MoltenShell"].CanBeUsed && SinceLastActivation(1.0)
```

### Conditional Logic
```
// Different behavior based on monster count
MonsterCount(100) > 10 ? 
  (Skills["AoESkill"].CanBeUsed && SinceLastActivation(1.0)) : 
  (MonsterCount(50) > 0 && Skills["SingleTargetSkill"].CanBeUsed && SinceLastActivation(1.0))
```

## Tips and Best Practices

1. **Rule Ordering**: Rules are evaluated in order. Place high-priority rules at the top.

2. **Performance**: Keep rules simple and efficient. Complex rules may impact game performance.

3. **Testing**: Test rules in safe areas before relying on them in dangerous content.

4. **Delays**: Use `SinceLastActivation(seconds)` to prevent spamming actions.

5. **Defensive Rules**: Place defensive rules (life flasks, guard skills) before offensive ones.

6. **Profiles**: Create different profiles for different characters or playstyles.

7. **Backup**: Export important profiles to avoid losing them.

8. **Debugging**: Use the "Dump State" button to troubleshoot rule issues.

## Examples of Complete Rule Sets

### Basic Flask Management
```
// Life flask at low health or when taking damage
Vitals.HP.Percent <= 65 && Flasks[0].CanBeUsed && !Flasks[0].Active

// Mana flask when low
Vitals.Mana.Percent <= 25 && Flasks[1].CanBeUsed && !Flasks[1].Active

// Quicksilver when moving
IsMoving && Flasks[2].CanBeUsed && !Flasks[2].Active && SinceLastActivation(4.0)

// Defensive flask when monsters nearby
MonsterCount(60) >= 3 && Flasks[3].CanBeUsed && !Flasks[3].Active && SinceLastActivation(1.0)

// Offensive flask when monsters nearby
MonsterCount(60) >= 5 && Flasks[4].CanBeUsed && !Flasks[4].Active && SinceLastActivation(1.0)
```

### Skill Automation
```
// Guard skill when monsters nearby
MonsterCount(80) >= 5 && Skills["MoltenShell"].CanBeUsed && SinceLastActivation(8.0)

// Golem resummon when not present
!Buffs["SummonedGolem"] && Skills["SummonChaosGolem"].CanBeUsed && SinceLastActivation(5.0)

// Curse on monsters
MonsterCount(100) >= 3 && Skills["Despair"].CanBeUsed && SinceLastActivation(1.0)
```

### Advanced Logic with Side Effects
```csharp
// Return multiple side effects based on game state
MonsterCount(100) > 5 ? 
  new ISideEffect[] { 
    new PressKeySideEffect(Keys.Q), 
    new SetFlagSideEffect("combat", true),
    new StartTimerSideEffect("combatTimer")
  } : 
  null
``` 