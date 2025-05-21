using System;
using System.Collections.Generic;
using System.Numerics;

namespace ExpeditionIcons.PathPlannerData;

public record ExpeditionEnvironment(
    List<(Vector2, IExpeditionRelic)> Relics,
    List<(Vector2, IExpeditionLoot)> Loot,
    float ExplosionRange,
    float ExplosionRadius,
    int MaxExplosions,
    Vector2 StartingPoint,
    Func<Vector2, bool> IsValidPlacement,
    (Vector2 Min, Vector2 Max) ExclusionArea,
    bool IsLogbook);