using System.Collections.Generic;
using System.Numerics;

namespace ExpeditionIcons.PathPlannerData;

public record PathState(List<Vector2> Points, double Score);