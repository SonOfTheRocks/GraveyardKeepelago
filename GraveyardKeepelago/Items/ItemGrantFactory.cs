using System;
using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.GameModifications;
using GraveyardKeepelago.Items.Grants;
using GraveyardKeepelago.Logic;
using KaitoKid.Utilities.Interfaces;
using static GraveyardKeepelago.GameModifications.PlayerUtilities.UnlockType;

namespace GraveyardKeepelago.Items;

internal sealed class ItemGrantFactory
{
    private readonly Dictionary<string, ClientContractGrant> _definitions;
    private readonly ILogger _logger;

    public ItemGrantFactory(IEnumerable<ClientContractItem> enabledItems, ILogger logger)
    {
        _definitions = enabledItems.ToDictionary(item => item.Name, item => item.Grant, StringComparer.Ordinal);
        _logger = logger;
    }

    public IItemGrant Create(string itemName, int previousReceipts)
    {
        if (previousReceipts < 0)
            throw new ArgumentOutOfRangeException(nameof(previousReceipts));

        if (itemName == null || !_definitions.TryGetValue(itemName, out var grant))
        {
            _logger.LogError($"No enabled shared grant for received item '{itemName}'.");
            return null;
        }

        switch (grant.Type)
        {
            case "trap": return new TrapGrant(grant.TrapID);
            case "game_item": return new PhysicalItemGrant(grant.ItemID);
            case "craft_unlock": return Unlock(Craft, grant.CraftIDs);
            case "work_unlock":
                return new UnlockGrant(grant.WorkIDs.Select(id => (Work, id))
                    .Concat(grant.PerkIDs.Select(id => (Perk, id))).ToArray());
            case "perk_unlock":
                return new UnlockGrant(new[] { (Perk, grant.PerkID) }
                    .Concat(grant.CraftIDs.Select(id => (Craft, id)))
                    .Concat(grant.WorkIDs.Select(id => (Work, id))).ToArray());
            case "progressive_craft_unlock":
            case "progressive_work_unlock":
                return previousReceipts < grant.Stages.Count
                    ? Unlock(grant.Type == "progressive_craft_unlock" ? Craft : Work, grant.Stages[previousReceipts])
                    : new CompletedGrant();
            case "permanent_buff": return new PermanentBuffGrant(grant.BuffID);
            case "relation": return new RelationGrant(grant.NpcID, grant.Amount);
            case "research_points": return new ResearchPointsGrant(grant.Color, grant.Amount);
            case "technology_unlock": return new TechnologyUnlockGrant(grant.TechnologyIDs);
            case "flow_script": return new FlowScriptGrant(grant.ScriptID);
            default:
                _logger.LogError($"Unsupported grant type '{grant.Type}' for received item '{itemName}'.");
                return null;
        }
    }

    private static UnlockGrant Unlock(PlayerUtilities.UnlockType type, IEnumerable<string> ids)
        => new(ids.Select(id => (type, id)).ToArray());

    internal sealed class CompletedGrant : IItemGrant
    {
        public GrantResult Apply(GrantContext context) => GrantResult.Applied;
    }
}
