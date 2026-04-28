using System.Collections.Generic;
using System.Reflection;
using GraveyardKeepelago.GameModifications.Patches;
using HarmonyLib;
using KaitoKid.ArchipelagoUtilities.Net.Constants;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace GraveyardKeepelago.Locations.Patches
{
    public static class TechUnlockConstants
    {
        private static readonly Dictionary<string, int> TechIds = new Dictionary<string, int> {
            // anatomy and alchemy
            {"First slice", 101},
            {"The Beginning Of Alchemy", 102},
            {"Hardspares", 103},
            {"Softspares", 104},
            {"Embalm 1", 105}, //Embalming Liquids 1
            {"Alchemy storage", 106},
            {"Gentle butcher", 107},
            {"Embalming", 108},
            {"Advanced alchemy", 109},
            {"Important parts", 110},
            {"Embalming 2", 111},
            {"Embalm 2", 112}, //Embalming Liquids 2
            {"tech_bag_alchemy", 113},
            {"Anatomy 2", 114},
            {"Second chance", 115},
            {"Embalm 3", 116}, //Embalming Liquids 3
            {"Distilate", 117},
            {"Surgery", 118},
            {"Dark body", 119},
            {"Zombie logistic", 120},
            {"Master of alchemy", 121},
            {"Zombie alchemy", 122},
            {"tech_bag_farming", 123},
            {"Tech_cultist", 124},
            {"tech_bag_fishing", 125},
            {"tech_bag_tools", 126},
            {"tech_bag_universal", 127},
            {"tech_bag_potions", 128},
            {"tech_bag_builder", 129},
            {"tech_bag_food", 130},
            {"tech_bag_universal_big", 131},
            
            // theology
            {"Faith", 201},
            {"Grave plate", 202}, //Humble Marker
            {"Light of faith", 203},
            {"Сomfort of faith", 204},
            {"Faithbuisness", 205},
            {"Simple gravestones", 206},
            {"Power of faith", 207},
            {"Price of faith", 208},
            {"Corpses burning", 209},
            {"Stone gravestones", 210},
            {"Illumination of faith", 211},
            {"Smell of faith", 212},
            {"Praying stand", 213}, //Graveyard Enhancement
            {"Carved gravestones", 214},
            {"Softness of faith", 215},
            {"Corpses burning 2", 216},
            {"Grave monuments", 217},
            {"Superpower of faith", 218},
            {"Pheromones", 219},
            {"Stone praying stand", 220},
            {"Marble gravestones", 221},
            {"Shining of faith", 222},
            {"Advanced gravestones", 223},
            {"Carved marble gravestones", 224},
            {"Crypts", 225},
            {"Advanced gravestones II", 226},
            {"Stone statues", 227},
            {"Marble statues", 228},
            {"Marble fences", 229},
            
            // book writing
            {"Research", 301},
            {"Journalist", 302},
            {"Writing", 303},
            {"Paper crafting", 304},
            {"Inventing storyes", 305},
            {"Writing supplies", 306},
            {"Random text generator", 307},
            {"Writing tricks", 308},
            {"Books", 309},
            {"Writer inspiration", 310},
            {"Playwright", 311},
            {"Paper production", 312},
            {"A simple printing press", 313},
            {"Complex printing press", 314},
            
            // farming and nature
            {"Garden beds", 401},
            {"Gathering", 402},
            {"Improvement", 403},
            {"The master gathering", 404},
            {"Transplanting", 405},
            {"Browncastle", 406}, //Beekeeping??
            {"Gardening", 407},
            {"Insects", 408},
            {"Decay", 409},
            {"Zombie gardening", 410},
            {"Grape farming", 411},
            {"The domestication of bees", 412},
            {"Simple fertilizers", 413},
            {"Brewing", 414},
            {"Zombie vineyard", 415},
            {"Beefriend", 416},
            {"Digestion", 417},
            {"Zombie brewing", 418},
            {"Winemaking", 419},
            {"Complex fertilizers", 420},
            {"Strong alcohol", 421},
            {"Zombie winemaking", 422},
            {"Blending", 423},
            
            // smithing
            {"Iron", 501},
            {"Primitive forging", 502},
            {"Glass", 503},
            {"Advanced forging", 504},
            {"Instruments", 505},
            {"Weapons", 506},
            {"Inborn blacksmith", 507},
            {"Advanced smelting", 508},
            {"Glass-blower", 509},
            {"Martial skills", 510},
            {"Steel", 511},
            {"Rules of burning", 512},
            {"Engineer", 513},
            {"Steel instruments", 514},
            {"Iron castings", 515},
            {"Glass-blower 2", 516},
            {"Steel Weapons", 517},
            {"Jeweler", 518},
            {"Skilled casting", 519},
            {"Sword master", 520},
            {"Precious little things", 521},
            
            // building
            {"The idea of the tree", 601},
            {"The idea of the stone", 602},
            {"The idea of the earth", 603},
            {"Saw", 604},
            {"Clay", 605},
            {"Firewood", 606},
            {"Stone processing", 607},
            {"Mining", 608},
            {"Ceramic firing", 609},
            {"Woodcutter", 610},
            {"Precious metals", 611},
            {"Zombie mining", 612},
            {"Wood processing", 613},
            {"Zombie woodcuttering", 614},
            {"Related ore", 615},
            {"Assembly stand", 616},
            {"Circular saw", 617},
            {"Stone carving", 618},
            {"Scent of gold", 619},
            {"Wooden work", 620}, //Fine woodworking
            {"Working trick", 621}, //Tricks of the Trade
            {"Marble Quarrying", 622},
            {"Zombie quarrying I", 623},
            {"Gems", 624},
            {"The art of stone", 625},
            {"Zombie quarrying II", 626},
            {"Best friend", 627},
            
            // cookery
            {"2min", 701}, //Simple and Tasty
            {"Vegetable dishes", 702},
            {"Jelly", 703},
            {"Bread", 704},
            {"Fish", 705},
            {"Sweet baking", 706}, //Pastries
            {"Vegetable soups", 707},
            {"French cuisine", 708}, //Egg dishes
            {"Italian kitchen", 709}, //Good carbs
            {"Fish shish kebab", 710},
            {"Pies", 711},
            {"Vegetable bowl", 712}, //Tasty and Healthy
            {"Fast food", 713}, //Meat Fiesta
            {"For beer", 714},
            {"Fish delicous", 715},
            {"Desert", 716}, //Cake
            {"Simple snacks", 717},
            {"Alcohol cook", 718}, //Nectar of the gods
            {"Vegetable set", 719},
            {"Milk processing", 720},
            {"In Lentil We Trust", 721},
            {"Patties", 722},
            {"Cheese dishes", 723},
            {"Jelly desserts", 724},
            {"Honey cake", 725},
            
            // spiritualism
            {"soul_sins_1", 801}, //Beginner healer
            {"soul_sins_2", 802}, //Healer enthusiast
            {"soul_stone_fences", 803},
            {"soul_church_additions", 804}, //Prays for Souls
            {"soul_buildings", 805}, //Updated Equipment
            {"soul_totem_tech", 806}, //Useful Equipment
            {"soul_marble_fences", 807},
            {"soul_sins_3", 808}, //Experienced Healer
            {"soul_stone_statues", 809},
            {"soul_writing_additions", 810},
            {"soul_buildings_2", 811}, //Updated Equipment II
            {"soul_garden_additions", 812},
            {"soul_marble_statues", 813},
            {"soul_sins_4", 814}, //Professional healer
        };
    }
    //public TechUnlock.TechUnlockData GetData()
    [HarmonyPatch(typeof(TechUnlock), nameof(TechUnlock.GetData))]
    public class TechUnlockGetDataPatch : BasePatch
    {
        private static bool Prefix(TechUnlock __instance, MethodBase __originalMethod)
        {
            LogBefore(__originalMethod);
            
            if (!__instance.id.StartsWith("ap_"))
                return MethodPrefix.RUN_ORIGINAL_METHOD;
            
            if (Traverse.Create(__instance).Field("_data").GetValue() != null)
                return MethodPrefix.RUN_ORIGINAL_METHOD;

            var data = new TechUnlock.TechUnlockData
            {
                name = "AP Unlock",
                description = "An AP Item unlocking... something",
                sprite = APSpriteLoader.GetSprite("archipelago_32x32"),
            };
            Traverse.Create(__instance).Field("_data").SetValue(data);
            
            return MethodPrefix.RUN_ORIGINAL_METHOD;
        }

        private static void Postfix(MethodBase __originalMethod)
        {
            LogAfter(__originalMethod);
        }
    }
}