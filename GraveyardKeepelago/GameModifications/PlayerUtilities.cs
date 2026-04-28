using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.GameModifications;

public class PlayerUtilities
{
    public static bool IsDroppingItems { get; set; }
    private static ILogger _logger;

    public static void Initialize(ILogger logger)
    {
        _logger = logger;
    }
    
    // List<PlayerBuff> MainGame.me.save.buffs
    // Buff is permanent if PlayerBuff.definition.tick_period.EqualsTo(0.0f)
    public static void ApplyPermanentBuff(string buffID)
    {
        // Change Buff Definition to be permanent
        GameBalance.me.GetData<BuffDefinition>(buffID).tick_period = 0.0f;
            
        // Apply Buff
        BuffsLogics.AddBuff(buffID);
    }
    
    public enum UnlockType
    {
        Craft,
        Perk,
        Work,
        Phrase
    }

    public static void ApplyUnlocks(List<(UnlockType, string)> unlocks)
    {
        if (unlocks.Count == 0)
            return;
        
        // needed things: price, GetState(), GetVisibleUnlocksList()
        // creating a temporary TechDefinition for TechUnlockDialog and the actual unlock
        var tempTechDefinition = new TechDefinition();
        const string techDefID = "temp";
        tempTechDefinition.id = techDefID;
        tempTechDefinition.invisible = true;
        tempTechDefinition.requires_dlc = DLCEngine.DLCVersion.None;
        foreach (var (type, id) in unlocks)
        {
            switch (type)
            {
                case UnlockType.Craft:
                    tempTechDefinition.crafts.Add(id); break;
                case UnlockType.Perk:
                    tempTechDefinition.perks.Add(id); break;
                case UnlockType.Work:
                    tempTechDefinition.works.Add(id); break;
                case UnlockType.Phrase:
                    tempTechDefinition.phrases.Add(id); break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // add tempTechDefinition to GameBalance datas and cache
        var types = Traverse.Create(GameBalance.me).Field("_types").GetValue<List<System.Type>>();
        var datas = Traverse.Create(GameBalance.me).Field("_datas").GetValue<List<IList>>();
        var cache = Traverse.Create(GameBalance.me).Field("_cache").GetValue<List<Dictionary<string, int>>>();
        
        var index = types.IndexOf(typeof(TechDefinition));
        var index2 = datas[index].Count; // length of datas[index] will be the index2 of the added TechDefiniton
        datas[index].Add(tempTechDefinition);
        cache[index].Add(techDefID, index2);
        
        // ids that start with '@' are not shown in the dialog
        /*
        GUIElements.me.tech_dialog.Open(
            GameBalance.me.GetData<TechDefinition>(tempTechDefinition.id), 
            (GJCommons.VoidDelegate) null, 
            true);
        */
        // delete temporary TechDefinition again (may need to be done later with on_hide)
        datas[index].RemoveAt(index2);
        cache[index].Remove(techDefID);
    }
    
    public static void ApplyRelation(string npcID, int amount)
    {
        MainGame.me.player.data.AddToParams("_rel_" + npcID, (float) amount);
        MainGame.me.save.known_npcs.GetOrCreateNPC(npcID);
        var npcWGO = WorldMap.GetWorldGameObjectByObjId(npcID);
        npcWGO.ShowRelationChangeBubble(amount);
    }

    public static void DropItem(string itemID, int amount = 1)
    {
        var player = MainGame.me.player;
        var pos = player.transform.position + player.transform.forward;
        
        _logger.LogInfo($"Dropping Item '{itemID}'");

        var techpoint_amount = 100;
        if (itemID.StartsWith("techpoints"))
        {
            var red = itemID.EndsWith("red") ? techpoint_amount : 0;
            var green = itemID.EndsWith("green") ? techpoint_amount : 0;
            var blue = itemID.EndsWith("blue") ? techpoint_amount : 0;
            TechPointsDrop.Drop(pos, red, green, blue);
        }
        else
            return;
        
        //TODO: maybe need to set flowscript for certain items
        var item = new Item(itemID, amount);
        IsDroppingItems = true;
        var transform = MainGame.me.world_root;
        //DropResGameObject.Drop(pos, item, transform);
        //WorldGameObject.DropItem()
        //DropResGameObject.Drop(pos, item, transform, Direction.IgnoreDirection, force_stacked_drop: true);
        
        
        //MainGame.me.player.DropItem(item, Direction.None, pos);
        //MainGame.me.player_char.wgo.DropItem(item);
        if (!item.definition.player_cant_throw_out)
            MainGame.me.player.AddToInventory(item);
        else
        {
            var c = MainGame.me.player.components.character;
            DropResGameObject.Drop(c.tf.position, item, c.tf.parent, Direction.ToPlayer);
        }

        //CraftComponent.wgo.DropItems(objList)
        IsDroppingItems = false;
    }

    public static void SkipIntro()
    {
        var intro = Traverse.Create(typeof(Intro)).Field("_me").GetValue<Intro>();
        if (intro == null)
            return;
        
        if (MainGame.me != null)
            Intro.OnIntroAnimationFinished();
            
        Traverse.Create(intro).Field("_on_finished").GetValue<Action>().TryInvoke();
    }

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