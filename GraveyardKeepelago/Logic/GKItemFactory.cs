using System;
using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.GameModifications;
using GraveyardKeepelago.Items;

namespace GraveyardKeepelago.Logic;

public class GKItemFactory
{
    private readonly IPlayerActions _playerActions;

    public GKItemFactory(IPlayerActions playerActions)
    {
        _playerActions = playerActions;
    }

    public void BuildAll(GKItemRegistry registry)
    {
        InitializePermanentBuffItems(registry);
        InitializeRecipeItems(registry);
        InitializePerkItems(registry);
        InitializeRelationItems(registry);
        InitializeIngameItems(registry);
    }

    private void InitializePermanentBuffItems(GKItemRegistry registry)
    {
        var buffs = new[]
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
        foreach (var item in buffs)
        {
            registry.AddBuff("Permanent Buff: " + item.name, new GKPermaBuff(item.id, item.res_types.ToList(), item.icon, _playerActions));
        }
    }

    private void InitializeRecipeItems(GKItemRegistry registry)
    {
        InitializeBlueprintItems(registry);
        InitializeProgressiveBlueprintItems(registry);
        
        InitializeGatheringRecipeItems(registry);
        
        InitializeExtractionRecipeItems(registry);
        InitializeFarmingRecipeItems(registry);
        InitializeCookingRecipeItems(registry);
        InitializePrayerRecipeItems(registry);
        InitializeInjectionRecipeItems(registry);
        InitializeBagRecipeItems(registry);
        InitializeSmithingRecipeItems(registry);
        InitializeBuildingRecipeItems(registry);
        InitializeSpiritualismRecipeItems(registry);
        
        InitializeQuestRecipeItems(registry);
        InitializeNormalRecipeItems(registry);
        
        InitializeProgressiveRecipeItems(registry);
    }

