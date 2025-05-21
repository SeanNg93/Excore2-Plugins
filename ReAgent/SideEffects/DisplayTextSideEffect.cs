﻿using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Numerics;
using ReAgent.State;

namespace ReAgent.SideEffects;

[DynamicLinqType]
[Api]
[method: Api]
public record DisplayTextSideEffect(string Text, Vector2 Position, string Color) : ISideEffect
{
    public SideEffectApplicationResult Apply(RuleState state)
    {
        state.InternalState.TextToDisplay.Add((Text, Position, Color));
        return SideEffectApplicationResult.AppliedUnique;
    }

    public override string ToString() => $"Display \"{Text}\" at {Position} with color {Color}";
}