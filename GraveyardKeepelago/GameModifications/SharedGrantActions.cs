using System;
using System.Collections.Generic;
using System.IO;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.GameModifications;

internal sealed class SharedGrantActions : ISharedGrantActions
{
    private readonly GameSave _save;
    private readonly IPlayerActions _legacyActions;
    private readonly ILogger _logger;

    public SharedGrantActions(GameSave save, IPlayerActions legacyActions, ILogger logger)
    {
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _legacyActions = legacyActions;
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
        _legacyActions.ApplyPermanentBuff(buffID);
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
                return DropResGameObject.Drop(character.tf.position, item, character.tf.parent, Direction.ToPlayer) != null;
            });
    }

    public void AddResearchPoints(string color, int amount)
    {
        var player = Player;
        var (red, green, blue) = color switch
        {
            "red" => (amount, 0, 0),
            "green" => (0, amount, 0),
            "blue" => (0, 0, amount),
            _ => throw new InvalidDataException($"Unknown research point color '{color}'."),
        };
        TechPointsDrop.Drop(player.transform.position + player.transform.forward, red, green, blue);
    }

    public void UnlockTechnologies(IEnumerable<string> technologyIDs)
    {
        _ = Player;
        foreach (var id in technologyIDs)
        {
            _save.UnlockTech(id);
            if (!_save.unlocked_techs.Contains(id))
                throw new InvalidDataException($"Could not unlock AP technology '{id}'.");
        }
    }

    public void RunFlowScript(string scriptID)
    {
        _ = Player;
        if (GS.RunFlowScript(scriptID) == null)
            throw new InvalidDataException($"Could not start AP flow script '{scriptID}'.");
    }
}
