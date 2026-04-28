using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.Items;
using KaitoKid.ArchipelagoUtilities.Net.Client;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Logic;

public class GKItemManager
{
    private ILogger _logger;

    private Dictionary<string, IAPItem> _permanentBuffsByName;
    private Dictionary<string, IAPItem> _recipesByName;
    private Dictionary<string, GKPerk> _perksByName;
    private Dictionary<string, GKRelation> _relationItemsByName;
    private Dictionary<string, GKIngameItem> _ingameItemsByName;

    public GKItemManager(ILogger logger, ArchipelagoClient archipelago)
    {
        _logger = logger;
        InitializeData();
    }

    private void InitializeData()
    {
        _permanentBuffsByName = new Dictionary<string, IAPItem>();
        _recipesByName = new Dictionary<string, IAPItem>();
        _perksByName = new Dictionary<string, GKPerk>();
        _relationItemsByName = new Dictionary<string, GKRelation>();
        _ingameItemsByName = new Dictionary<string, GKIngameItem>();
        
        InitializePermanentBuffItems();
        InitializeRecipeItems();
        InitializePerkItems();
        InitializeRelationItems();
        InitializeIngameItems();
    }

    private void InitializePermanentBuffItems()
    {
        var buffs = new []
        {
            new { name = "Beer Thirst", id = "buff_beer", res_types = new[]{"buff_beer"}, icon = "b_beer" },
            new { name = "Berserk (Damage)", id = "buff_pot_berserk_damage", res_types = new[]{"add_damage"}, icon = "b_sword" },
            new { name = "Berserk (Poison)", id = "buff_pot_berserk_poison", res_types = new string[]{}, icon = "b_heart_hyphen" },
            // Circumspect
            new { name = "Deep Sleep", id = "buff_sleep", res_types = new[]{"sleep_k_add"}, icon = "b_pillow" },
            new { name = "Difficult Corpses", id = "buff_skull", res_types = new[]{"body_max"}, icon = "b_skull" },
            new { name = "Digestion", id = "buff_pot_appetite", res_types = new[]{"food_multiplier"}, icon = "b_hunger" },
            new { name = "Fast Reflexes", id = "buff_fishing", res_types = new[]{"buff_fish_catch_time_mltplr"}, icon = "b_fishing_1" },
            new { name = "Fertility", id = "buff_plant", res_types = new[]{"buff_plant"}, icon = "b_plant" },
            new { name = "Good Fisherman", id = "buff_fishing2", res_types = new[]{"buff_pulling_fish_multplr"}, icon = "b_fishing_2" },
            new { name = "Handyman Mood", id = "buff_star", res_types = new string[]{}, icon = "b_star" },
            //new { name = "", id = "buff_star_food", res_types = new string[]{}, icon = "b_star" },
            new { name = "Hard Worker", id = "buff_hardwork", res_types = new[]{"axe_energy_k_add", "pickaxe_energy_k_add"}, icon = "b_axe" },
            new { name = "Inspiration", id = "buff_pen", res_types = new string[]{}, icon = "b_pen" },
            //new { name = "", id = "buff_pen_food", res_types = new string[]{}, icon = "p_pen" },
            new { name = "Rage", id = "buff_sword", res_types = new[]{"add_damage"}, icon = "b_sword" },
            //new { name = "Rage", id = "buff_pot_damage", res_types = new[]{"add_damage"}, icon = "b_sword" },
            //new { name = "Rage", id = "buff_sword_food", res_types = new[]{"add_damage"}, icon = "b_sword" },
            new { name = "Repentance", id = "buff_sins", res_types = new[]{"buff_sins"}, icon = "b_sins" },
            new { name = "Restoring", id = "buff_pot_heal_long", res_types = new string[]{}, icon = "b_heart_plus" },
            new { name = "Slow Metabolism", id = "buff_longtimer", res_types = new[]{"buff_longtimer"}, icon = "b_snail" },
            new { name = "Speed", id = "buff_pot_speed", res_types = new[]{"speed_buff"}, icon = "b_run" },
            new { name = "Spiritual Blessing", id = "buff_gp_increase", res_types = new[]{"increase_gp_gain"}, icon = "b_thanks" },
            new { name = "Spiritual Magnetism", id = "buff_sin_shard", res_types = new[]{"increase_sin_shard_drop"}, icon = "b_shards" },
            new { name = "Steady Hand", id = "buff_cleancut", res_types = new[]{"buff_cleancut"}, icon = "b_scalpel" },
            //new { name = "Tired", id = "buff_tired", res_types = new[]{"tired"}, icon = "b_sleep" },
            new { name = "Tough Mood", id = "buff_shield", res_types = new[]{"add_armor"}, icon = "b_shield" },
            //new { name = "Tough Mood", id = "buff_pot_armor", res_types = new[]{"add_armor"}, icon = "b_shield" },
            //new { name = "Tough Mood", id = "buff_shield_food", res_types = new[]{"add_armor"}, icon = "b_shield" },
            
            // TODO: either this buff or the perk gives the energy regeneration, need to check
            new { name = "Persistence", id = "buff_dlc_refugee_persistence", res_types = new string[]{}, icon = "" },
            
            // unused
            /*
            new { name = "", id = "poison", res_types = new string[]{}, icon = "b_skull" },
            new { name = "", id = "poison_2", res_types = new string[]{}, icon = "b_skull" },
            new { name = "", id = "buff_garlic_poison", res_types = new string[]{}, icon = "b_skull" },
            new { name = "", id = "buff_tired_tech", res_types = new string[]{}, icon = "" },
            new { name = "", id = "buff_survay", res_types = new[]{"buff_survay"}, icon = "b_science" },
            */
        };
        foreach (var item in buffs )
        {
            _permanentBuffsByName.Add(item.name, new GKPermaBuff($"Permanent Buff: {item.id}", item.res_types.ToList(), item.icon));
        }
    }

    private void InitializeRecipeItems()
    {
        InitializeBlueprintItems();
        InitializeProgressiveBlueprintItems();

        InitializeGatheringRecipeItems();
        
        InitializeExtractionRecipeItems();
        InitializeFarmingRecipeItems();
        InitializeCookingRecipeItems();
        InitializePrayerRecipeItems();
        InitializeInjectionRecipeItems();
        InitializeBagRecipeItems();
        InitializeSmithingRecipeItems();
        InitializeBuildingRecipeItems();
        InitializeSpiritualismRecipeItems();

        InitializeQuestRecipeItems();
        InitializeNormalRecipeItems();

        InitializeProgressiveRecipeItems();
    }
    