    private void InitializeBlueprintItems(GKItemRegistry registry)
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
            foreach (var bp in kvp.Value)
            {
                registry.AddRecipe("Blueprint: " + bp.Key, new GKRecipe(builddesk + ":" + bp.Value, _playerActions));
            }
        }

        registry.AddRecipe("Blueprint: " + "Lawn", new GKRecipe(new List<string>
        {
            "graveyard_builddesk:p:grass_sward_2x3",
            "@graveyard_builddesk:p:grass_sward_2x4",
            "@graveyard_builddesk:p:grass_sward_2x6"
        }, _playerActions));
        registry.AddRecipe("Blueprint: " + "Trunk", new GKRecipe(new List<string>
        {
            "garden_builddesk:p:mf_box_stuff_place",
            "@mining_builddesk:p:mf_box_stuff_place",
            "@vineyard_builddesk:p:mf_box_stuff_place",
            "@graveyard_builddesk:p:mf_box_stuff_place",
            "@cremation_builddesk:p:mf_box_stuff_place"
        }, _playerActions));
        registry.AddRecipe("Blueprint: " + "Vine press", new GKRecipe(new List<string>
        {
            "mf_wood_builddesk:p:mf_vine_press_place",
            "@cellar_builddesk:p:mf_vine_press_place"
        }, _playerActions));
        registry.AddRecipe("Blueprint: " + "Stone stockpile", new GKRecipe(new List<string>
        {
            "mf_wood_builddesk:p:mf_stones_1_place",
            "@mining_builddesk:p:mf_stones_1_place"
        }, _playerActions));
        registry.AddRecipe("Blueprint: " + "Iron ore stockpile", new GKRecipe(new List<string>
        {
            "mf_wood_builddesk:p:mf_ore_1_complete",
            "@mining_builddesk:p:mf_ore_1_complete"
        }, _playerActions));
        registry.AddRecipe("Blueprint: " + "Zombie sawmill", new GKRecipe(new List<string>
        {
            "zombie_sawmill_unfinished_place"
        }, _playerActions));
        registry.AddRecipe("Blueprint: " + "Zombie ore mine", new GKRecipe(new List<string>
        {
            "mining_builddesk::zombie_mine_bench_left",
            "@mining_builddesk::zombie_mine_bench_right"
        }, _playerActions));
        registry.AddRecipe("Blueprint: " + "Zombie stone mine", new GKRecipe(new List<string>
        {
            "mining_builddesk::zombie_mine_fence_front_stone",
            "@mining_builddesk::zombie_mine_fence_left_front_stone"
        }, _playerActions));
        registry.AddRecipe("Blueprint: " + "Zombie marble mine", new GKRecipe(new List<string>
        {
            "mining_builddesk::zombie_mine_fence_front_marble_right",
            "@mining_builddesk::zombie_mine_fence_front_marble_left"
        }, _playerActions));
    }

    private void InitializeProgressiveBlueprintItems(GKItemRegistry registry)
    {
        var progressiveBlueprints = new[]
        {
            new { name = "Progressive Preparation Place", ids = new[]
            {
                new[] { "morgue_builddesk:p:mf_preparation_1_place" },
                new[] { "morgue_builddesk:p:mf_preparation_2_place" }
            } },
            new { name = "Progressive Pallet", ids = new[]
            {
                new[] { "morgue_builddesk:p:corpse_bed_place" },
                new[] { "morgue_builddesk:p:corpse_bed_big_place" },
                new[] { "morgue_builddesk:p:corpse_fridge_place" }
            } },
            new { name = "Progressive Embalming Table", ids = new[]
            {
                new[] { "morgue_builddesk:p:mf_balsamation_1_place" },
                new[] { "morgue_builddesk:p:mf_balsamation_2_place" }
            } },
            new { name = "Progressive Distillation Cube", ids = new[]
            {
                new[] { "alchemy_builddesk:p:mf_distcube_2_clay_place" },
                new[] { "alchemy_builddesk:p:mf_distcube_2_cuprum_place", "@upgr_to_mf_distcube_2_cuprum" }
            } },
            new { name = "Progressive Candelabra", ids = new[]
            {
                new[] { "church_builddesk:p:candelabrum_1", "@church_builddesk:p:wall_candelabrum_1" },
                new[] { "church_builddesk:p:candelabrum_2", "@church_builddesk:p:wall_candelabrum_2" },
                new[] { "church_builddesk:p:candelabrum_3", "@church_builddesk:p:wall_candelabrum_3" }
            } },
            new { name = "Progressive Church Bench", ids = new[]
            {
                new[] { "church_builddesk:p:church_bench" },
                new[] { "church_builddesk:p:church_bench_2" }
            } },
            new { name = "Progressive Confessional", ids = new[]
            {
                new[] { "church_builddesk:p:church_budka_1_place" },
                new[] { "church_builddesk:p:church_budka_2_place" }
            } },
            new { name = "Progressive Church Shrine", ids = new[]
            {
                new[] { "church_builddesk:p:church_table_1_place" },
                new[] { "church_builddesk:p:church_table_2_place" }
            } },
            new { name = "Progressive Incense Burner", ids = new[]
            {
                new[] { "church_builddesk:p:c_obj_incense_1_place" },
                new[] { "church_builddesk:p:c_obj_incense_2_place" }
            } },
            new { name = "Progressive Prayer Station", ids = new[]
            {
                new[] { "graveyard_builddesk:p:pray_stand_wd_place" },
                new[] { "graveyard_builddesk:p:pray_stand_stn_place" }
            } },
            new { name = "Progressive Desk", ids = new[]
            {
                new[] { "alchemy_builddesk:p:desk" },
                new[] { "alchemy_builddesk:p:desk_2" }
            } },
            new { name = "Progressive Printing Press", ids = new[]
            {
                new[] { "alchemy_builddesk:p:mf_printing_press_place" },
                new[] { "alchemy_builddesk:p:mf_printing_press_2_place", "@mf_printing_press_1_to_2" }
            } },
            new { name = "Progressive Furnace", ids = new[]
            {
                new[] { "mf_wood_builddesk:p:mf_furnace_0_place" },
                new[] { "mf_wood_builddesk:p:mf_furnace_1_place", "@mf_furnace_0_to_1" },
                new[] { "mf_wood_builddesk:p:mf_furnace_2_place", "@mf_furnace_1_to_2" }
            } },
            new { name = "Progressive Anvil", ids = new[]
            {
                new[] { "mf_wood_builddesk:p:mf_anvil_1" },
                new[] { "mf_wood_builddesk:p:mf_anvil_2" },
                new[] { "mf_wood_builddesk:p:mf_anvil_3", "@mf_anvil_2_to_3" }
            } },
            new { name = "Progressive Saw", ids = new[]
            {
                new[] { "mf_wood_builddesk:p:mf_timber_1_place" },
                new[] { "mf_wood_builddesk:p:mf_saw_1_place" }
            } },
            new { name = "Progressive Stone Cutter", ids = new[]
            {
                new[] { "mf_wood_builddesk:p:mf_hammer_0_place", "@mining_builddesk:p:mf_hammer_0_place" },
                new[] { "mf_wood_builddesk:p:mf_hammer_1_place" }
            } },
            new { name = "Progressive Carpenter's Workbench", ids = new[]
            {
                new[] { "mf_wood_builddesk:p:mf_workbench_1_place" },
                new[] { "mf_wood_builddesk:p:mf_workbench_2_place", "@mf_workbench_1_to_2" }
            } },
            new { name = "Progressive Soul Container", ids = new[]
            {
                new[] { "souls_builddesk:p:soul_container_place" },
                new[] { "souls_builddesk:p:soul_container_2_place" },
                new[] { "souls_builddesk:p:soul_container_3_place" }
            } },
            new { name = "Progressive Soul Extractor", ids = new[]
            {
                new[] { "souls_builddesk::soul_extractor_upgrd_to_2" },
                new[] { "souls_builddesk::soul_extractor_upgrd_to_3" }
            } },
        };
        foreach (var item in progressiveBlueprints)
        {
            var recipeName = "Blueprint: " + item.name;
            var progressiveIDs = item.ids.Select(row => row.ToList()).ToList();
            registry.AddRecipe(recipeName, new GKProgressiveRecipe(progressiveIDs, _playerActions));
        }
    }

    private void InitializeGatheringRecipeItems(GKItemRegistry registry)
    {
        registry.AddRecipe("Gathering: " + "Old books", new GKWork(null, "p_t_old_books", _playerActions));
        registry.AddRecipe("Gathering: " + "Edible mushroom", new GKWork("t_mushroom", null, _playerActions));
        registry.AddRecipe("Gathering: " + "Berry", new GKWork("t_berry", null, _playerActions));
        registry.AddRecipe("Gathering: " + "Apple", new GKWork("t_apple", null, _playerActions));
        registry.AddRecipe("Gathering: " + "Beeswax", new GKWork(null, "p_t_beeswax", _playerActions));
        registry.AddRecipe("Gathering: " + "Butterfly", new GKWork(null, "p_t_butterfly", _playerActions));
        registry.AddRecipe("Gathering: " + "Moth", new GKWork(null, "p_t_moth", _playerActions));
        registry.AddRecipe("Gathering: " + "Bee", new GKWork(null, "p_t_bee", _playerActions));
        registry.AddRecipe("Gathering: " + "Maggot", new GKWork(null, "p_t_maggot", _playerActions));
        registry.AddRecipe("Gathering: " + "Swamp iron", new GKWork("t_iron_ore_1", null, _playerActions));
        registry.AddRecipe("Gathering: " + "Stick", new GKWork("t_stick", null, _playerActions));
        registry.AddRecipe("Gathering: " + "Stone rock", new GKWork("t_stone", null, _playerActions));
        registry.AddRecipe("Gathering: " + "Sand", new GKWork("t_sand", null, _playerActions));
        registry.AddRecipe("Gathering: " + "Clay", new GKWork("t_clai", null, _playerActions));
        registry.AddRecipe("Gathering: " + "Coal", new GKWork("t_coal", null, _playerActions));
        registry.AddRecipe("Gathering: " + "Iron ore", new GKWork("t_iron_ore_2", null, _playerActions));
        registry.AddRecipe("Gathering: " + "Silver ore", new GKWork(null, "p_t_silver_ore", _playerActions));
        registry.AddRecipe("Gathering: " + "Gold ore", new GKWork(null, "p_t_gold_ore", _playerActions));
        registry.AddRecipe("Gathering: " + "Pyrite", new GKWork(null, "p_t_pyrite", _playerActions));
        registry.AddRecipe("Gathering: " + "Sulfur", new GKWork(null, "p_t_sulfur", _playerActions));
        registry.AddRecipe("Gathering: " + "Limestone", new GKWork("p_t_lifestone", null, _playerActions));
        registry.AddRecipe("Gathering: " + "Big marble rock", new GKWork("t_marble", null, _playerActions));
        registry.AddRecipe("Gathering: " + "Diamond", new GKWork("t_diamond", null, _playerActions));
        // @p_t_sand_improve??
        // p_t_emerald?
        // p_t_marble_gold?
        // p_t_diamond?
        
        registry.AddRecipe("Gathering: " + "Progressive Tree felling",
            new GKProgressiveWork(new List<List<string>> { new() { "t_wood_small" }, new() { "t_wood_big" } }, _playerActions));
    }

    private void InitializeExtractionRecipeItems(GKItemRegistry registry)
    {
        var extractions = new[]
        {
            ("Flesh", "flesh"),
            ("Skull", "skull"),
            ("Bone", "bone"),
            ("Skin", "skin"),
            ("Blood", "blood"),
            ("Fat", "fat"),
            ("Brain", "brain"),
            ("Heart", "heart"),
            ("Intestines", "intestine"),
            ("Dark Brain", "brain_dark"),
            ("Dark Heart", "heart_dark"),
            ("Dark Intestines", "intestine_dark")
        };
        foreach (var (name, id) in extractions)
        {
            var recipeName = "Extract: " + name;
            registry.AddRecipe(recipeName, new GKRecipe(new List<string>
            {
                $"ex:mf_preparation_1:{id}",
                $"ex:mf_preparation_2:{id}"
            }, _playerActions));
        }
    }

    private void InitializeFarmingRecipeItems(GKItemRegistry registry)
    {
        registry.AddRecipe("Create: " + "Honey", new GKRecipe("honey_production", _playerActions));
        registry.AddRecipe("Create: " + "Grapes", new GKRecipe("garden_grapes_growing", _playerActions));
        registry.AddRecipe("Create: " + "Hops", new GKRecipe("garden_hop_growing", _playerActions));
    }

    private void InitializeCookingRecipeItems(GKItemRegistry registry)
    {
        var nonTavernCookings = new[]
        {
            ("Grated carrot", "grated_carrot"),
            ("Grated beets", "grated_beetroot"),
            ("Boiled egg", "boiled_egg"),
            ("A mug of mead", "cup_mead"),
            ("A mug of beer", "cup_beer"),
            ("Vegetable salad", "vegetable_salad"),
            ("Vegetable stew", "vegetable_stew"),
            ("Creamy vegetable soup", "creamy_vegetable_soup"),
            ("Cheese", "cheese"),
            ("Butter", "butter"),
            ("Cream of mushroom soup", "cream_of_mushroom_soup"),
            ("Lentil porridge", "lentil_porridge"),
            ("Fancy lentil soup", "greek_lentil_soup"),
            ("Lentil cutlets", "lentil_cutlets"),
            ("Vegetable patty", "vegetable_patty"),
            ("Mushroom patty", "mushroom_patty"),
            ("Cheese patty", "cheese_patty"),
            ("Cheese pie", "cheese_pie"),
            ("Cheesecake", "cheesecake"),
            ("Panna cotta", "panna_cotta"),
            ("Honey pudding", "honey_pudding"),
            ("Berry pudding", "berry_pudding"),
            ("Honey cake", "honey_cake")
        };
        foreach (var (name, id) in nonTavernCookings)
        {
            registry.AddRecipe("Create: " + name, new GKRecipe(id, _playerActions));
        }

        var tavernItems = new[]
        {
            ("Baked mushrooms", "baked_kebab_7"),
            ("Baked apple", "baked_apple"),
            ("Carrot cutlet", "cutlet_carrot"),
            ("A bowl of sauerkraut", "bowl_sauerkraut"),
            ("Beet slices", "beet_slice"),
            ("Green jelly", "jelly_green"),
            ("Red jelly", "jelly_red"),
            ("Dough", "dough"),
            ("Pastry dough", "dough_2"),
            ("Bread", "bread"),
            ("Baked fish", "baked_fish"),
            ("Fish soup", "soup_fish"),
            ("Fish nuggets", "nuggets_fish"),
            ("Pancakes", "pancakes"),
            ("Croissant", "croissant"),
            ("Muffin", "muffin"),
            ("Cabbage soup", "soup_red_green"),
            ("Pumpkin soup", "soup_red_yellow"),
            ("Vegetable soup", "soup_yellow_green"),
            ("Omelette", "omlette"),
            ("Fried egg", "fried_egg"),
            ("Lasagna", "lasagne"),
            ("Pasta", "pasta"),
            ("Baked kebab (Pumpkin)", "baked_kebab_1"),
            ("Baked kebab (Onions)", "baked_kebab_2"),
            ("Baked kebab (Mushrooms)", "baked_kebab_5"),
            ("Berry pie", "pie_1"),
            ("Grape pie", "pie_2"),
            ("A bowl of pumpkin soup", "bowl_pumpkin"),
            ("Baked pumpkin", "baked_pumpkin"),
            ("A bowl of lentils", "bowl_lentil"),
            ("Burger", "burger"),
            ("Sandwich", "sandwich"),
            ("Baked meat", "baked_meat"),
            ("Onion rings", "onion_ring"),
            ("Toasts with onions", "toasts"),
            ("Baked salmon", "baked_salmon"),
            ("Royal fish", "honey_fish"),
            ("Cake", "pie")
            // t_flour_from_wheat
            // t_fish_*_fillet
            // t_raw_meat_sliced_from_*
            // t_potion_berries_juice
        };
        foreach (var (name, id) in tavernItems)
        {
            registry.AddRecipe("Create: " + name, new GKRecipe(new List<string> { id, $"@t_{id}" }, _playerActions));
        }
    }

    private void InitializePrayerRecipeItems(GKItemRegistry registry)
    {
        var prayers = new[]
        {
            ("Prayer for repose", "skull"),
            ("Prayer for faith", "faith"),
            ("Prayer for prosperity", "village"),
            ("Prayer for repentance", "sins"),
            ("Prayer for donations", "money"),
            ("Combo prayer", "faith_money"),
            ("Prayer for imagination", "pen"),
            ("Prayer for shoots and roots", "plant"),
            ("Prayer for retribution", "sword"),
            ("Prayer for protection", "shield"),
            ("Prayer for excellence", "star"),
            ("Prayer for souls' repose", "grat_points_incr"),
            ("Prayer for souls' contentment", "souls"),
            ("Prayer for souls' thorough cleansing", "sin_shard")
        };
        foreach (var (name, id) in prayers)
        {
            registry.AddRecipe("Create: " + name, new GKRecipe(new List<string> { $"b_{id}", $"@b_{id}_2" }, _playerActions));
        }
    }

    private void InitializeInjectionRecipeItems(GKItemRegistry registry)
    {
        var injections = new[]
        {
            ("Acid Injection", "-1_-1"),
            ("Lye Injection", "1_1"),
            ("Glue Injection", "0_1"),
            ("Preservative Injection", "stop"),
            ("Restore Injection", "50"),
            ("Dark Injection", "2_0"),
            ("Silver Injection", "-1_1"),
            ("Gold Injection", "-2_2")
        };
        foreach (var (name, id) in injections)
        {
            registry.AddRecipe("Create: " + name, new GKRecipe($"embalm_{id}", _playerActions));
        }
    }

    private void InitializeBagRecipeItems(GKItemRegistry registry)
    {
        var bags = new[]
        {
            ("Alchemist's bag", "alchemy"),
            ("Farmer's bag", "farming"),
            ("Fisherman's bag", "fishing"),
            ("Tool bag", "tools"),
            ("Universal bag", "universal"),
            ("Potion bag", "potions"),
            ("Builder's bag", "builder"),
            ("Food bag", "food"),
            ("Big universal bag", "universal_big")
        };
        foreach (var (name, id) in bags)
        {
            registry.AddRecipe("Create: " + name, new GKRecipe($"bag_{id}", _playerActions));
        }
    }

    private void InitializeSmithingRecipeItems(GKItemRegistry registry)
    {
        var smithingRecipes = new[]
        {
            ("Nails", new[]{"nails"}),
            ("Lens", new[]{"lense"}),
            ("Silver Ingot", new[]{ "ingot_silver", "@ingot_silver_2" }),
            ("Gold Ingot", new[]{ "ingot_gold", "@ingot_gold_2" })
        };
        foreach (var (name, ids) in smithingRecipes)
        {
            registry.AddRecipe("Create: " + name, new GKRecipe(ids.ToList(), _playerActions));
        }
    }

    private void InitializeBuildingRecipeItems(GKItemRegistry registry)
    {
        var buildingRecipes = new[]
        {
            ("Flitch", new[] { "flitch" }),
            ("Wood billet", new[] { "wood1" }),
            ("Firewood", new[] { "firewood" }),
            ("Wood wedge", new[] { "spike_1" }),
            ("Wooden plank", new[] { "wooden_plank", "@wooden_plank_3" }),
            ("Wood repair kit", new[] { "repair_wdn", "@repair_wdn_1" }),
            ("Jointing", new[] { "wood_constr_1" }),
            ("Wooden beam", new[] { "wood_balk_1" }),
            ("Carved wood", new[] { "carved_wood" }),
            ("A piece of stone", new[] { "stone_plate_1", "@stone_plate_1_2" }),
            ("Stone repair kit", new[] { "repair_stn", "@repair_stn_2" }),
            ("A polished brick of stone", new[]
            {
                "stone_plate_2",
                "@stone_plate_2_2",
                "@stone_plate_2_3",
                "@stone_plate_2_4"
            }),
            ("A carved piece of stone", new[] { "stone_plate_3", "@stone_plate_3b" }),
            ("A piece of marble", new[] { "marble_plate_1", "@marble_plate_1_2" }),
            ("Marble repair kit", new[] { "repair_mrb", "@repair_mrb_2" }),
            ("A polished brick of marble", new[] { "marble_plate_2", "@marble_plate_2_2" }),
            ("A carved piece of marble", new[] { "marble_plate_3" }),
            ("Ceramic bowls", new[] { "ceramic_1" }),
            ("Polishing paste", new[] { "polishing_paste", "@polishing_paste_2" }),
            ("Ceramic jug", new[] { "ceramic_2", "@ceramic_2_1", "@ceramic_2_2" }),
            ("Porcelain pitcher", new[] { "ceramic_3", "@ceramic_3_1", "@ceramic_3_2" }),
            ("Graphite", new[] { "graphite", "@graphite_2" })
        };
        foreach (var (name, ids) in buildingRecipes)
        {
            registry.AddRecipe("Create: " + name, new GKRecipe(ids.ToList(), _playerActions));
        }
    }

    private void InitializeSpiritualismRecipeItems(GKItemRegistry registry)
    {
        var spiritualismRecipes = new[]
        {
            ("Remote craft control", new[] { "fake_global_craft" }),
            ("Story (Organ workbench)", new[] { "story_souls" }),
            ("Chapter (Organ workbench)", new[] { "chapter_souls" }),
            ("Book (Organ workbench)", new[] { "book_souls" }),
            ("Boost fertilizer I (Organ workbench)", new[] { "sack_clock_silver_souls" }),
            ("Boost fertilizer II (Organ workbench)", new[] { "@sack_clock_gold_souls" }),
            ("Quality fertilizer I (Organ workbench)", new[] { "sack_star_silver_souls" }),
            ("Quality fertilizer II (Organ workbench)", new[] { "@sack_star_gold_souls" })
        };
        foreach (var (name, ids) in spiritualismRecipes)
        {
            registry.AddRecipe("Create: " + name, new GKRecipe(ids.ToList(), _playerActions));
        }
    }

    private void InitializeQuestRecipeItems(GKItemRegistry registry)
    {
        var items = new[]
        {
            ("Roof tile", new[]{"roof_tile_1", "roof_tile_2", "roof_tile_3"}),
            ("Mysterious food sauce", new[]{"sauce_for_meal"})
        };
        foreach (var (name, ids) in items)
        {
            registry.AddRecipe("Create: " + name, new GKRecipe(ids.ToList(), _playerActions));
        }
    }

    private void InitializeNormalRecipeItems(GKItemRegistry registry)
    {
        var items = new[]
        {
            ("Iron hammer", new[]{"hammer_2"}),
            ("Wodden grave fence", new[]{"grave_bot_wd_1", "@destroy_grave_bot_wd_1"}),
            ("Ceramic funeral urn", new[]{"funeral_urn_1"}),
            ("Porcelain funeral urn", new[]{"funeral_urn_1"}),
            ("Candle (1)", new[]{"candle_1"}),
            ("Candle (3)", new[]{"candle_3"}),
            ("Candle (4)", new[]{"candle_4"}),
            ("Flyer", new[]{"flyer_bad", "@flyer_good", "@flyer_good_2"}),
            ("Incense", new[]{"incense_1"}),
            ("Incense II", new[]{"incense_2"}),
            ("Ink", new[]{"ink_jar"}),
            ("Pen and Ink", new[]{"ink_pen"}),
            ("Pigskin paper", new[]{"skroll_skin_pig", "@skroll_skin_pig_2"}),
            ("Paper glop", new[]{"pail_wet_paper"}),
            ("Clean paper", new[]{"paper_bad, paper_from_herb"}),
            ("Notes", new[]{"notes"}),
            ("Chapter", new[]{"chapter"}),
            ("Story", new[]{"story"}),
            ("Soft cover", new[]{"cover_1"}),
            ("Hard cover", new[]{"cover_hard", "@cover_hard_2", "@cover_hard_3"}),
            ("Book", new[]{"book_hard", "@book_hard_2"}),
            ("Peat", new[]{"peat_from_waste"}),
            ("Boost fertilizer I", new[]{"sack_clock_silver"}),
            ("Boost fertilizer II", new[]{"sack_clock_gold"}),
            ("Quality fertilizer I", new[]{"sack_star_silver"}),
            ("Quality fertilizer II", new[]{"sack_star_gold"}),
            
            ("Bottle of apple ferment", new[]{"bottle_apple_braga"}),
            ("Bottle of berry ferment", new[]{"bottle_berry_braga"}),
            ("Red wine", new[]{"bottle_red_vine"}),
            ("Booze", new[]
            {
                "booze_from_bottle_apple_braga",
                "@booze_from_bottle_berry_braga",
                "@booze_from_bottle_red_vine_1",
                "@booze_from_bottle_red_vine_2",
                "@booze_from_bottle_red_vine_3"
            })
        };
        foreach (var (name, ids) in items)
        {
            registry.AddRecipe("Create: " + name, new GKRecipe(ids.ToList(), _playerActions));
        }
    }

    private void InitializeProgressiveRecipeItems(GKItemRegistry registry)
    {
        var progressiveTools = new[]
        {
            new { name = "Progressive Shovel", ids = new[]
            {
                new[] { "shovel_1", "@shovel_1_2", "@shovel_1_3" },
                new[] { "shovel_2" }
            } },
            new { name = "Progressive Pickaxe", ids = new[]
            {
                new[] { "pickaxe_1", "@pickaxe_1_2", "@pickaxe_1_3" },
                new[] { "pickaxe_2" }
            } },
            new { name = "Progressive Axe", ids = new[]
            {
                new[] { "axe_1", "@axe_1_2", "@axe_1_3" },
                new[] { "axe_2" }
            } },
            new { name = "Progressive Hammer", ids = new[]
            {
                new[] { "@hammer_0" },
                new[] { "hammer_2" }
            } },
            new { name = "Progressive Chisel", ids = new[]
            {
                new[] { "chisel_1", "@chisel_1_2" },
                new[] { "chisel_2", "@chisel_2_2", "@chisel_2a", "@chisel_2b", "@chisel_2_2a", "@chisel_2_2b" }
            } },
            new { name = "Progressive Sword", ids = new[]
            {
                new[] { "sword_1", "@sword_1_2" },
                new[] { "sword_steel" },
                new[] { "sword_steel_gem" },
                new[] { "sword_damask_gem" }
            } },
            new { name = "Progressive Armor", ids = new[]
            {
                new[] { "armor_lamellar_1", "@armor_lamellar_1_2" },
                new[] { "armor_lamellar_2" }
            } },
        };
        foreach (var item in progressiveTools)
        {
            var recipeName = "Create: " + item.name;
            registry.AddRecipe(recipeName, new GKProgressiveRecipe(item.ids.Select(row => row.ToList()).ToList(), _playerActions));
        }

        var progressiveSmithingRecipes = new[]
        {
            new { name = "Progressive Ingot", ids = new[]
            {
                new[] { "ingot_metal" },
                new[] { "ingot_steel", "@ingot_steel_2" }
            } },
            new { name = "Progressive Metal Parts", ids = new[]
            {
                new[] { "detail_1" },
                new[] { "detail_2" },
                new[] { "detail_3" }
            } },
            new { name = "Progressive Glass", ids = new[]
            {
                new[] { "glass_0", "@glass_0_1", "@glass_0_2" },
                new[] { "glass_1", "@glass_1_2" },
                new[] { "glass_2", "@glass_2_2" }
            } },
            new { name = "Progressive Jewelry", ids = new[]
            {
                new[] { "jewelry_detail_gold" },
                new[] { "bijouterie_gold" }
            } },
        };
        foreach (var item in progressiveSmithingRecipes)
        {
            var recipeName = "Create: " + item.name;
            registry.AddRecipe(recipeName, new GKProgressiveRecipe(item.ids.Select(row => row.ToList()).ToList(), _playerActions));
        }

        var progressiveGraveMarkers = new[]
        {
            new { name = "Progressive Wooden Grave Marker", ids = new[]
            {
                new[] { "grave_top_wd_tab_1", "@grave_top_wd_tab_1_2", "@grave_top_wd_tab_1_3" },
                new[] { "grave_top_wd_cross_1", "@grave_top_wd_cross_1_2" }
            } },
            new { name = "Progressive Stone Grave Marker", ids = new[]
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
                new[] { "grave_top_sculpt_stn_5", "@destroy_top_sculpt_stn_5" }
            } },
            new { name = "Progressive Marble Grave Marker", ids = new[]
            {
                new[] { "grave_top_mrb_cross_1" },
                new[] { "grave_top_mrb_cross_2" },
                new[] { "grave_top_memorial_mrb_1" },
                new[] { "grave_top_stella_mrb_1" },
                new[] { "grave_top_sculpt_mrb_1" },
                new[] { "grave_top_sculpt_mrb_2" },
                new[] { "grave_top_womansaver_mrb_1" },
                new[] { "grave_top_highangel_mrb_1" },
                new[] { "grave_top_sculpt_mrb_4" },
                new[] { "grave_top_sculpt_mrb_5" }
            } },
            new { name = "Progressive Stone Grave Fence", ids = new[]
            {
                new[] { "grave_bot_stn_1", "@grave_bot_stn_1_2", "@destroy_grave_bot_stn_1" },
                new[] { "grave_bot_stn_2", "@destroy_grave_bot_stn_2" },
                new[] { "grave_bot_stn_3" },
                new[] { "grave_bot_stn_4" },
                new[] { "grave_bot_stn_5" },
                new[] { "grave_bot_stn_6" },
                new[] { "grave_bot_stn_7" },
                new[] { "grave_bot_stn_8" }
            } },
            new { name = "Progressive Marble Grave Fence", ids = new[]
            {
                new[] { "grave_bot_mrb_1" },
                new[] { "grave_bot_mrb_2" },
                new[] { "grave_bot_mrb_3" },
                new[] { "grave_bot_mrb_4" },
                new[] { "grave_bot_mrb_5" },
                new[] { "grave_bot_mrb_6" },
                new[] { "grave_bot_mrb_7" },
                new[] { "grave_bot_mrb_8" }
            } },
        };
        foreach (var item in progressiveGraveMarkers)
        {
            var recipeName = "Create: " + item.name;
            registry.AddRecipe(recipeName, new GKProgressiveRecipe(item.ids.Select(row => row.ToList()).ToList(), _playerActions));
        }
    }

    private void InitializePerkItems(GKItemRegistry registry)
    {
        registry.AddPerk("Perk: " + "Butcher", new GKPerk("p_butcher", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Surgeon", new GKPerk("p_doctor", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Scientist", new GKPerk("p_scientist", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Cultist", new GKPerk("p_cultist", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Preacher", new GKPerk("p_preacher", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Cardinal", new GKPerk("p_cardinal", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Curious mind", new GKPerk("p_naturalist", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Journalist", new GKPerk("p_journalist", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Writer", new GKPerk("p_writer", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Playwright", new GKPerk("p_good_writer", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Master gatherer", new GKPerk("p_collector", null, "t_mushroom2", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Farmer", new GKPerk("p_farmer", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Beekeeper", new GKPerk("p_beekeeper2", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Wine master", new GKPerk("p_wine_master", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Blacksmith", new GKPerk("p_blacksmith", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Big buy", new GKPerk("p_big_buy", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Fireman", new GKPerk("p_fireman", new List<string> { "@ingot_metal_huge","@ingot_metal_1_huge","@ingot_metal_2_huge" }, playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Engineer", new GKPerk("p_engineer", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Sword master", new GKPerk("p_sword_master", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Axeman", new GKPerk("p_axeman", null, "t_wood_big", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Miner", new GKPerk("p_miner", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Carpenter", new GKPerk("p_woodworker", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Mason", new GKPerk("p_mason", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Jeweler", new GKPerk("p_jevelery", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Eloquence", new GKPerk("p_eloquence", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Industriousness", new GKPerk("p_industriousness", playerActions: _playerActions));
        registry.AddPerk("Perk: " + "Persistence", new GKPerk("p_persistence", playerActions: _playerActions));
    }

    private void InitializeRelationItems(GKItemRegistry registry)
    {
        var npcs = new[]
        {
            // Main
            ("Astrologer", "npc_astrologer"),
            ("Bishop", "npc_bishop"),
            ("Inquisitor", "npc_inquisitor"),
            ("Merchant", "npc_merchant"),
            ("Ms. Charm", "npc_actress"),
            ("Snake", "npc_cultist"),
            // Village
            ("Adam", "npc_potter"),
            ("Beekeeper", "npc_beekeeper"),
            ("Dig", "npc_dig"),
            ("Farmer", "npc_farmer"),
            ("Horadric", "npc_tavern owner"),
            ("Krezvold", "npc_blacksmith"),
            ("Miller", "npc_miller"),
            ("Miss Chain", "npc_mrs_chain"),
            ("Woodcutter", "npc_wood_cutter"),
            ("Vagner", "npc_actor"),
            // Other
            ("Clotho", "npc_witch"),
            ("Donkey", "donkey"),
            ("Gerry", "crafting_skull_3"),
            ("Gypsy Baron", "npc_gypsy"),
            ("Koukol", "npc_hunchback"),
            ("Royal Services", "npc_royal_box"),
            // Game of Crone DLC
            ("Cook", "npc_refugee_cook"),
            ("Master Alaric", "npc_master_alarich"),
            ("Marquis Teodoro Jr.", "npc_marquis_teodoro_jr"),
            ("Undertaker", "npc_refugee_coffin_maker"),
            ("Tanner", "npc_refugee_tanner"),
            // Better Save Soul DLC
            ("Euric", "npc_euric")
            // Smiler??
        };
        foreach (var (name, id) in npcs)
        {
            registry.AddRelation(name, new GKRelation(id, 10, _playerActions));
        }
    }

    private void InitializeIngameItems(GKItemRegistry registry)
    {
        var ingameItems = new[]
        {
            ("Red Research Points", "techpoints_red"),
            ("Green Research Points", "techpoints_green"),
            ("Blue Research Points", "techpoints_blue"),
            
            // quest items
            ("Instructions for Key", "quest_instruction_snake"),
            ("Keepers Key", "quest_key_astrologer"),
            ("Graveyard Scroll", "scroll_graveyard_prop"),
            ("Stamp", "stamp"),
            ("Paper with calculations", "calculation_for_mill_fix"),
            ("Memory powder", "memory_powder"),
            ("Piece of donkey hair", "donkey_lock_wool"),
            ("Casual prayer", ""),
            
            // portal items
            ("Salty Fork", "fork_salty"),
            ("Mirror of Pride", "mirror_of_pride"),
            ("Eternal burning coal", "eternal_burning_coal"),
            ("Golden angle", "sextant"),
            ("Endless notebook", "endless_book"),
            ("Necklace", "necklace"),
            
            // better save soul quest items
            ("Old rusty key", "souls_zone_key"),
            ("Lost tooth", "gerry_tooth"),
            ("Ode for Bishop", "ode_for_bishop"),
            ("Pride charged shard", "pride_charged_shard"),
            ("Wrath charged shard", "wrath_charged_shard"),
            ("Gluttony charged shard", "gluttony_charged_shard"),
            ("Sloth charged shard", "sloth_charged_shard"),
            ("Lust charged shard", "lust_charged_shard"),
            ("Envy charged shard", "envy_charged_shard"),
            ("Remote crafting", ""),
            ("Unusual ash", "ash_on_shawl"),
            ("Note with rumor", "note_with_rumors")
        };
        foreach (var (name, id) in ingameItems)
        {
            registry.AddIngameItem(name, new GKIngameItem(id, _playerActions));
        }
    }
}
