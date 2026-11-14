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
    private readonly ILogger _logger;

    public SharedItemGrants(IEnumerable<ClientContractItem> enabledItems, ISharedGrantActions actions,
        ILogger logger)
    {
        _grants = enabledItems.ToDictionary(item => item.Name, item => item.Grant, StringComparer.Ordinal);
        _actions = actions;
        _logger = logger;
    }

    // The caller supplies the number of confirmed receipts of this exact name in the active save.
    public GrantResult TryApply(string itemName, int previousReceipts, Action<string> queueTrap)
    {
        if (previousReceipts < 0)
            throw new ArgumentOutOfRangeException(nameof(previousReceipts));

        if (itemName == null || !_grants.TryGetValue(itemName, out var grant))
        {
            _logger.LogError($"No enabled shared grant for received item '{itemName}'.");
            return GrantResult.Retry;
        }

        var result = GrantResult.Retry;
        try
        {
            ItemProcessor.ApplyItem(() => result = Apply(itemName, grant, previousReceipts, queueTrap));
        }
        catch (UndeliveredPhysicalItemException ex)
        {
            _logger.LogWarning(ex.Message);
        }
        return result;
    }

    private GrantResult Apply(string name, ClientContractGrant grant, int previousReceipts, Action<string> queueTrap)
    {
        switch (grant.Type)
        {
            case "trap":
                queueTrap(grant.TrapID);
                break;
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
                return _actions.TryCompleteTechnologyUnlocks(grant.TechnologyIDs) ? GrantResult.Applied : GrantResult.Pending;
            case "flow_script":
                return _actions.TryCompleteFlowScript(grant.ScriptID) ? GrantResult.Applied : GrantResult.Pending;
            default:
                _logger.LogError($"Unsupported grant type '{grant.Type}' for received item '{name}'.");
                return GrantResult.Retry;
        }

        return GrantResult.Applied;
    }

    private void Unlock(IEnumerable<string> ids, PlayerUtilities.UnlockType type)
        => _actions.ApplyUnlocks(ids.Select(id => (type, id)));
}