    private void InitializeBlueprintItems()
    {
        var blueprintsByBuilddesk = new Dictionary<string, Dictionary<string, object>>()
        {
            ["alchemy_builddesk"] = new() {
                ["Alchemy mill"] = "p:mf_alchemy_mill_place",
                ["Alchemy rack"] = "p:rack_alchemy_place",
                ["Alchemy workbench"] = "p:mf_alchemy_craft_02_place",
                ["Alchemy workbench II"] = "p:mf_alchemy_craft_03_place",
                ["Bookshelf"] = "p:obj_church_bookcase_place",
                ["Church workbench"] = "p:table_book_constr_place",
                ["Hand mixer"] = "p:mf_alchemy_stirrer_01_place",
                ["Random text generator"] = "p:zombie_pulpit",
                ["Scrollshelf"] = "p:obj_church_scroll_cabinet_place",
                ["Study table"] = "p:mf_alchemy_survey_place",
                ["Zombie alchemy decomposer"] = "p:alchemy_table_zombie_place",
                ["Zombie alchemy workbench"] = "p:alchemy_workbench_zombie_place",
            },
            ["beehouse"] = new() {
                ["Beehive"] = "p:beehouse_place",
            },
            ["cellar_builddesk"] = new() {
                ["Alcohol distiller"] = "p:mf_distcube_3_place",
                ["Brewing stand"] = "p:brewing_stand_place",
                ["Fermentation barrel"] = "p:barrel_brew_place",
                ["Wine making barrel"] = "p:mf_barrel_mid_place",
                ["Zombie-brewery"] = "p:mf_zombie_brewing_place",
                ["Zombie-winery"] = "p:mf_zombie_winemaking_place",
            },
            ["church_builddesk"] = new() {
                ["Stained glass window"] = "p:church_window_1",
            },
            ["cremation_builddesk"] = new() {
                ["Place for burning corpses"] = "p:mf_pyre_placed",
            },
            ["garden_builddesk"] = new() {
                ["Compost heap"] = "p:v_obj_compost_heap_place",
                ["Empty garden bed"] = "p:garden_empty_place",
                ["Garden bed with sticks"] = "p:garden_empty_stick_place",
                ["Zombie farm"] = "p:zombie_garden_desk_place",
            },
            ["graveyard_builddesk"] = new() {
                ["Stone columbarium"] = "p:columbarium_stone_place",
                ["Marble columbarium"] = "p:columbarium_marble_place",
                ["Marble flagstones"] = "p:road_marble",
                ["Lantern"] = "p:lantern_3",
            },
            ["keeper_room_builddesk"] = new() {
                ["Burning Witch"] = "p:keeper_room_picture_1",
                ["Bishop's Choice"] = "p:keeper_room_picture_4",
                ["Inquisitors' Joy"] = "p:keeper_room_picture_5",
                ["Ms. Charm Monroe"] = "p:keeper_room_picture_6",
                ["Donkeys of the world, unite!"] = "p:keeper_room_picture_7",
            },
            ["mining_builddesk"] = new() {
                ["Lantern network"] = "p:lantern_network",
                ["Place for lantern"] = "p:lantern_place",
                ["Porter station"] = "p:porter_station",
            },
            ["mf_wood_builddesk"] = new() {
                ["Chopping spot"] = "p:mf_chocks_1",
                ["Jewelry table"] = "p:mf_jewelry_place",
                ["Paper press"] = "p:mf_paper_press_place",
                ["Potter's wheel"] = "p:mf_potter_wheel_1_place",
                ["Timber stockpile"] = "p:mf_timber_1_place",
                ["Water pump"] = "p:well_pump_place",
            },
            ["morgue_builddesk"] = new() {
                ["Mortuary rack"] = "p:rack_organs_place",
                ["Resurrection table"] = "p:zombie_crafting_table_place",
            },
            ["souls_builddesk"] = new() {
                ["Extension: Healing from Envy"] = ":envy_healing_extension",
                ["Extension: Healing from Gluttony"] = ":gluttony_healing_extension",
                ["Extension: Healing from Lust"] = ":lust_healing_extension",
                ["Extension: Healing from Pride"] = ":pride_healing_extension",
                ["Extension: Healing from Sloth"] = ":sloth_healing_extension",
                ["Extension: Healing from Wrath"] = ":wrath_healing_extension",
                ["Organ workbench"] = ":soul_workbench",
                ["Soul receiver"] = ":soul_totem_souls",
                ["Wall crematorium"] = "p:mf_crematorium",
            },
            ["tree_garden_builddesk"] = new() {
                ["Place for the apple tree"] = "p:tree_apple_garden_place",
                ["Place for the berry bush"] = "p:bush_berry_garden_place",
            },
            ["vineyard_builddesk"] = new()
            {
                ["Vine trellis"] = "p:vineyard_grapes_stick_place",
                ["Zombie vineyard"] = "p:zombie_vineyard_desk_place",
            }
        };
        foreach (var kvp in blueprintsByBuilddesk)
        {
            var builddesk = kvp.Key;
            var blueprints = kvp.Value;
            foreach (var bp in blueprints)
            {
                var fullName = ItemParser.BLUEPRINT_PREFIX + bp.Key;
                var fullID = builddesk + ":" + bp.Value;
                _recipesByName.Add(fullName, new GKRecipe(fullID));
            }
        }
        _recipesByName.Add(ItemParser.BLUEPRINT_PREFIX + "Lawn", new GKRecipe([
            "graveyard_builddesk:p:grass_sward_2x3",
            "@graveyard_builddesk:p:grass_sward_2x4",
            "@graveyard_builddesk:p:grass_sward_2x6"
        ]));
        _recipesByName.Add(ItemParser.BLUEPRINT_PREFIX + "Trunk", new GKRecipe([
            "garden_builddesk:p:mf_box_stuff_place",
            "@mining_builddesk:p:mf_box_stuff_place",
            "@vineyard_builddesk:p:mf_box_stuff_place",
            "@graveyard_builddesk:p:mf_box_stuff_place",
            "@cremation_builddesk:p:mf_box_stuff_place",
        ]));
        _recipesByName.Add(ItemParser.BLUEPRINT_PREFIX + "Vine press", new GKRecipe([
            "mf_wood_builddesk:p:mf_vine_press_place",
            "@cellar_builddesk:p:mf_vine_press_place"
        ]));
        _recipesByName.Add(ItemParser.BLUEPRINT_PREFIX + "Stone stockpile", new GKRecipe([
            "mf_wood_builddesk:p:mf_stones_1_place",
            "@mining_builddesk:p:mf_stones_1_place"
        ]));
        _recipesByName.Add(ItemParser.BLUEPRINT_PREFIX + "Iron ore stockpile", new GKRecipe([
            "mf_wood_builddesk:p:mf_ore_1_complete",
            "@mining_builddesk:p:mf_ore_1_complete"
        ]));
        _recipesByName.Add(ItemParser.BLUEPRINT_PREFIX + "Zombie sawmill", new GKRecipe([
            "zombie_sawmill_unfinished_place"
        ]));
        _recipesByName.Add(ItemParser.BLUEPRINT_PREFIX + "Zombie ore mine", new GKRecipe([
            "mining_builddesk::zombie_mine_bench_left",
            "@mining_builddesk::zombie_mine_bench_right"
        ]));
        _recipesByName.Add(ItemParser.BLUEPRINT_PREFIX + "Zombie stone mine", new GKRecipe([
            "mining_builddesk::zombie_mine_fence_front_stone",
            "@mining_builddesk::zombie_mine_fence_left_front_stone"
        ]));
        _recipesByName.Add(ItemParser.BLUEPRINT_PREFIX + "Zombie marble mine", new GKRecipe([
            "mining_builddesk::zombie_mine_fence_front_marble_right",
            "@mining_builddesk::zombie_mine_fence_front_marble_left"
        ]));
    }

