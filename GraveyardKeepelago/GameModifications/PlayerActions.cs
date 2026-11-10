using System;
using System.Collections.Generic;
using HarmonyLib;
using GraveyardKeepelago.Locations;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.GameModifications;

public class PlayerActions : IPlayerActions
{
    private Action<string> _queueTrap;
    public void SetTrapQueue(Action<string> queueTrap) => _queueTrap = queueTrap;
    public void QueueTrap(string trapID)
    {
        if (_queueTrap == null) throw new InvalidOperationException("Trap queue is not initialized for this save.");
        _queueTrap(trapID);
    }

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
        var runtime = GraveyardKeepelago.Technologies.TechnologyRuntime.Current;
        if (runtime == null || !runtime.IsActive)
            throw new InvalidOperationException("Cannot apply AP unlocks without an active save.");
        runtime.ApplyUnlocks(unlocks);
    }

    public void ApplyRelation(string npcID, int amount)
    {
        RelationReward.Apply(npcID, amount,
            id => MainGame.me.save.known_npcs.GetOrCreateNPC(id),
            (id, delta) => MainGame.me.player.data.AddToParams("_rel_" + id, (float)delta),
            (id, delta) => WorldMap.GetWorldGameObjectByObjId(id, ignore_not_found_error: true)?.ShowRelationChangeBubble(delta),
            ex => _logger.LogWarning($"Relation reward applied; optional NPC bubble could not be shown: {ex}"));
    }

    public void AddResearchPoints(string color, int amount)
    {
        var pos = MainGame.me.player.transform.position + MainGame.me.player.transform.forward;
        TechPointsDrop.Drop(
            pos,
            color == "red" ? amount : 0,
            color == "green" ? amount : 0,
            color == "blue" ? amount : 0
        );
    }

    public void UnlockTechnologies(List<string> technologyIDs)
    {
        foreach (var technologyID in technologyIDs)
            MainGame.me.save.UnlockTech(technologyID);
    }

    public void RunFlowScript(string scriptID)
    {
        GS.RunFlowScript(scriptID);
    }

    public void DropItem(string itemID, int amount = 1)
    {
        var player = MainGame.me.player;
        var pos = player.transform.position + player.transform.forward;

        _logger.LogDebug($"Dropping Item '{itemID}'");

        const int techpointAmount = 100;
        if (itemID.StartsWith("techpoints"))
        {
            AddResearchPoints(
                itemID.EndsWith("red") ? "red" : itemID.EndsWith("green") ? "green" : "blue",
                techpointAmount
            );
            return;
        }

        var item = new Item(itemID, amount);
        if (LocationTriggerRuntime.IsApplyingArchipelagoItem)
            LocationTriggerRuntime.MarkArchipelagoItem(item);
        PhysicalItemDelivery.Deliver(!item.definition.player_cant_throw_out,
            () => player.AddToInventory(item),
            () =>
            {
                var character = player.components.character;
                // Native Drop returns null for a successful split multi-item drop.
                // A stacked drop gives us the spawned object for positive acknowledgement.
                var dropped = DropResGameObject.Drop(character.tf.position, item, character.tf.parent,
                    Direction.ToPlayer, force_stacked_drop: true);
                if (dropped == null) return false;
                // Native replacement/splitting may copy the item. Keep its AP provenance.
                if (LocationTriggerRuntime.IsApplyingArchipelagoItem)
                    LocationTriggerRuntime.MarkArchipelagoItem(dropped.res);
                return true;
            });
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
