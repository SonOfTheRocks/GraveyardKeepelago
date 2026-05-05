using System;
using System.Collections.Generic;
using System.Collections;
using HarmonyLib;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.GameModifications;

public class PlayerActions : IPlayerActions
{
    private readonly ILogger _logger;

    public PlayerActions(ILogger logger)
    {
        _logger = logger;
    }

    public void ApplyPermanentBuff(string buffID)
    {
        GameBalance.me.GetData<BuffDefinition>(buffID).tick_period = 0.0f;
        BuffsLogics.AddBuff(buffID);
    }

    public void ApplyUnlocks(List<(PlayerUtilities.UnlockType, string)> unlocks)
    {
        if (unlocks.Count == 0)
            return;

        var tempTechDefinition = new TechDefinition();
        const string techDefID = "temp";
        tempTechDefinition.id = techDefID;
        tempTechDefinition.invisible = true;
        tempTechDefinition.requires_dlc = DLCEngine.DLCVersion.None;
        foreach (var (type, id) in unlocks)
        {
            switch (type)
            {
                case PlayerUtilities.UnlockType.Craft:
                    tempTechDefinition.crafts.Add(id); break;
                case PlayerUtilities.UnlockType.Perk:
                    tempTechDefinition.perks.Add(id); break;
                case PlayerUtilities.UnlockType.Work:
                    tempTechDefinition.works.Add(id); break;
                case PlayerUtilities.UnlockType.Phrase:
                    tempTechDefinition.phrases.Add(id); break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        var types = Traverse.Create(GameBalance.me).Field("_types").GetValue<List<System.Type>>();
        var datas = Traverse.Create(GameBalance.me).Field("_datas").GetValue<List<IList>>();
        var cache = Traverse.Create(GameBalance.me).Field("_cache").GetValue<List<Dictionary<string, int>>>();

        var index = types.IndexOf(typeof(TechDefinition));
        var index2 = datas[index].Count;
        datas[index].Add(tempTechDefinition);
        cache[index].Add(techDefID, index2);

        datas[index].RemoveAt(index2);
        cache[index].Remove(techDefID);
    }

    public void ApplyRelation(string npcID, int amount)
    {
        MainGame.me.player.data.AddToParams("_rel_" + npcID, (float) amount);
        MainGame.me.save.known_npcs.GetOrCreateNPC(npcID);
        var npcWGO = WorldMap.GetWorldGameObjectByObjId(npcID);
        npcWGO.ShowRelationChangeBubble(amount);
    }

    public void DropItem(string itemID, int amount = 1)
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

        var item = new Item(itemID, amount);
        PlayerUtilities.IsDroppingItems = true;
        var transform = MainGame.me.world_root;

        if (!item.definition.player_cant_throw_out)
            MainGame.me.player.AddToInventory(item);
        else
        {
            var c = MainGame.me.player.components.character;
            DropResGameObject.Drop(c.tf.position, item, c.tf.parent, Direction.ToPlayer);
        }

        PlayerUtilities.IsDroppingItems = false;
    }

    public void SkipIntro()
    {
        var intro = Traverse.Create(typeof(Intro)).Field("_me").GetValue<Intro>();
        if (intro == null)
            return;

        if (MainGame.me != null)
            Intro.OnIntroAnimationFinished();

        Traverse.Create(intro).Field("_on_finished").GetValue<Action>().TryInvoke();
    }
}
