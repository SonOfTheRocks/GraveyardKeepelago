using System;
using System.Collections.Generic;
using System.Linq;

namespace GraveyardKeepelago.Locations;

[Flags]
public enum FlowRewardSuppression
{
    None = 0,
    Quest = 1,
    Relation = 2,
}

public sealed class LocationTriggerService
{
    private readonly ILocationChecker _locationChecker;
    private readonly LocationTriggerRegistry _registry;

    public LocationTriggerService(ILocationChecker locationChecker, LocationTriggerRegistry registry)
    {
        _locationChecker = locationChecker ?? throw new ArgumentNullException(nameof(locationChecker));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public FlowRewardSuppression HandleFlowChoice(string graph, int nodeID, int answerIndex)
    {
        var matches = _registry.GetFlow(graph, nodeID, answerIndex);
        Check(matches);
        return GetFlowSuppression(matches);
    }

    public bool HandleFlowNode(string graph, int nodeID, string nodeType,
        FlowRewardSuppression inheritedSuppression = FlowRewardSuppression.None)
    {
        if (string.IsNullOrWhiteSpace(graph) || nodeID <= 0 || string.IsNullOrWhiteSpace(nodeType))
            return false;

        var matches = _registry.GetFlow(graph, nodeID);
        Check(matches);

        var reward = nodeType switch
        {
            "Flow_DropItem" or "Flow_AddItemToWGO" => FlowRewardSuppression.Quest,
            "Flow_AddRelation" or "Flow_AddWGOParam" => FlowRewardSuppression.Relation,
            _ => FlowRewardSuppression.None,
        };
        return ((GetFlowSuppression(matches) | inheritedSuppression) & reward) != 0;
    }

    public void HandleCraftCompletion(string craftID) => Check(_registry.GetCraftCompletion(craftID));

    public bool HandleCraftUnlock(string craftID) => Check(_registry.GetCraftUnlock(craftID));

    public bool ShouldSuppressCraftReveal(IEnumerable<string> craftIDs)
        => craftIDs != null && craftIDs.Any(craftID => _registry.GetCraftUnlock(craftID).Count > 0);

    public bool HandleTechnology(string technologyID) => Check(_registry.GetTechnology(technologyID));

    public bool HandleTechnologyDialog(string technologyID, bool forcedUnlock, bool revealTech, bool pseudotech)
        => forcedUnlock && !revealTech && !pseudotech && HandleTechnology(technologyID);

    public bool HandleWorldItem(string itemID) => Check(_registry.GetWorldItem(itemID));

    private static FlowRewardSuppression GetFlowSuppression(IReadOnlyList<LocationTriggerMatch> matches)
    {
        var suppression = FlowRewardSuppression.None;
        foreach (var match in matches)
            suppression |= match.TriggerType == LocationTriggerTypes.FlowQuestReward
                ? FlowRewardSuppression.Quest
                : FlowRewardSuppression.Relation;
        return suppression;
    }

    private bool Check(IReadOnlyList<LocationTriggerMatch> matches)
    {
        if (matches.Count == 0)
            return false;

        // The checker owns persistence and deduplication across separate events.
        _locationChecker.AddCheckedLocations(matches.Select(match => match.LocationName).Distinct().ToArray());
        return true;
    }
}
