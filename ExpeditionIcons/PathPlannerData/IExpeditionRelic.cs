﻿namespace ExpeditionIcons.PathPlannerData;

public interface IExpeditionRelic
{
    public (double, double) GetScoreMultiplier(IExpeditionLoot loot);
}