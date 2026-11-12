using System;
using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.GameModifications;
using GraveyardKeepelago.Logic;
using KaitoKid.Utilities.Interfaces;
using static GraveyardKeepelago.GameModifications.PlayerUtilities.UnlockType;

namespace GraveyardKeepelago.Items;

internal sealed class SharedItemGrants
{
    private readonly Dictionary<string, ClientContractGrant> _grants;
    private readonly ISharedGrantActions _actions;
    private readonly Func<string, bool> _acceptTrap;
    private readonly ILogger _logger;

    public SharedItemGrants(IEnumerable<ClientContractItem> enabledItems, ISharedGrantActions actions,
        Func<string, bool> acceptTrap, ILogger logger)
    {
        _grants = enabledItems.ToDictionary(item => item.Name, item => item.Grant, StringComparer.Ordinal);
        _actions = actions;
        _acceptTrap = acceptTrap;
        _logger = logger;
    }

    // The caller supplies the number of confirmed receipts of this exact name in the active save.
    public bool TryApply(string itemName, int previousReceipts)
    {
        if (previousReceipts < 0)
            throw new ArgumentOutOfRangeException(nameof(previousReceipts));

        if (itemName == null || !_grants.TryGetValue(itemName, out var grant))
        {
            _logger.LogError($"No enabled shared grant for received item '{itemName}'.");
            return false;
        }

        var applied = false;
        ItemProcessor.ApplyItem(() => applied = Apply(itemName, grant, previousReceipts));
        return applied;
    }

    private bool Apply(string name, ClientContractGrant grant, int previousReceipts)
    {
        switch (grant.Type)
        {
            case "trap":
                return _acceptTrap(grant.TrapID);
            case "game_item":
                _actions.DeliverItem(grant.ItemID);
                break;
            case "craft_unlock":
                Unlock(grant.CraftIDs, Craft);
                break;
            case "work_unlock":
                _actions.ApplyUnlocks(grant.WorkIDs.Select(id => (Work, id))
                    .Concat(grant.PerkIDs.Select(id => (Perk, id))));
                break;
            case "perk_unlock":
                _actions.ApplyUnlocks(new[] { (Perk, grant.PerkID) }
                    .Concat(grant.CraftIDs.Select(id => (Craft, id)))
                    .Concat(grant.WorkIDs.Select(id => (Work, id))));
                break;
            case "progressive_craft_unlock":
            case "progressive_work_unlock":
                if (previousReceipts < grant.Stages.Count)
                    Unlock(grant.Stages[previousReceipts], grant.Type == "progressive_craft_unlock" ? Craft : Work);
                break;
            case "permanent_buff":
                _actions.ApplyPermanentBuff(grant.BuffID);
                break;
            case "relation":
                _actions.ApplyRelation(grant.NpcID, grant.Amount);
                break;
            case "research_points":
                _actions.AddResearchPoints(grant.Color, grant.Amount);
                break;
            case "technology_unlock":
                _actions.UnlockTechnologies(grant.TechnologyIDs);
                break;
            case "flow_script":
                _actions.RunFlowScript(grant.ScriptID);
                break;
            default:
                _logger.LogError($"Unsupported grant type '{grant.Type}' for received item '{name}'.");
                return false;
        }

        return true;
    }

    private void Unlock(IEnumerable<string> ids, PlayerUtilities.UnlockType type)
        => _actions.ApplyUnlocks(ids.Select(id => (type, id)));
}
