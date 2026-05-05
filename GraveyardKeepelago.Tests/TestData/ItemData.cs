using System.Collections.Generic;

namespace GraveyardKeepelago.Tests.TestData;

public static class ItemData
{
    public static IEnumerable<object[]> BuffTestCases => new List<object[]>
    {
        new object[] { "Permanent Buff: Beer Thirst", "buff_beer", "buff_beer", "b_beer" },
        new object[] { "Permanent Buff: Berserk (Damage)", "buff_pot_berserk_damage", "add_damage", "b_sword" },
        new object[] { "Permanent Buff: Berserk (Poison)", "buff_pot_berserk_poison", "", "b_heart_hyphen" },
        new object[] { "Permanent Buff: Deep Sleep", "buff_sleep", "sleep_k_add", "b_pillow" },
        new object[] { "Permanent Buff: Difficult Corpses", "buff_skull", "body_max", "b_skull" },
        new object[] { "Permanent Buff: Digestion", "buff_pot_appetite", "food_multiplier", "b_hunger" },
        new object[] { "Permanent Buff: Fast Reflexes", "buff_fishing", "buff_fish_catch_time_mltplr", "b_fishing_1" },
        new object[] { "Permanent Buff: Fertility", "buff_plant", "buff_plant", "b_plant" },
        new object[] { "Permanent Buff: Good Fisherman", "buff_fishing2", "buff_pulling_fish_multplr", "b_fishing_2" },
        new object[] { "Permanent Buff: Handyman Mood", "buff_star", "", "b_star" },
        new object[] { "Permanent Buff: Hard Worker", "buff_hardwork", "axe_energy_k_add|pickaxe_energy_k_add", "b_axe" },
        new object[] { "Permanent Buff: Inspiration", "buff_pen", "", "b_pen" },
        new object[] { "Permanent Buff: Rage", "buff_sword", "add_damage", "b_sword" },
        new object[] { "Permanent Buff: Repentance", "buff_sins", "buff_sins", "b_sins" },
        new object[] { "Permanent Buff: Restoring", "buff_pot_heal_long", "", "b_heart_plus" },
        new object[] { "Permanent Buff: Slow Metabolism", "buff_longtimer", "buff_longtimer", "b_snail" },
        new object[] { "Permanent Buff: Speed", "buff_pot_speed", "speed_buff", "b_run" },
        new object[] { "Permanent Buff: Spiritual Blessing", "buff_gp_increase", "increase_gp_gain", "b_thanks" },
        new object[] { "Permanent Buff: Spiritual Magnetism", "buff_sin_shard", "increase_sin_shard_drop", "b_shards" },
        new object[] { "Permanent Buff: Steady Hand", "buff_cleancut", "buff_cleancut", "b_scalpel" },
        new object[] { "Permanent Buff: Tough Mood", "buff_shield", "add_armor", "b_shield" },
        new object[] { "Permanent Buff: Persistence", "buff_dlc_refugee_persistence", "", "" },
    };

    public static IEnumerable<object[]> RecipeTestCases => new List<object[]>
    {
        new object[] { "Create: Iron hammer", "hammer_2", "Craft" },
        new object[] { "Create: Incense", "incense_1", "Craft" },
        new object[] { "Create: Ink", "ink_jar", "Craft" },
        new object[] { "Create: Peat", "peat_from_waste", "Craft" },
        new object[] { "Create: Red wine", "bottle_red_vine", "Craft" },
        new object[] { "Blueprint: Progressive Pallet", null, "ProgressiveCraft" },
    };

    public static IEnumerable<object[]> PerkTestCases => new List<object[]>
    {
        new object[] { "Perk: Butcher", "p_butcher", null, null },
        new object[] { "Perk: Surgeon", "p_doctor", null, null },
        new object[] { "Perk: Fireman", "p_fireman", new[] { "@ingot_metal_huge", "@ingot_metal_1_huge", "@ingot_metal_2_huge" }, null },
        new object[] { "Perk: Master gatherer", "p_collector", null, new[] { "t_mushroom2" } },
        new object[] { "Perk: Axeman", "p_axeman", null, new[] { "t_wood_big" } },
        new object[] { "Perk: Eloquence", "p_eloquence", null, null },
    };

    public static IEnumerable<object[]> WorkTestCases => new List<object[]>
    {
        new object[] { "Gathering: Berry", "t_berry", null },
        new object[] { "Gathering: Stone rock", "t_stone", null },
        new object[] { "Gathering: Coal", "t_coal", null },
        new object[] { "Gathering: Old books", null, "p_t_old_books" },
        new object[] { "Gathering: Progressive Tree felling", null, "ProgressiveWork" },
    };

    public static IEnumerable<object[]> RelationTestCases => new List<object[]>
    {
        new object[] { "Astrologer", "npc_astrologer", 10 },
        new object[] { "Bishop", "npc_bishop", 10 },
        new object[] { "Inquisitor", "npc_inquisitor", 10 },
        new object[] { "Merchant", "npc_merchant", 10 },
        new object[] { "Ms. Charm", "npc_actress", 10 },
        new object[] { "Snake", "npc_cultist", 10 },
        new object[] { "Adam", "npc_potter", 10 },
        new object[] { "Koukol", "npc_hunchback", 10 },
        new object[] { "Cook", "npc_refugee_cook", 10 },
        new object[] { "Euric", "npc_euric", 10 },
    };

    public static IEnumerable<object[]> IngameItemTestCases => new List<object[]>
    {
        new object[] { "Red Research Points", "techpoints_red" },
        new object[] { "Green Research Points", "techpoints_green" },
        new object[] { "Blue Research Points", "techpoints_blue" },
        new object[] { "Keepers Key", "quest_key_astrologer" },
        new object[] { "Graveyard Scroll", "scroll_graveyard_prop" },
        new object[] { "Stamp", "stamp" },
        new object[] { "Salty Fork", "fork_salty" },
        new object[] { "Mirror of Pride", "mirror_of_pride" },
        new object[] { "Old rusty key", "souls_zone_key" },
        new object[] { "Pride charged shard", "pride_charged_shard" },
    };
}
