using System;
using System.Collections.Generic;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Technologies;

namespace GraveyardKeepelago.Locations;

internal static class LocationTriggerTypes
{
    public const string FlowQuestReward = "flow_quest_reward";
    public const string FlowRelationReward = "flow_relation_reward";
    public const string CraftCompletionReward = "craft_completion_reward";
    public const string CraftUnlockReward = "craft_unlock_reward";
    public const string TechnologyReward = "technology_reward";
    public const string TechnologyResearch = "technology_research";
    public const string WorldItemReward = "world_item_reward";
}

public sealed class LocationTriggerMatch
{
    public LocationTriggerMatch(string locationName, string triggerType)
    {
        LocationName = locationName;
        TriggerType = triggerType;
    }

    public string LocationName { get; }
    public string TriggerType { get; }
}

public sealed class LocationTriggerRegistry
{
    private readonly Dictionary<(string Graph, int NodeID, int? AnswerIndex), List<LocationTriggerMatch>>
        _flowTriggers = new();
    private readonly Dictionary<(string Type, string ID), List<LocationTriggerMatch>> _otherTriggers = new();

    public LocationTriggerRegistry(ClientContract contract, IEnumerable<string> activeLocations,
        TechnologyCatalog technologyCatalog = null)
    {
        if (contract == null)
            throw new ArgumentNullException(nameof(contract));
        var activeNames = new HashSet<string>(activeLocations, StringComparer.Ordinal);

        foreach (var location in contract.Locations.Values)
        {
            if (!activeNames.Contains(location.Name))
                continue;

            foreach (var trigger in location.Triggers)
            {
                var match = new LocationTriggerMatch(location.Name, trigger.Type);
                switch (trigger.Type)
                {
                    case LocationTriggerTypes.FlowQuestReward:
                    case LocationTriggerTypes.FlowRelationReward:
                        if (string.IsNullOrWhiteSpace(trigger.Graph) || trigger.RuntimeNodeID <= 0 ||
                            trigger.AnswerIndex < 0)
                            throw new ArgumentException($"Invalid flow trigger for '{location.Name}'");
                        Add(_flowTriggers, (trigger.Graph, trigger.RuntimeNodeID, trigger.AnswerIndex), match);
                        break;
                    case LocationTriggerTypes.CraftCompletionReward:
                    case LocationTriggerTypes.CraftUnlockReward:
                        AddOther(trigger.CraftID, match);
                        break;
                    case LocationTriggerTypes.TechnologyReward:
                        AddOther(trigger.TechnologyID, match);
                        break;
                    case LocationTriggerTypes.WorldItemReward:
                        AddOther(trigger.ItemID, match);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported location trigger type '{trigger.Type}'");
                }
            }
        }

        if (technologyCatalog == null)
            return;

        foreach (var technology in technologyCatalog.Definitions)
        {
            foreach (var name in technology.Value.Locations)
            {
                if (activeNames.Contains(name))
                    AddOther(technology.Key, new LocationTriggerMatch(name, LocationTriggerTypes.TechnologyResearch));
            }
        }
    }

    public IReadOnlyList<LocationTriggerMatch> GetFlow(string graph, int nodeID, int? answerIndex = null)
        => Get(_flowTriggers, (graph, nodeID, answerIndex));

    public IReadOnlyList<LocationTriggerMatch> GetCraftCompletion(string craftID)
        => Get(_otherTriggers, (LocationTriggerTypes.CraftCompletionReward, craftID));

    public IReadOnlyList<LocationTriggerMatch> GetCraftUnlock(string craftID)
        => Get(_otherTriggers, (LocationTriggerTypes.CraftUnlockReward, craftID));

    public IReadOnlyList<LocationTriggerMatch> GetTechnology(string technologyID)
        => Get(_otherTriggers, (LocationTriggerTypes.TechnologyReward, technologyID));

    public IReadOnlyList<LocationTriggerMatch> GetTechnologyResearch(string technologyID)
        => Get(_otherTriggers, (LocationTriggerTypes.TechnologyResearch, technologyID));

    public IReadOnlyList<LocationTriggerMatch> GetWorldItem(string itemID)
        => Get(_otherTriggers, (LocationTriggerTypes.WorldItemReward, itemID));

    private void AddOther(string id, LocationTriggerMatch match)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException($"Trigger for '{match.LocationName}' is missing its identifier");
        Add(_otherTriggers, (match.TriggerType, id), match);
    }

    private static void Add<TKey>(Dictionary<TKey, List<LocationTriggerMatch>> target, TKey key,
        LocationTriggerMatch match)
    {
        if (!target.TryGetValue(key, out var matches))
        {
            matches = new List<LocationTriggerMatch>();
            target.Add(key, matches);
        }

        matches.Add(match);
    }

    private static IReadOnlyList<LocationTriggerMatch> Get<TKey>(
        Dictionary<TKey, List<LocationTriggerMatch>> source, TKey key)
        => source.TryGetValue(key, out var matches) ? matches : Array.Empty<LocationTriggerMatch>();
}
