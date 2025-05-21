namespace ExpeditionIcons;

public class RelicSettings
{
    public float Multiplier = 1;
    public float Increase = 0;

    public static RelicSettings Default => new RelicSettings { Multiplier = 1, Increase = 0.15f };
}