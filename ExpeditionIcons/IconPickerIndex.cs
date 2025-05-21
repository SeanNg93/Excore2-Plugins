using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ExpeditionIcons;

[JsonConverter(typeof(StringEnumConverter))]
public enum IconPickerIndex
{
    Experience,
    Rarity,
    Logbooks,
    PackSize,
    MonsterMods,
    Goblin,
    Artifacts,
    Quantity,
    CorruptedItems,
    RarityExcavatedChest,
    ArtifactsExcavatedChest,
    QuantityExcavatedChest,
    RunicMonsterDuplication,
    EliteMonstersIndicator,
    BadModsIndicator,
    BlightChest,
    FragmentChest,
    LeagueChest,
    JewelleryChest,
    WeaponChest,
    CurrencyChest,
    HeistChest,
    BreachChest,
    RitualChest,
    MetamorphChest,
    MapsChest,
    GemsChest,
    FossilsChest,
    DivinationCardsChest,
    EssenceChest,
    ArmourChest,
    LegionChest,
    DeliriumChest,
    UniquesChest,
    OtherChests,
}