    private void InitializeProgressiveBlueprintItems()
    {
        InitializeProgressiveAnatomyAndAlchemyBlueprintItems();
        InitializeProgressiveTheologyBlueprintItems();
        InitializeProgressiveBookWritingBlueprintItems();
        //InitializeProgressiveFarmingAndNatureBlueprintItems();
        InitializeProgressiveSmithingBlueprintItems();
        InitializeProgressiveBuildingBlueprintItems();
        InitializeProgressiveSpiritualismBlueprintItems();
    }

    private void InitializeProgressiveAnatomyAndAlchemyBlueprintItems()
    {
        var progressiveBlueprints = new[]
        {
            new
            {
                name = "Progressive Preparation Place", ids = new[]
                {
                    new[] { "morgue_builddesk:p:mf_preparation_1_place" },
                    new[] { "morgue_builddesk:p:mf_preparation_2_place" },
                }
            },
            new
            {
                name = "Progressive Pallet", ids = new[]
                {
                    new[] { "morgue_builddesk:p:corpse_bed_place" },
                    new[] { "morgue_builddesk:p:corpse_bed_big_place" },
                    new[] { "morgue_builddesk:p:corpse_fridge_place" },
                }
            },
            new
            {
                name = "Progressive Embalming Table", ids = new[]
                {
                    new[] { "morgue_builddesk:p:mf_balsamation_1_place" },
                    new[] { "morgue_builddesk:p:mf_balsamation_2_place" },
                }
            },
            new
            {
                name = "Progressive Distillation Cube", ids = new[]
                {
                    new[] { "alchemy_builddesk:p:mf_distcube_2_clay_place" },
                    new[] { "alchemy_builddesk:p:mf_distcube_2_cuprum_place", "@upgr_to_mf_distcube_2_cuprum" },
                }
            },
        };
        foreach (var item in progressiveBlueprints)
        {
            var recipeName = ItemParser.BLUEPRINT_PREFIX + item.name;
            var progressiveIDs = item.ids.Select(row => row.ToList()).ToList();
            _recipesByName.Add(recipeName, new GKProgressiveRecipe(progressiveIDs));
        }
    }

    private void InitializeProgressiveTheologyBlueprintItems()
    {
        var progressiveBlueprints = new[]
        {
            new
            {
                name = "Progressive Candelabra", ids = new[]
                {
                    new[] { "church_builddesk:p:candelabrum_1", "@church_builddesk:p:wall_candelabrum_1" },
                    new[] { "church_builddesk:p:candelabrum_2", "@church_builddesk:p:wall_candelabrum_2" },
                    new[] { "church_builddesk:p:candelabrum_3", "@church_builddesk:p:wall_candelabrum_3" },
                }
            },
            new
            {
                name = "Progressive Church Bench", ids = new[]
                {
                    new[] { "church_builddesk:p:church_bench" },
                    new[] { "church_builddesk:p:church_bench_2" },
                }
            },
            new
            {
                name = "Progressive Confessional", ids = new[]
                {
                    new[] { "church_builddesk:p:church_budka_1_place" },
                    new[] { "church_builddesk:p:church_budka_2_place" },
                }
            },
            new
            {
                name = "Progressive Church Shrine", ids = new[]
                {
                    new[] { "church_builddesk:p:church_table_1_place" },
                    new[] { "church_builddesk:p:church_table_2_place" },
                }
            },
            new
            {
                name = "Progressive Incense Burner", ids = new[]
                {
                    new[] { "church_builddesk:p:c_obj_incense_1_place" },
                    new[] { "church_builddesk:p:c_obj_incense_2_place" },
                }
            },
            new
            {
                name = "Progressive Prayer Station", ids = new[]
                {
                    new[] { "graveyard_builddesk:p:pray_stand_wd_place" },
                    new[] { "graveyard_builddesk:p:pray_stand_stn_place" },
                }
            },
        };
        foreach (var item in progressiveBlueprints)
        {
            var recipeName = ItemParser.BLUEPRINT_PREFIX + item.name;
            var progressiveIDs = item.ids.Select(row => row.ToList()).ToList();
            _recipesByName.Add(recipeName, new GKProgressiveRecipe(progressiveIDs));
        }
    }
    
    private void InitializeProgressiveBookWritingBlueprintItems()
    {
        var progressiveBlueprints = new[]
        {
            new
            {
                name = "Progressive Desk", ids = new[]
                {
                    new[] { "alchemy_builddesk:p:desk" },
                    new[] { "alchemy_builddesk:p:desk_2" },
                }
            },
            new
            {
                name = "Progressive Printing Press", ids = new[]
                {
                    new[] { "alchemy_builddesk:p:mf_printing_press_place" },
                    new[] { "alchemy_builddesk:p:mf_printing_press_2_place", "@mf_printing_press_1_to_2" },
                }
            },
        };
        foreach (var item in progressiveBlueprints)
        {
            var recipeName = ItemParser.RECIPE_PREFIX + item.name;
            var progressiveIDs = item.ids.Select(row => row.ToList()).ToList();
            _recipesByName.Add(recipeName, new GKProgressiveRecipe(progressiveIDs));
        }
    }
    
    private void InitializeProgressiveSmithingBlueprintItems()
    {
        var progressiveBlueprints = new[]
        {
            new
            {
                name = "Progressive Furnace", ids = new[]
                {
                    new[] { "mf_wood_builddesk:p:mf_furnace_0_place" },
                    new[] { "mf_wood_builddesk:p:mf_furnace_1_place", "@mf_furnace_0_to_1" },
                    new[] { "mf_wood_builddesk:p:mf_furnace_2_place", "@mf_furnace_1_to_2" },
                }
            },
            new
            {
                name = "Progressive Anvil", ids = new[]
                {
                    new[] { "mf_wood_builddesk:p:mf_anvil_1" },
                    new[] { "mf_wood_builddesk:p:mf_anvil_2" },
                    new[] { "mf_wood_builddesk:p:mf_anvil_3", "@mf_anvil_2_to_3" },
                }
            },
        };
        foreach (var item in progressiveBlueprints)
        {
            var recipeName = ItemParser.RECIPE_PREFIX + item.name;
            var progressiveIDs = item.ids.Select(row => row.ToList()).ToList();
            _recipesByName.Add(recipeName, new GKProgressiveRecipe(progressiveIDs));
        }
    }

    private void InitializeProgressiveBuildingBlueprintItems()
    {
        var progressiveBlueprints = new[]
        {
            new
            {
                name = "Progressive Saw", ids = new[]
                {
                    new[] { "mf_wood_builddesk:p:mf_timber_1_place" },
                    new[] { "mf_wood_builddesk:p:mf_saw_1_place" },
                }
            },
            new
            {
                name = "Progressive Stone Cutter", ids = new[]
                {
                    new[] { "mf_wood_builddesk:p:mf_hammer_0_place", "@mining_builddesk:p:mf_hammer_0_place" },
                    new[] { "mf_wood_builddesk:p:mf_hammer_1_place" },
                }
            },
            new
            {
                name = "Progressive Carpenter's Workbench", ids = new[]
                {
                    new[] { "mf_wood_builddesk:p:mf_workbench_1_place" },
                    new[] { "mf_wood_builddesk:p:mf_workbench_2_place", "@mf_workbench_1_to_2" },
                }
            },
        };
        foreach (var item in progressiveBlueprints)
        {
            var recipeName = ItemParser.RECIPE_PREFIX + item.name;
            var progressiveIDs = item.ids.Select(row => row.ToList()).ToList();
            _recipesByName.Add(recipeName, new GKProgressiveRecipe(progressiveIDs));
        }
    }
    
