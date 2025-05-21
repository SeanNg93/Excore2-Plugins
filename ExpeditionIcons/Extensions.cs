using System.Numerics;
using ExileCore2.Shared.Helpers;
using GameOffsets2.Native;

namespace ExpeditionIcons;

public static class Extensions
{
    public static bool DistanceLessThanOrEqual(this Vector2 v, Vector2 other, float distance)
    {
        return v.DistanceSquared(other) < distance * distance;
    }

    public static bool DistanceLessThanOrEqual(this Vector2i v, Vector2i other, float distance)
    {
        return v.DistanceSqr(other) < distance * distance;
    }
}