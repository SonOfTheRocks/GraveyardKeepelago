using System.Collections.Generic;
using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.Logic;
using HarmonyLib;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Locations
{
    public class TechsModifier
    {
        private static ILogger _logger;
        private static GKArchipelagoClient _archipelago;
        private static GKItemManager _itemManager;
        private static GKLocationChecker _locationChecker;

        public TechsModifier(ILogger logger, GKArchipelagoClient archipelago, GKItemManager itemManager,
            GKLocationChecker locationChecker)
        {
            _logger = logger;
            _archipelago = archipelago;
            _itemManager = itemManager;
            _locationChecker = locationChecker;
        }

        public static bool HandleTechUnlockLocation(string id)
        {
            //_logger.LogInfo($"Game tries to unlock craft: {id}");

            var locationName = "";
            if (id.StartsWith("ap_"))
                locationName = id.Substring(3);
            // TODO: show correct unlocks in dialog directly before, most likely by patching GetData or something
            else
                craftIds.TryGetValue(id, out locationName);

            if (!string.IsNullOrEmpty(locationName))
            {
                _logger.LogInfo($"Add checked Location: {locationName}");
                _locationChecker.AddCheckedLocation(locationName);
                return true;
            }

            if (!craftIdBlacklist.Contains(id))
                _logger.LogError($"Unknow craft_id: {id}");
                
            return false;
        }

        public void ReplaceTechDefinitionUnlocksWithChecks(TechDefinition tech)
        {
            // dump techs
            //_logger.LogDebug($"Replacing {JsonUtility.ToJson(tech)}");

            // clear original unlocks
            tech.crafts = new List<string>();
            tech.works = new List<string>();
            tech.phrases = new List<string>();
            tech.perks = new List<string>();

            // TODO: Handle these 3 techs that should be perks (and also handle how the story only gets one of these 3)
            if (new List<string> {"Industriousness", "Persistence", "Eloquence"}.Contains(tech.id))
                return;

            if(!TechIds.TryGetValue(tech.id, out var techArchipelagoID))
                _logger.LogError($"Could not determine Archipelago ID for tech {tech.id}");
            
            // add archipelago locations as craft unlocks
            tech.crafts.Add($"ap_{techArchipelagoID} - Unlock 1");
            tech.crafts.Add($"ap_{techArchipelagoID} - Unlock 2");
            tech.crafts.Add($"ap_{techArchipelagoID} - Unlock 3");

            // Clear unlocks cache
            Traverse.Create(tech).Field("_unlocks_list").SetValue(null);
        }

        // crafts (recipes) that should be handled like vanilla
        private static readonly List<string> craftIdBlacklist = new List<string>()
        {
            "souls_builddesk:p:corpse_bed_place",
            
            // unlocked by fishing
            "fish_gudgeon_fillet",
            "fish_anchovy_fillet",
            "fish_tilapia_fillet",
            "fish_perch_fillet",
            "fish_eel_fillet",
            "fish_bream_fillet",
            "fish_tuna_fillet",
            "fish_sardine_fillet",
            "fish_pike_fillet",
            "fish_crucian_silver_fillet",
            "fish_salmon_fillet",
            "fish_sturgeon_fillet",
            "fish_crucian_gold_fillet",
            "fish_carp_fillet",
            "raw_meat_sliced_from_fish_frog_green",
            "t_fish_gudgeon_fillet",
            "t_fish_anchovy_fillet",
            "t_fish_tilapia_fillet",
            "t_fish_perch_fillet",
            "t_fish_eel_fillet",
            "t_fish_bream_fillet",
            "t_fish_tuna_fillet",
            "t_fish_sardine_fillet",
            "t_fish_pike_fillet",
            "t_fish_crucian_silver_fillet",
            "t_fish_salmon_fillet",
            "t_fish_sturgeon_fillet",
            "t_fish_crucian_gold_fillet",
            "t_fish_carp_fillet",
            "t_raw_meat_sliced_from_fish_frog_green",
        };

        // crafts (recipes) that player gets through other means than the tech tree
        private static readonly Dictionary<string, string> craftIds = new Dictionary<string, string>
        {
            { "keeper_room_builddesk:p:keeper_room_picture_7", "Donkey - Meet the Donkey" },
            { "keeper_room_builddesk:p:keeper_room_picture_4", "Tutorial - Talk to Bishop (2)" },
        };

        private static readonly Dictionary<string, string> TechIds = new()
        {
            // anatomy and alchemy
            {"First slice", "FIRST_SLICE"},
            {"The Beginning Of Alchemy", "THE_BEGINNING_OF_ALCHEMY"},
            {"Hardspares", "HARDSPARES"},
            {"Softspares", "SOFTSPARES"},
            {"Embalm 1", "EMBALMING_LIQUIDS"},
            {"Alchemy storage", "ALCHEMY_STORAGE"},
            {"Gentle butcher", "GENTLE_BUTCHER"},
            {"Embalming", "EMBALMING"},
            {"Advanced alchemy", "ADVANCED_ALCHEMY"},
            {"Important parts", "IMPORTANT_PARTS"},
            {"Embalming 2", "EMBALMING_2"},
            {"Embalm 2", "EMBALMING_LIQUIDS_2"},
            {"tech_bag_alchemy", "ALCHEMISTS_BAG"},
            {"Anatomy 2", "ANATOMY_2"},
            {"Second chance", "SECOND_CHANCE"},
            {"Embalm 3", "EMBALMING_LIQUIDS_3"},
            {"Distilate", "DISTILLATE"},
            {"Surgery", "SURGERY"},
            {"Dark body", "DARK_BODIES"},
            {"Zombie logistic", "ZOMBIE_LOGISTICS"},
            {"Master of alchemy", "MASTER_OF_ALCHEMY"},
            {"Zombie alchemy", "ZOMBIE_ALCHEMY_WORKBENCH"},
            {"tech_bag_farming", "FARMERS_BAG"},
            {"Tech_cultist", "CULTIST"},
            {"tech_bag_fishing", "FISHERMANS_BAG"},
            {"tech_bag_tools", "TOOL_BAG"},
            {"tech_bag_universal", "UNIVERSAL_BAG"},
            {"tech_bag_potions", "POTION_BAG"},
            {"tech_bag_builder", "BUILDERS_BAG"},
            {"tech_bag_food", "FOOD_BAG"},
            {"tech_bag_universal_big", "BIG_UNIVERSAL_BAG"},
            
            // theology
            {"Faith", "FAITH"},
            {"Grave plate", "HUMBLE_MARKER"},
            {"Light of faith", "LIGHT_OF_FAITH"},
            {"Сomfort of faith", "COMFORT_OF_FAITH"},
            {"Faithbuisness", "BUSINESS_OF_FAITH"},
            {"Simple gravestones", "SIMPLE_GRAVESTONES"},
            {"Power of faith", "POWER_OF_FAITH"},
            {"Price of faith", "PRICE_OF_FAITH"},
            {"Corpses burning", "CREMATION"},
            {"Stone gravestones", "STONE_GRAVESTONES"},
            {"Illumination of faith", "ILLUMINATION_OF_FAITH"},
            {"Smell of faith", "SMELL_OF_FAITH"},
            {"Praying stand", "GRAVEYARD_ENHANCEMENT"},
            {"Carved gravestones", "CARVED_GRAVESTONES"},
            {"Softness of faith", "SOFTNESS_OF_FAITH"},
            {"Corpses burning 2", "CREMATION_2"},
            {"Grave monuments", "GRAVE_MONUMENTS"},
            {"Superpower of faith", "SUPERPOWER_OF_FAITH"},
            {"Pheromones", "PHEROMONES"},
            {"Stone praying stand", "STONE_PRAYER_STATION"},
            {"Marble gravestones", "MARBLE_GRAVESTONES"},
            {"Shining of faith", "SHINING_OF_FAITH"},
            {"Advanced gravestones", "ADVANCED_GRAVESTONES"},
            {"Carved marble gravestones", "CARVED_MARBLE_GRAVESTONES"},
            {"Crypts", "CRYPTS"},
            {"Advanced gravestones II", "ADVANCED_GRAVESTONES_2"},
            {"Stone statues", "STONE_STATUES"},
            {"Marble statues", "MARBLE_STATUES"},
            {"Marble fences", "MARBLE_FENCES"},
            
            // book writing
            {"Research", "RESEARCH"},
            {"Journalist", "JOURNALIST"},
            {"Writing", "WRITING"},
            {"Paper crafting", "PAPER_CRAFTING"},
            {"Inventing storyes", "INVENTING_STORIES"},
            {"Writing supplies", "WRITING_SUPPLIES"},
            {"Random text generator", "RANDOM_TEXT_GENERATOR"},
            {"Writing tricks", "WRITING_TRICKS"},
            {"Books", "BOOKS"},
            {"Writer inspiration", "WRITERS_INSPIRATION"},
            {"Playwright", "PLAYWRIGHT"},
            {"Paper production", "PAPER_PRODUCTION"},
            {"A simple printing press", "A_SIMPLE_PRINTING_PRESS"},
            {"Complex printing press", "COMPLEX_PRINTING_PRESS"},
            
            // farming and nature
            {"Garden beds", "GARDEN_BEDS"},
            {"Gathering", "GATHERING"},
            {"Improvement", "IMPROVEMENT"},
            {"The master gathering", "MASTER_GATHERER"},
            {"Transplanting", "TRANSPLANTING"},
            {"Browncastle", "BEEKEEPING"},
            {"Gardening", "GARDENING"},
            {"Insects", "INSECTS"},
            {"Decay", "DECAY"},
            {"Zombie gardening", "ZOMBIE_GARDENING"},
            {"Grape farming", "GRAPE_FARMING"},
            {"The domestication of bees", "BEE_DOMESTICATION"},
            {"Simple fertilizers", "SIMPLE_FERTILIZERS"},
            {"Brewing", "BREWING"},
            {"Zombie vineyard", "ZOMBIE_VINEYARD"},
            {"Beefriend", "BEEFRIEND"},
            {"Digestion", "DIGESTION"},
            {"Zombie brewing", "ZOMBIE_BREWERY"},
            {"Winemaking", "WINEMAKING"},
            {"Complex fertilizers", "COMPLEX_FERTILIZERS"},
            {"Strong alcohol", "STRONG_ALCOHOL"},
            {"Zombie winemaking", "ZOMBIE_WINERY"},
            {"Blending", "BLENDING"},
            
            // smithing
            {"Iron", "IRON"},
            {"Primitive forging", "PRIMITIVE_FORGING"},
            {"Glass", "GLASS"},
            {"Advanced forging", "ADVANCED_FORGING"},
            {"Instruments", "TOOLS"},
            {"Weapons", "WEAPONS"},
            {"Inborn blacksmith", "INBORN_BLACKSMITH"},
            {"Advanced smelting", "ADVANCED_SMELTING"},
            {"Glass-blower", "GLASS_BLOWER"},
            {"Martial skills", "MARTIAL_SKILLS"},
            {"Steel", "STEEL"},
            {"Rules of burning", "RULES_OF_BURNING"},
            {"Engineer", "ENGINEER"},
            {"Steel instruments", "STEEL_TOOLS"},
            {"Iron castings", "IRON_CASTINGS"},
            {"Glass-blower 2", "GLASS_BLOWER_2"},
            {"Steel Weapons", "STEEL_WEAPONS"},
            {"Jeweler", "JEWELER"},
            {"Skilled casting", "SKILLED_CASTING"},
            {"Sword master", "SWORD_MASTER"},
            {"Precious little things", "PRECIOUS_LITTLE_THINGS"},
            
            // building
            {"The idea of the tree", "THE_CONCEPT_OF_WOOD"},
            {"The idea of the stone", "THE_CONCEPT_OF_STONE"},
            {"The idea of the earth", "THE_CONCEPT_OF_DIRT"},
            {"Saw", "SAWING"},
            {"Clay", "CLAY"},
            {"Firewood", "FIREWOOD"},
            {"Stone processing", "STONEWORKING"},
            {"Mining", "MINING"},
            {"Ceramic firing", "CERAMIC_FIRING"},
            {"Woodcutter", "WOODCUTTER"},
            {"Precious metals", "PRECIOUS_METALS"},
            {"Zombie mining", "ZOMBIE_MINING"},
            {"Wood processing", "WOODWORKING"},
            {"Zombie woodcuttering", "ZOMBIE_WOODCUTTING"},
            {"Related ore", "RELATED_ORE"},
            {"Assembly stand", "ASSEMBLY_STAND"},
            {"Circular saw", "CIRCULAR_SAW"},
            {"Stone carving", "STONE_CARVING"},
            {"Scent of gold", "SCENT_OF_GOLD"},
            {"Wooden work", "FINE_WOODWORKING"},
            {"Working trick", "TRICKS_OF_THE_TRADE"},
            {"Marble Quarrying", "MARBLE_QUARRYING"},
            {"Zombie quarrying I", "ZOMBIE_QUARRYING_1"},
            {"Gems", "GEMS"},
            {"The art of stone", "THE_ART_OF_STONE"},
            {"Zombie quarrying II", "ZOMBIE_QUARRYING_2"},
            {"Best friend", "BEST_FRIEND"},
            
            // cookery
            {"2min", "SIMPLE_AND_TASTY"}, //Simple and Tasty
            {"Vegetable dishes", "VEGETABLE_DISHES"},
            {"Jelly", "JELLY"},
            {"Bread", "BREAD"},
            {"Fish", "FISH_DISHES"},
            {"Sweet baking", "PASTRIES"}, //Pastries
            {"Vegetable soups", "SOUPS"},
            {"French cuisine", "EGG_DISHES"}, //Egg dishes
            {"Italian kitchen", "GOOD_CARBS"}, //Good carbs
            {"Fish shish kebab", "KEBAB"},
            {"Pies", "PIES"},
            {"Vegetable bowl", "TASTY_AND_HEALTHY"}, //Tasty and Healthy
            {"Fast food", "MEAT_FIESTA"}, //Meat Fiesta
            {"For beer", "BEER_SNACKS"},
            {"Fish delicous", "FISH_DELICACIES"},
            {"Desert", "CAKE"}, //Cake
            {"Simple snacks", "SIMPLE_SNACKS"},
            {"Alcohol cook", "NECTAR_OF_THE_GODS"}, //Nectar of the gods
            {"Vegetable set", "VEGETABLE_SET"},
            {"Milk processing", "MILK_PROCESSING"},
            {"In Lentil We Trust", "IN_LENTIL_WE_TRUST"},
            {"Patties", "PATTIES"},
            {"Cheese dishes", "CHEESE_DISHES"},
            {"Jelly desserts", "JELLY_DESSERTS"},
            {"Honey cake", "HONEY_CAKE"},
            
            // spiritualism
            {"soul_sins_1", "BEGINNER_HEALER"},
            {"soul_sins_2", "HEALER_ENTHUSIAST"},
            {"soul_stone_fences", "STONE_GRAVE_FENCE"},
            {"soul_church_additions", "PRAYS_FOR_SOULS"},
            {"soul_buildings", "UPDATED_EQUIPMENT"},
            {"soul_totem_tech", "USEFUL_EQUIPMENT"},
            {"soul_marble_fences", "MARBLE_GRAVE_FENCE"},
            {"soul_sins_3", "EXPERIENCED_HEALER"},
            {"soul_stone_statues", "STONE_GRAVESTONES"}, //+ (Spritualism)
            {"soul_writing_additions", "EXQUISITE_WRITING"},
            {"soul_buildings_2", "UPDATED_EQUIPMENT_2"},
            {"soul_garden_additions", "SIMPLIFIED_FARMING"},
            {"soul_marble_statues", "MARBLE_GRAVESTONES"}, //+ (Spritualism)
            {"soul_sins_4", "PROFESSIONAL_HEALER"},
        };
    }
}