    private void InitializeProgressiveSpiritualismBlueprintItems()
    {
        var progressiveBlueprints = new[]
        {
            new
            {
                name = "Progressive Soul Container", ids = new[]
                {
                    new[] { "souls_builddesk:p:soul_container_place" },
                    new[] { "souls_builddesk:p:soul_container_2_place" },
                    new[] { "souls_builddesk:p:soul_container_3_place" },
                }
            },
            new
            {
                name = "Progressive Soul Extractor", ids = new[]
                {
                    new[] { "souls_builddesk::soul_extractor_upgrd_to_2" },
                    new[] { "souls_builddesk::soul_extractor_upgrd_to_3" },
                }
            },
        };
        foreach (var item in progressiveBlueprints)
        {
            var recipeName = ItemParser.RECIPE_PREFIX + item.name;
            var progressiveIDs = item.ids.Select(row => row.ToList()).ToList();
            _recipesByName.Add(recipeName, new GKProgressiveRecipe(progressiveIDs));
        }
    }

    private void InitializeGatheringRecipeItems()
    {
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Old books", new GKWork(null, "p_t_old_books"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Edible mushroom", new GKWork($"t_mushroom"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Berry", new GKWork("t_berry"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Apple", new GKWork("t_apple"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Beeswax", new GKWork(null, "p_t_beeswax"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Butterfly", new GKWork(null, "p_t_butterfly"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Moth", new GKWork(null, "p_t_moth"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Bee", new GKWork(null, "p_t_bee"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Maggot", new GKWork(null, "p_t_maggot"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Swamp iron", new GKWork("t_iron_ore_1"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Stick", new GKWork("t_stick"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Stone rock", new GKWork("t_stone"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Sand", new GKWork("t_sand"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Clay", new GKWork("t_clai"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Coal", new GKWork("t_coal"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Iron ore", new GKWork("t_iron_ore_2"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Silver ore", new GKWork(null, "p_t_silver_ore"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Gold ore", new GKWork(null, "p_t_gold_ore"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Pyrite", new GKWork(null, "p_t_pyrite"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Sulfur", new GKWork(null, "p_t_sulfur"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Limestone", new GKWork("p_t_lifestone"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Big marble rock", new GKWork("t_marble"));
        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Diamond", new GKWork("t_diamond"));
        // @p_t_sand_improve??
        // p_t_emerald?
        // p_t_marble_gold?
        // p_t_diamond?

        _recipesByName.Add(ItemParser.GATHERING_PREFIX + "Progressive Tree felling",
            new GKProgressiveWork([[ "t_wood_small" ], [ "t_wood_big" ]]));
    }
    
    private void InitializeExtractionRecipeItems()
    {
        var extractions = new[]
        {
            new { name = "Flesh", id = "flesh" },
            new { name = "Skull", id = "skull" },
            new { name = "Bone", id = "bone" },
            new { name = "Skin", id = "skin" },
            new { name = "Blood", id = "blood" },
            new { name = "Fat", id = "fat" },
            new { name = "Brain", id = "brain" },
            new { name = "Heart", id = "heart" },
            new { name = "Intestines", id = "intestine" },
            new { name = "Dark Brain", id = "brain_dark" },
            new { name = "Dark Heart", id = "heart_dark" },
            new { name = "Dark Intestines", id = "intestine_dark" },
        };
        foreach (var item in extractions)
        {
            var recipeName = ItemParser.EXTRACT_PREFIX + item.name;
            var recipeIDs = new List<string>
            {
                $"ex:mf_preparation_1:{item.id}",
                $"ex:mf_preparation_2:{item.id}",
            };
            _recipesByName.Add(recipeName, new GKRecipe(recipeIDs));
        }
    }

    private void InitializeFarmingRecipeItems()
    {
        _recipesByName.Add(ItemParser.RECIPE_PREFIX + "Honey", new GKRecipe("honey_production"));
        _recipesByName.Add(ItemParser.RECIPE_PREFIX + "Grapes", new GKRecipe("garden_grapes_growing"));
        _recipesByName.Add(ItemParser.RECIPE_PREFIX + "Hops", new GKRecipe("garden_hop_growing"));
    }

    private void InitializeCookingRecipeItems()
    {
        InitializeNonTavernCookingRecipeItems();
        InitializeTavernCookingRecipeItems();
    }

    private void InitializeNonTavernCookingRecipeItems()
    {
        var nonTavernCookings = new[]
        {
            new { name = "Grated carrot", id = "grated_carrot" },
            new { name = "Grated beets", id = "grated_beetroot" },
            new { name = "Boiled egg", id = "boiled_egg" },
            new { name = "A mug of mead", id = "cup_mead" },
            new { name = "A mug of beer", id = "cup_beer" },
            new { name = "Vegetable salad", id = "vegetable_salad" },
            new { name = "Vegetable stew", id = "vegetable_stew" },
            new { name = "Creamy vegetable soup", id = "creamy_vegetable_soup" },
            new { name = "Cheese", id = "cheese" },
            new { name = "Butter", id = "butter" },
            new { name = "Cream of mushroom soup", id = "cream_of_mushroom_soup" },
            new { name = "Lentil porridge", id = "lentil_porridge" },
            new { name = "Fancy lentil soup", id = "greek_lentil_soup" },
            new { name = "Lentil cutlets", id = "lentil_cutlets" },
            new { name = "Vegetable patty", id = "vegetable_patty" },
            new { name = "Mushroom patty", id = "mushroom_patty" },
            new { name = "Cheese patty", id = "cheese_patty" },
            new { name = "Cheese pie", id = "cheese_pie" },
            new { name = "Cheesecake", id = "cheesecake" },
            new { name = "Panna cotta", id = "panna_cotta" },
            new { name = "Honey pudding", id = "honey_pudding" },
            new { name = "Berry pudding", id = "berry_pudding" },
            new { name = "Honey cake", id = "honey_cake" },
        };
        foreach (var item in nonTavernCookings)
        {
            var recipeName = ItemParser.RECIPE_PREFIX + item.name;
            _recipesByName.Add(recipeName, new GKRecipe(item.id));
        }
    }
    
    private void InitializeTavernCookingRecipeItems()
    {
        var tavernCookings = new[]
        {
            new { name = "Baked mushrooms", id = "baked_kebab_7" },
            new { name = "Baked apple", id = "baked_apple" },
            new { name = "Carrot cutlet", id = "cutlet_carrot" },
            new { name = "A bowl of sauerkraut", id = "bowl_sauerkraut" },
            new { name = "Beet slices", id = "beet_slice" },
            new { name = "Green jelly", id = "jelly_green" },
            new { name = "Red jelly", id = "jelly_red" },
            new { name = "Dough", id = "dough" },
            new { name = "Pastry dough", id = "dough_2" },
            new { name = "Bread", id = "bread" },
            new { name = "Baked fish", id = "baked_fish" },
            new { name = "Fish soup", id = "soup_fish" },
            new { name = "Fish nuggets", id = "nuggets_fish" },
            new { name = "Pancakes", id = "pancakes" },
            new { name = "Croissant", id = "croissant" },
            new { name = "Muffin", id = "muffin" },
            new { name = "Cabbage soup", id = "soup_red_green" },
            new { name = "Pumpkin soup", id = "soup_red_yellow" },
            new { name = "Vegetable soup", id = "soup_yellow_green" },
            new { name = "Omelette", id = "omlette" },
            new { name = "Fried egg", id = "fried_egg" },
            new { name = "Lasagna", id = "lasagne" },
            new { name = "Pasta", id = "pasta" },
            new { name = "Baked kebab (Pumpkin)", id = "baked_kebab_1" },
            new { name = "Baked kebab (Onions)", id = "baked_kebab_2" },
            new { name = "Baked kebab (Mushrooms)", id = "baked_kebab_5" },
            new { name = "Berry pie", id = "pie_1" },
            new { name = "Grape pie", id = "pie_2" },
            new { name = "A bowl of pumpkin soup", id = "bowl_pumpkin" },
            new { name = "Baked pumpkin", id = "baked_pumpkin" },
            new { name = "A bowl of lentils", id = "bowl_lentil" },
            new { name = "Burger", id = "burger" },
            new { name = "Sandwich", id = "sandwich" },
            new { name = "Baked meat", id = "baked_meat" },
            new { name = "Onion rings", id = "onion_ring" },
            new { name = "Toasts with onions", id = "toasts" },
            new { name = "Baked salmon", id = "baked_salmon" },
            new { name = "Royal fish", id = "honey_fish" },
            new { name = "Cake", id = "pie" },
            // t_flour_from_wheat
            // t_fish_*_fillet
            // t_raw_meat_sliced_from_*
            // t_potion_berries_juice
        };
        foreach (var item in tavernCookings)
        {
            var recipeName = ItemParser.RECIPE_PREFIX + item.name;
            var recipeIDs = new List<string>
            {
                item.id,
                $"@t_{item.id}",
            };
            _recipesByName.Add(recipeName, new GKRecipe(recipeIDs));
        }
    }

    private void InitializePrayerRecipeItems()
    {
        var prayers = new[]
        {
            new { name = "Prayer for repose", id = "skull" },
            new { name = "Prayer for faith", id = "faith" },
            new { name = "Prayer for prosperity", id = "village" }, //?
            new { name = "Prayer for repentance", id = "sins" }, //?
            new { name = "Prayer for donations", id = "money" },
            new { name = "Combo prayer", id = "faith_money" },
            new { name = "Prayer for imagination", id = "pen" },
            new { name = "Prayer for shoots and roots", id = "plant" },
            new { name = "Prayer for retribution", id = "sword" },
            new { name = "Prayer for protection", id = "shield" },
            new { name = "Prayer for excellence", id = "star" },
            new { name = "Prayer for souls' repose", id = "grat_points_incr"},
            new { name = "Prayer for souls' contentment", id = "souls" },
            new { name = "Prayer for souls' thorough cleansing", id = "sin_shard" },
        };
        foreach (var item in prayers)
        {
            var recipeName = ItemParser.RECIPE_PREFIX + item.name;
            var recipeIDs = new List<string>
            {
                $"b_{item.id}",
                $"@b_{item.id}_2",
            };
            _recipesByName.Add(recipeName, new GKRecipe(recipeIDs));
        }
    }

    private void InitializeInjectionRecipeItems()
    {
        var injections = new[]
        {
            new { name = "Acid Injection", id = "-1_-1" },
            new { name = "Lye Injection", id = "1_1" },
            new { name = "Glue Injection", id = "0_1" },
            new { name = "Preservative Injection", id = "stop" },
            new { name = "Restore Injection", id = "50" },
            new { name = "Dark Injection", id = "2_0" },
            new { name = "Silver Injection", id = "-1_1" },
            new { name = "Gold Injection", id = "-2_2" },
        };
        foreach (var item in injections)
        {
            var recipeName = ItemParser.RECIPE_PREFIX + item.name;
            _recipesByName.Add(recipeName, new GKRecipe($"embalm_{item.id}"));
        }
    }

    private void InitializeBagRecipeItems()
    {
        var bags = new[]
        {
            new { name = "Alchemist's bag", id = "alchemy" },
            new { name = "Farmer's bag", id = "farming" },
            new { name = "Fisherman's bag", id = "fishing" },
            new { name = "Tool bag", id = "tools" },
            new { name = "Universal bag", id = "universal" },
            new { name = "Potion bag", id = "potions" },
            new { name = "Builder's bag", id = "builder" },
            new { name = "Food bag", id = "food" },
            new { name = "Big universal bag", id = "universal_big" },
        };
        foreach (var item in bags)
        {
            var recipeName = ItemParser.RECIPE_PREFIX + item.name;
            _recipesByName.Add(recipeName, new GKRecipe($"bag_{item.id}"));
        }
    }

    private void InitializeSmithingRecipeItems()
    {
        var smithingRecipes = new[]
        {
            new { name = "Nails", ids = new[]{"nails"} },
            new { name = "Lens", ids = new[]{"lense"} },
            new { name = "Silver Ingot", ids = new[]{"ingot_silver", "@ingot_silver_2"} },
            new{ name = "Gold Ingot", ids = new[]{"ingot_gold", "@ingot_gold_2"} },
        };
        foreach (var item in smithingRecipes)
        {
            var recipeName = ItemParser.RECIPE_PREFIX + item.name;
            var recipeIDs = item.ids.ToList();
            _recipesByName.Add(recipeName, new GKRecipe(recipeIDs));
        }
    }

    private void InitializeBuildingRecipeItems()
    {
        var buildingRecipes = new[]
        {
            new { name = "Flitch", ids = new[] { "flitch" } },
            new { name = "Wood billet", ids = new[] { "wood1" } },
            new { name = "Firewood", ids = new[] { "firewood" } },
            new { name = "Wood wedge", ids = new[] { "spike_1" } },
            new { name = "Wooden plank", ids = new[] { "wooden_plank", "@wooden_plank_3" } },
            new { name = "Wood repair kit", ids = new[] { "repair_wdn", "@repair_wdn_1" } },
            new { name = "Jointing", ids = new[] { "wood_constr_1" } },
            new { name = "Wooden beam", ids = new[] { "wood_balk_1" } },
            new { name = "Carved wood", ids = new[] { "carved_wood" } },

            new { name = "A piece of stone", ids = new[] { "stone_plate_1", "@stone_plate_1_2" } },
            new { name = "Stone repair kit", ids = new[] { "repair_stn", "@repair_stn_2" } },
            new
            {
                name = "A polished brick of stone",
                ids = new[] { "stone_plate_2", "@stone_plate_2_2", "@stone_plate_2_3", "@stone_plate_2_4" }
            },
            new { name = "A carved piece of stone", ids = new[] { "stone_plate_3", "@stone_plate_3b" } },

            new { name = "A piece of marble", ids = new[] { "marble_plate_1", "@marble_plate_1_2" } },
            new { name = "Marble repair kit", ids = new[] { "repair_mrb", "@repair_mrb_2" } },
            new { name = "A polished brick of marble", ids = new[] { "marble_plate_2", "@marble_plate_2_2" } },
            new { name = "A carved piece of marble", ids = new[] { "marble_plate_3" } },

            new { name = "Ceramic bowls", ids = new[] { "ceramic_1" } },
            new { name = "Polishing paste", ids = new[] { "polishing_paste", "@polishing_paste_2" } },
            new { name = "Ceramic jug", ids = new[] { "ceramic_2", "@ceramic_2_1", "@ceramic_2_2" } },
            new { name = "Porcelain pitcher", ids = new[] { "ceramic_3", "@ceramic_3_1", "@ceramic_3_2" } },
            new { name = "Graphite", ids = new[] { "graphite", "@graphite_2" } },
        };
        foreach (var item in buildingRecipes)
        {
            var recipeName = ItemParser.RECIPE_PREFIX + item.name;
            var recipeIDs = item.ids.ToList();
            _recipesByName.Add(recipeName, new GKRecipe(recipeIDs));
        }
    }

    private void InitializeSpiritualismRecipeItems()
    {
        var spiritualismRecipes = new[]
        {
            new { name = "Remote craft control", ids = new[] { "fake_global_craft" } },
            new { name = "Story (Organ workbench)", ids = new[] { "story_souls" } },
            new { name = "Chapter (Organ workbench)", ids = new[] { "chapter_souls" } },
            new { name = "Book (Organ workbench)", ids = new[] { "book_souls" } },
            new { name = "Boost fertilizer I (Organ workbench)", ids = new[] { "sack_clock_silver_souls" } },
            new { name = "Boost fertilizer II (Organ workbench)", ids = new[] { "@sack_clock_gold_souls" } },
            new { name = "Quality fertilizer I (Organ workbench)", ids = new[] { "sack_star_silver_souls" } },
            new { name = "Quality fertilizer II (Organ workbench)", ids = new[] { "@sack_star_gold_souls" } },
        };
        foreach (var item in spiritualismRecipes)
        {
            var recipeName = ItemParser.RECIPE_PREFIX + item.name;
            var recipeIDs = item.ids.ToList();
            _recipesByName.Add(recipeName, new GKRecipe(recipeIDs));
        }
    }

    private void InitializeQuestRecipeItems()
    {
        var items = new []
        {
            new { name = "Roof tile", ids = new[]{"roof_tile_1", "roof_tile_2", "roof_tile_3"} },
            new { name = "Mysterious food sauce", ids = new[]{"sauce_for_meal"} },
        };
        foreach (var item in items)
        {
            var recipeName = ItemParser.RECIPE_PREFIX + item.name;
            var recipeIDs = item.ids.ToList();
            _recipesByName.Add(recipeName, new GKRecipe(recipeIDs));
        }
    }
    
    private void InitializeNormalRecipeItems()
    {
        var items = new[]
        {
            new { name = "Iron hammer", ids = new[]{"hammer_2"} },
            new { name = "Wodden grave fence", ids = new[]{"grave_bot_wd_1", "@destroy_grave_bot_wd_1"} },
            new { name = "Ceramic funeral urn", ids = new[]{"funeral_urn_1"} },
            new { name = "Porcelain funeral urn", ids = new[]{"funeral_urn_1"} },
            new { name = "Candle (1)", ids = new[]{"candle_1"} },
            new { name = "Candle (3)", ids = new[]{"candle_3"} },
            new { name = "Candle (4)", ids = new[]{"candle_4"} },
            new { name = "Flyer", ids = new[]{"flyer_bad", "@flyer_good", "@flyer_good_2"} },
            new { name = "Incense", ids = new[]{"incense_1"} },
            new { name = "Incense II", ids = new[]{"incense_2"} },
            new { name = "Ink", ids = new[]{"ink_jar"} },
            new { name = "Pen and Ink", ids = new[]{"ink_pen"} },
            new { name = "Pigskin paper", ids = new[]{"skroll_skin_pig", "@skroll_skin_pig_2"} },
            new { name = "Paper glop", ids = new[]{"pail_wet_paper"} },
            new { name = "Clean paper", ids = new[]{"paper_bad, paper_from_herb"} },
            new { name = "Notes", ids = new[]{"notes"} },
            new { name = "Chapter", ids = new[]{"chapter"} },
            new { name = "Story", ids = new[]{"story"} },
            new { name = "Soft cover", ids = new[]{"cover_1"} },
            new { name = "Hard cover", ids = new[]{"cover_hard", "@cover_hard_2", "@cover_hard_3"} },
            new { name = "Book", ids = new[]{"book_hard", "@book_hard_2"} },
            new { name = "Peat", ids = new[]{"peat_from_waste"} },
            new { name = "Boost fertilizer I", ids = new[]{"sack_clock_silver"} },
            new { name = "Boost fertilizer II", ids = new[]{"sack_clock_gold"} },
            new { name = "Quality fertilizer I", ids = new[]{"sack_star_silver"} },
            new { name = "Quality fertilizer II", ids = new[]{"sack_star_gold"} },
            
            new { name = "Bottle of apple ferment", ids = new[]{"bottle_apple_braga"} },
            new { name = "Bottle of berry ferment", ids = new[]{"bottle_berry_braga"} },
            new { name = "Red wine", ids = new[]{"bottle_red_vine"} },
            new { name = "Booze", ids = new[]
            {
                "booze_from_bottle_apple_braga",
                "@booze_from_bottle_berry_braga",
                "@booze_from_bottle_red_vine_1",
                "@booze_from_bottle_red_vine_2",
                "@booze_from_bottle_red_vine_3"
            } },
        };
        foreach (var item in items)
        {
            var recipeName = ItemParser.RECIPE_PREFIX + item.name;
            var recipeIDs = item.ids.ToList();
            _recipesByName.Add(recipeName, new GKRecipe(recipeIDs));
        }
    }
    
    private void InitializeProgressiveRecipeItems()
    {
        InitializeProgressiveToolRecipeItems();
        InitializeProgressiveSmithingRecipeItems();
        InitializeProgressiveGraveMarkerRecipeItems();
    }
    
    private void InitializeProgressiveToolRecipeItems()
    {
        var progressiveTools = new[]
        {
            new
            {
                name = "Progressive Shovel", ids = new[]
                {
                    new[] { "shovel_1", "@shovel_1_2", "@shovel_1_3" },
                    new[] { "shovel_2" },
                }
            },
            new
            {
                name = "Progressive Pickaxe", ids = new[]
                {
                    new[] { "pickaxe_1", "@pickaxe_1_2", "@pickaxe_1_3" },
                    new[] { "pickaxe_2" },
                }
            },
            new
            {
                name = "Progressive Axe", ids = new[]
                {
                    new[] { "axe_1", "@axe_1_2", "@axe_1_3" },
                    new[] { "axe_2" },
                }
                
            },
            new
            {
                name = "Progressive Hammer", ids = new[]
                {
                    new[] { "@hammer_0" },
                    new[] { "hammer_2" },
                }
            },
            new
            {
                name = "Progressive Chisel", ids = new[]
                {
                    new[] { "chisel_1", "@chisel_1_2" },
                    new[] { "chisel_2", "@chisel_2_2", "@chisel_2a", "@chisel_2b", "@chisel_2_2a", "@chisel_2_2b" },
                }
            },
            new
            {
                name = "Progressive Sword", ids = new[]
                {
                    new[] { "sword_1", "@sword_1_2" },
                    new[] { "sword_steel" },
                    new[] { "sword_steel_gem" },
                    new[] { "sword_damask_gem" },
                }
            },
            new
            {
                name = "Progressive Armor", ids = new[]
                {
                    new[] { "armor_lamellar_1", "@armor_lamellar_1_2" },
                    new[] { "armor_lamellar_2" },
                }
            }
        };
        foreach (var item in progressiveTools)
        {
            var recipeName = ItemParser.RECIPE_PREFIX + item.name;
            var progressiveIDs = item.ids.Select(row => row.ToList()).ToList();
            _recipesByName.Add(recipeName, new GKProgressiveRecipe(progressiveIDs));
        }
    }

    private void InitializeProgressiveSmithingRecipeItems()
    {
        var progressiveSmithingRecipes = new[]
        {
            new
            {
                name = "Progressive Ingot", ids = new[]
                {
                    new[] { "ingot_metal" }, //Fireman: "@ingot_metal_huge", "@ingot_metal_1_huge", "@ingot_metal_2_huge"
                    new[] { "ingot_steel", "@ingot_steel_2" },
                }
            },
            new
            {
                name = "Progressive Metal Parts", ids = new[]
                {
                    new[] { "detail_1" },
                    new[] { "detail_2" },
                    new[] { "detail_3" },
                }
            },
            new
            {
                name = "Progressive Glass", ids = new[]
                {
                    new[] { "glass_0", "@glass_0_1", "@glass_0_2" },
                    new[] { "glass_1", "@glass_1_2" },
                    new[] { "glass_2", "@glass_2_2" },
                }
            },
            new
            {
                name = "Progressive Jewelry", ids = new[]
                {
                    new[] { "jewelry_detail_gold" },
                    new[] { "bijouterie_gold" },
                }
            }
        };
        foreach (var item in progressiveSmithingRecipes)
        {
            var recipeName = ItemParser.RECIPE_PREFIX + item.name;
            var progressiveIDs = item.ids.Select(row => row.ToList()).ToList();
            _recipesByName.Add(recipeName, new GKProgressiveRecipe(progressiveIDs));
        }
    }
    
    private void InitializeProgressiveGraveMarkerRecipeItems()
    {
        var progressiveGraveMarkers = new[]
        {
            new
            {
                name = "Progressive Wooden Grave Marker", ids = new[]
                {
                    new[] { "grave_top_wd_tab_1", "@grave_top_wd_tab_1_2", "@grave_top_wd_tab_1_3" },
                    new[] { "grave_top_wd_cross_1", "@grave_top_wd_cross_1_2" },
                }
            },
            new
            {
                name = "Progressive Stone Grave Marker", ids = new[]
                {
                    new[] { "grave_top_stn_plate_1", "@grave_top_stn_plate_1_2", "@destroy_grave_top_stn_plate_1" },
                    new[] { "grave_top_stn_plate_2", "@destroy_grave_top_stn_plate_2" },
                    new[] { "grave_top_stn_cross_1", "@destroy_grave_top_stn_cross_1" },
                    new[] { "grave_top_memorial_stn_1" },
                    new[] { "grave_top_stn_cross_2", "@destroy_grave_top_stn_cross_2" },
                    new[] { "grave_top_stella_stn_1", "@destroy_grave_top_stella_stn_1" },
                    new[] { "grave_top_sculpt_stn_1", "@destroy_top_sculpt_stn_1" },
                    new[] { "grave_top_sculpt_stn_2", "@destroy_top_sculpt_stn_2" },
                    new[] { "grave_top_womansaver_stn_1" },
                    new[] { "grave_top_highangel_stn_1" },
                    // yes, there is no ...stn_3, ...stn_4 is V and ...stn_5 is VI in game 
                    new[] { "grave_top_sculpt_stn_4", "@destroy_top_sculpt_stn_4" },
                    new[] { "grave_top_sculpt_stn_5", "@destroy_top_sculpt_stn_5" },
                }
            },
            new
            {
                name = "Progressive Marble Grave Marker", ids = new[]
                {
                    new[] { "grave_top_mrb_cross_1" },
                    new[] { "grave_top_mrb_cross_2" },
                    new[] { "grave_top_memorial_mrb_1" },
                    new[] { "grave_top_stella_mrb_1" },
                    new[] { "grave_top_sculpt_mrb_1" },
                    new[] { "grave_top_sculpt_mrb_2" },
                    new[] { "grave_top_womansaver_mrb_1" },
                    new[] { "grave_top_highangel_mrb_1" },
                    // yes, there is no ...mrb_3, ...mrb_4 is V and ...mrb_5 is VI in game
                    new[] { "grave_top_sculpt_mrb_4" },
                    new[] { "grave_top_sculpt_mrb_5" },
                }
            },
            new
            {
                name = "Progressive Stone Grave Fence", ids = new[]
                {
                    new[] { "grave_bot_stn_1", "@grave_bot_stn_1_2", "@destroy_grave_bot_stn_1" },
                    new[] { "grave_bot_stn_2", "@destroy_grave_bot_stn_2" },
                    new[] { "grave_bot_stn_3" },
                    new[] { "grave_bot_stn_4" },
                    new[] { "grave_bot_stn_5" },
                    new[] { "grave_bot_stn_6" },
                    new[] { "grave_bot_stn_7" },
                    new[] { "grave_bot_stn_8" },
                }
            },
            new
            {
                name = "Progressive Marble Grave Fence", ids = new[]
                {
                    new[] { "grave_bot_mrb_1" },
                    new[] { "grave_bot_mrb_2" },
                    new[] { "grave_bot_mrb_3" },
                    new[] { "grave_bot_mrb_4" },
                    new[] { "grave_bot_mrb_5" },
                    new[] { "grave_bot_mrb_6" },
                    new[] { "grave_bot_mrb_7" },
                    new[] { "grave_bot_mrb_8" },
                }
            }
        };
        foreach (var item in progressiveGraveMarkers)
        {
            var recipeName = ItemParser.RECIPE_PREFIX + item.name;
            var progressiveIDs = item.ids.Select(row => row.ToList()).ToList();
            _recipesByName.Add(recipeName, new GKProgressiveRecipe(progressiveIDs));
        }
    }
    
    private void InitializePerkItems()
    {
        _perksByName.Add(ItemParser.PERK_PREFIX + "Butcher", new GKPerk("p_butcher"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Surgeon", new GKPerk("p_doctor"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Scientist", new GKPerk("p_scientist"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Cultist", new GKPerk("p_cultist"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Preacher", new GKPerk("p_preacher"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Cardinal", new GKPerk("p_cardinal"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Curious mind", new GKPerk("p_naturalist"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Journalist", new GKPerk("p_journalist"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Writer", new GKPerk("p_writer"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Playwright", new GKPerk("p_good_writer"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Master gatherer", new GKPerk("p_collector", null, "t_mushroom2"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Farmer", new GKPerk("p_farmer"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Beekeeper", new GKPerk("p_beekeeper2"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Wine master", new GKPerk("p_wine_master"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Blacksmith", new GKPerk("p_blacksmith"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Big buy", new GKPerk("p_big_buy"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Fireman", new GKPerk("p_fireman", ["@ingot_metal_huge","@ingot_metal_1_huge","@ingot_metal_2_huge"]));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Engineer", new GKPerk("p_engineer"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Sword master", new GKPerk("p_sword_master"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Axeman", new GKPerk("p_axeman", null, "t_wood_big"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Miner", new GKPerk("p_miner"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Carpenter", new GKPerk("p_woodworker"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Mason", new GKPerk("p_mason"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Jeweler", new GKPerk("p_jevelery"));
        // better save soul
        _perksByName.Add(ItemParser.PERK_PREFIX + "Eloquence", new GKPerk("p_eloquence"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Industriousness", new GKPerk("p_industriousness"));
        _perksByName.Add(ItemParser.PERK_PREFIX + "Persistence", new GKPerk("p_persistence"));
    }
    
    private void InitializeRelationItems()
    {
        var npcs = new []
        {
            // Main
            new{ name = "Astrologer", id = "npc_astrologer" },
            new{ name = "Bishop", id = "npc_bishop" },
            new{ name = "Inquisitor", id = "npc_inquisitor" },
            new{ name = "Merchant", id = "npc_merchant" },
            new{ name = "Ms. Charm", id = "npc_actress" },
            new{ name = "Snake", id = "npc_cultist" },
            // Village
            new{ name = "Adam", id = "npc_potter" },
            new{ name = "Beekeeper", id = "npc_beekeeper" },
            new{ name = "Dig", id = "npc_dig" },
            new{ name = "Farmer", id = "npc_farmer" },
            new{ name = "Horadric", id = "npc_tavern owner" },
            new{ name = "Krezvold", id = "npc_blacksmith" },
            new{ name = "Miller", id = "npc_miller" },
            new{ name = "Miss Chain", id = "npc_mrs_chain" },
            new{ name = "Woodcutter", id = "npc_wood_cutter" },
            new{ name = "Vagner", id = "npc_actor" },
            // Other
            new{ name = "Clotho", id = "npc_witch" },
            new{ name = "Donkey", id = "donkey" },
            new{ name = "Gerry", id = "crafting_skull_3"},
            new{ name = "Gypsy Baron", id = "npc_gypsy" },
            new{ name = "Koukol", id = "npc_hunchback" },
            new{ name = "Royal Services", id = "npc_royal_box" },
            // Game of Crone DLC
            new{ name = "Cook", id = "npc_refugee_cook" },
            new{ name = "Master Alaric", id = "npc_master_alarich" },
            new{ name = "Marquis Teodoro Jr.", id = "npc_marquis_teodoro_jr" },
            new{ name = "Undertaker", id = "npc_refugee_coffin_maker" },
            new{ name = "Tanner", id = "npc_refugee_tanner" },
            // Better Save Soul DLC
            new{ name = "Euric", id = "npc_euric" }
            // Smiler??
        };
        foreach (var npc in npcs )
        {
            var npcName = ItemParser.PERK_PREFIX + npc.name;
            _relationItemsByName.Add(npcName, new GKRelation(npc.id, 10));
        }
    }

    private void InitializeIngameItems()
    {
        var ingameItems = new[]
        {
            new { name = "Red Research Points", id = "techpoints_red" },
            new { name = "Green Research Points", id = "techpoints_green" },
            new { name = "Blue Research Points", id = "techpoints_blue" },
            
            // quest items
            new { name = "Instructions for Key", id = "quest_instruction_snake" },
            new { name = "Keepers Key", id = "quest_key_astrologer" },
            new { name = "Graveyard Scroll", id = "scroll_graveyard_prop" },
            new { name = "Stamp", id = "stamp" },
            new { name = "Paper with calculations", id = "calculation_for_mill_fix" },
            new { name = "Memory powder", id = "memory_powder" }, // TODO: make unlimited uses, remove text and crafting it; OR: only give recipe
            new { name = "Piece of donkey hair", id = "donkey_lock_wool" },
            new { name = "Casual prayer", id = "" },
            
            // portal items
            new { name = "Salty Fork", id = "fork_salty" },
            new { name = "Mirror of Pride", id = "mirror_of_pride" },
            new { name = "Eternal burning coal", id = "eternal_burning_coal" },
            new { name = "Golden angle", id = "sextant" },
            new { name = "Endless notebook", id = "endless_book" },
            new { name = "Necklace", id = "necklace" },
            
            // better save soul quest items
            new { name = "Old rusty key", id = "souls_zone_key" },
            new { name = "Lost tooth", id = "gerry_tooth" },
            new { name = "Ode for Bishop", id = "ode_for_bishop" },
            new { name = "Pride charged shard", id = "pride_charged_shard" },
            new { name = "Wrath charged shard", id = "wrath_charged_shard" },
            new { name = "Gluttony charged shard", id = "gluttony_charged_shard" },
            new { name = "Sloth charged shard", id = "sloth_charged_shard" },
            new { name = "Lust charged shard", id = "lust_charged_shard" },
            new { name = "Envy charged shard", id = "envy_charged_shard" },
            new { name = "Remote crafting", id = "" },
            new { name = "Unusual ash", id = "ash_on_shawl" },
            new { name = "Note with rumor", id = "note_with_rumors" },
        };
        foreach (var item in ingameItems)
        {
            var itemName = item.name;
            var itemID = item.id;
            _ingameItemsByName.Add(itemName, new GKIngameItem(itemID));
        }
    }

    public IAPItem GetBuffItem(string buffName)
    {
        if(_permanentBuffsByName.TryGetValue(buffName, out var buff))
            return buff;
            
        _logger.LogError($"Could not find permanent buff {buffName}");
        return null;
    }

    public IAPItem GetRecipeItem(string recipeName)
    {
        if(_recipesByName.TryGetValue(recipeName, out var recipe))
            return recipe;
        
        _logger.LogError($"Could not find recipe {recipeName}");
        return null;
    }

    public IAPItem GetPerkItem(string perkName)
    {
        if (_perksByName.TryGetValue(perkName, out var perk))
            return perk;
        
        _logger.LogError($"Could not find perk {perkName}");
        return null;
    }

    public IAPItem GetRelationItem(string npcName, int amount)
    {
        if (_relationItemsByName.TryGetValue(npcName, out var relation))
        {
            Debug.Assert(relation.Amount == amount);
            return relation;
        }

        _logger.LogError($"Could not find npc {npcName}");
        return null;
    }
    
    public bool TryGetIngameItem(string itemName, out GKIngameItem item)
    {
        return _ingameItemsByName.TryGetValue(itemName, out item);
    }
}