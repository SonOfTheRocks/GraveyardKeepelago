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
    private static readonly HashSet<string> QuestRewardNodeTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "Flow_DropItem",
        "Flow_AddItemToWGO",
    };

    private static readonly HashSet<string> RelationRewardNodeTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "Flow_AddRelation",
        "Flow_AddWGOParam",
    };

    private readonly ILocationChecker _locationChecker;
    private readonly LocationTriggerRegistry _registry;

    public LocationTriggerService(ILocationChecker locationChecker, LocationTriggerRegistry registry)
    {
        _locationChecker = locationChecker ?? throw new ArgumentNullException(nameof(locationChecker));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public FlowRewardSuppression HandleFlowChoice(string graph, int nodeID, int answerIndex)
    {
        if (string.IsNullOrEmpty(graph))
            return FlowRewardSuppression.None;

        var matches = _registry.GetFlow(graph, nodeID, answerIndex);
        Check(matches);

        var suppression = FlowRewardSuppression.None;
        if (matches.Any(match => match.TriggerType == LocationTriggerTypes.FlowQuestReward))
            suppression |= FlowRewardSuppression.Quest;
        if (matches.Any(match => match.TriggerType == LocationTriggerTypes.FlowRelationReward))
            suppression |= FlowRewardSuppression.Relation;
        return suppression;
    }

    public bool HandleFlowNode(string graph, int nodeID, string nodeType,
        FlowRewardSuppression inheritedSuppression = FlowRewardSuppression.None)
    {
        if (string.IsNullOrEmpty(graph) || nodeID < 0 || string.IsNullOrEmpty(nodeType))
            return false;

        var matches = _registry.GetFlow(graph, nodeID);
        Check(matches);

        var isQuestRewardNode = QuestRewardNodeTypes.Contains(nodeType);
        var isRelationRewardNode = RelationRewardNodeTypes.Contains(nodeType);
        var directlyReplacesQuestReward = isQuestRewardNode &&
                                          matches.Any(match => match.TriggerType == LocationTriggerTypes.FlowQuestReward);
        var directlyReplacesRelationReward = isRelationRewardNode &&
                                             matches.Any(match => match.TriggerType == LocationTriggerTypes.FlowRelationReward);

        return directlyReplacesQuestReward || directlyReplacesRelationReward ||
               isQuestRewardNode && (inheritedSuppression & FlowRewardSuppression.Quest) != 0 ||
               isRelationRewardNode && (inheritedSuppression & FlowRewardSuppression.Relation) != 0;
    }

    public void HandleCraftCompletion(string craftID) => Check(_registry.GetCraftCompletion(craftID));

    public bool HandleCraftUnlock(string craftID)
    {
        var matches = _registry.GetCraftUnlock(craftID);
        Check(matches);
        return matches.Count > 0;
    }

    public bool ShouldSuppressCraftReveal(IEnumerable<string> craftIDs)
        => craftIDs != null && craftIDs.Any(craftID => _registry.GetCraftUnlock(craftID).Count > 0);

    public bool HandleTechnology(string technologyID)
    {
        var matches = _registry.GetTechnology(technologyID);
        Check(matches);
        return matches.Count > 0;
    }

    public bool HandleTechnologyDialog(string technologyID, bool forcedUnlock, bool revealTech, bool pseudotech)
        => forcedUnlock && !revealTech && !pseudotech && HandleTechnology(technologyID);

    public bool HandleWorldItem(string itemID)
    {
        var matches = _registry.GetWorldItem(itemID);
        Check(matches);
        return matches.Count > 0;
    }

    private void Check(IReadOnlyList<LocationTriggerMatch> matches)
    {
        if (matches.Count == 0)
            return;
        _locationChecker.AddCheckedLocations(matches.Select(match => match.LocationName).Distinct().ToArray());
    }

}
