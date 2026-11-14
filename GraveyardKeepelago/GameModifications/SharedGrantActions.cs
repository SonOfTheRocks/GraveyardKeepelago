using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KaitoKid.Utilities.Interfaces;
using GraveyardKeepelago.GameModifications.Patches;

namespace GraveyardKeepelago.GameModifications;

internal sealed class SharedGrantActions : ISharedGrantActions
{
    private readonly GameSave _save;
    private readonly ILogger _logger;
    private CustomFlowScript _flow;
    private Queue<string> _technologyFlows;
    private bool _flowFinished;
    private bool _flowStarted;

    public SharedGrantActions(GameSave save, ILogger logger)
    {
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _logger = logger;
    }

    private WorldGameObject Player
    {
        get
        {
            var game = MainGame.me;
            if (!ReferenceEquals(game?.save, _save) || game.player == null)
                throw new InvalidOperationException("Cannot grant an AP item outside its active save.");
            return game.player;
        }
    }

    public void ApplyUnlocks(IEnumerable<(PlayerUtilities.UnlockType Type, string ID)> unlocks)
    {
        _ = Player;
        foreach (var (type, rawID) in unlocks)
        {
            var id = Technologies.TechnologyGameChecks.Canonical(rawID);
            if (string.IsNullOrEmpty(id))
                throw new InvalidDataException($"Missing ID for AP {type} unlock.");

            switch (type)
            {
                case PlayerUtilities.UnlockType.Craft:
                    _save.UnlockCraft(id);
                    break;
                case PlayerUtilities.UnlockType.Work:
                    if (!_save.unlocked_works.Contains(id))
                        _save.unlocked_works.Add(id);
                    break;
                case PlayerUtilities.UnlockType.Perk:
                    _save.UnlockPerk(id);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unlocks), type, "Unsupported AP unlock type.");
            }
        }
    }

    public void ApplyPermanentBuff(string buffID)
    {
        _ = Player;
        if (GameBalance.me.GetDataOrNull<BuffDefinition>(buffID) == null)
            throw new InvalidDataException($"Unknown game buff '{buffID}'.");

        if (_save.buffs.Any(active => active.buff_id == buffID && PermanentBuffUpdatePatch.IsPermanent(active.end_time)))
            return;

        BuffsLogics.AddBuff(buffID);
        var buff = _save.buffs.FirstOrDefault(active => active.buff_id == buffID);
        if (buff == null)
            throw new InvalidDataException($"Could not apply AP buff '{buffID}'.");
        buff.end_time = float.MaxValue;
    }

    public void ApplyRelation(string npcID, int amount)
    {
        var player = Player;
        _save.known_npcs.GetOrCreateNPC(npcID);
        player.data.AddToParams("_rel_" + npcID, (float)amount);
        try
        {
            WorldMap.GetWorldGameObjectByObjId(npcID, ignore_not_found_error: true)?.ShowRelationChangeBubble(amount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Could not show relation change for '{npcID}': {ex}");
        }
    }

    public void DeliverItem(string itemID)
    {
        var player = Player;
        var item = new Item(itemID, 1);
        if (item.definition == null)
            throw new InvalidDataException($"Unknown game item '{itemID}'.");

        PhysicalItemDelivery.Deliver(itemID, !item.definition.player_cant_throw_out,
            () => player.AddToInventory(item),
            () =>
            {
                var character = player.components.character;
                return DropResGameObject.Drop(character.tf.position, item, character.tf.parent,
                    Direction.ToPlayer, force_stacked_drop: true) != null;
            });
    }

    public void AddResearchPoints(string color, int amount)
    {
        var player = Player;
        var counter = color switch
        {
            "red" => "r",
            "green" => "g",
            "blue" => "b",
            _ => throw new InvalidDataException($"Unknown research point color '{color}'."),
        };
        // The native drop credits this same saved counter only after the animation.
        player.AddToParams(counter, amount);
        _save.achievements.CheckKeyQuests("tech_collect_" + counter, amount);
        GUIElements.me?.hud?.tech_points_bar?.Redraw();
    }

    public bool TryCompleteTechnologyUnlocks(IEnumerable<string> technologyIDs)
    {
        _ = Player;
        if (_technologyFlows == null)
        {
            _technologyFlows = new Queue<string>();
            foreach (var id in technologyIDs)
            {
                if (_save.unlocked_techs.Contains(id))
                    continue;

                var tech = GameBalance.me.GetDataOrNull<TechDefinition>(id);
                if (tech == null)
                    throw new InvalidDataException($"Unknown game technology '{id}'.");

                // The technology patch applies the unlock and leaves its flow here to await.
                _save.UnlockTech(id);
                if (!_save.unlocked_techs.Contains(id))
                    throw new InvalidDataException($"Could not unlock AP technology '{id}'.");
                if (!string.IsNullOrEmpty(tech.flowscript))
                    _technologyFlows.Enqueue(tech.flowscript);
            }
        }

        while (_technologyFlows.Count > 0)
        {
            if (!RunFlowToCompletion(_technologyFlows.Peek()))
                return false;
            _technologyFlows.Dequeue();
        }

        _technologyFlows = null;
        return true;
    }

    public bool TryCompleteFlowScript(string scriptID)
    {
        _ = Player;
        // The v9 blueprint starts a cutscene that later places this tagged object.
        if (scriptID != "elevator_and_supplies_place")
            throw new InvalidDataException($"No completion check for AP flow script '{scriptID}'.");

        if (!_flowStarted && ElevatorExists())
            return true;
        if (!RunFlowToCompletion(scriptID))
            return false;
        if (!ElevatorExists())
            throw new InvalidDataException($"AP flow script '{scriptID}' ended without placing the elevator.");

        return true;
    }

    private bool RunFlowToCompletion(string scriptID)
    {
        if (!_flowStarted)
        {
            _flow = GS.RunFlowScript(scriptID, _ => _flowFinished = true);
            _flowStarted = true;
            if (_flow == null)
                throw new InvalidDataException($"Could not start AP flow script '{scriptID}'.");
        }

        if (!_flowFinished)
        {
            if (_flow == null)
                throw new InvalidDataException($"AP flow script '{scriptID}' stopped before completion.");
            return false;
        }

        _flow = null;
        _flowStarted = false;
        _flowFinished = false;
        return true;
    }

    internal void StopPendingFlow()
    {
        if (_flow != null)
            _flow.TerminateMe();
    }

    private static bool ElevatorExists() =>
        WorldMap.GetWorldGameObjectByCustomTag("elevator_top", ignore_not_found_error: true) != null;
}
