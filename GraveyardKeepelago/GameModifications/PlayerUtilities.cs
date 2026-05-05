using System.Collections.Generic;

namespace GraveyardKeepelago.GameModifications;

public static class PlayerUtilities
{
    private static IPlayerActions _instance;

    public static void Initialize(IPlayerActions instance)
    {
        _instance = instance;
    }

    public static bool IsDroppingItems { get; set; }

    public static void ApplyPermanentBuff(string buffID)
        => _instance?.ApplyPermanentBuff(buffID);

    public enum UnlockType
    {
        Craft,
        Perk,
        Work,
        Phrase
    }

    public static void ApplyUnlocks(List<(UnlockType, string)> unlocks)
        => _instance?.ApplyUnlocks(unlocks);

    public static void ApplyRelation(string npcID, int amount)
        => _instance?.ApplyRelation(npcID, amount);

    public static void DropItem(string itemID, int amount = 1)
        => _instance?.DropItem(itemID, amount);

    public static void SkipIntro()
        => _instance?.SkipIntro();

    private static readonly Dictionary<string, string> Flowscripts = new Dictionary<string, string>
    {
        { "First slice", "tech_first_slice" },
        { "The Beginning Of Alchemy", "tech_alchemy_begins" },
        { "tech_bag_alchemy", "tech_bag_alchemy" },
        { "Zombie logistic", "unlocked_zombie_logistics" },
        { "tech_bag_farming", "tech_bag_farming" },
        { "tech_bag_fishing", "tech_bag_fishing" },
        { "tech_bag_tools", "tech_bag_tools" },
        { "Advanced gravestones", "tech_advanced_gravestones" },
        { "Advanced gravestones II", "tech_advanced_gravestones_2" },
        { "Stone statues", "tech_stone_statues" },
        { "Vegetable set", "tech_vegetables_set" },
        { "Milk processing", "tech_milk_processing" },
        { "In Lentil We Trust", "tech_in_lentils_we_trust" },
        { "Honey cake", "tech_honey_cake" },
        { "soul_sins_1", "tech_soul_sins_1" },
        { "soul_sins_2", "tech_soul_sins_2" },
        { "soul_sins_3", "tech_soul_sins_3" },
        { "soul_sins_4", "tech_soul_sins_4" },
        { "soul_totem_tech", "unlock_totems" }
    };
}
