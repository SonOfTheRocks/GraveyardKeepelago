using System;
using System.Collections.Generic;
using GraveyardKeepelago.Logic;

namespace GraveyardKeepelago.Locations;

public static class LocationTriggerTypes
{
    public const string FlowQuestReward = "flow_quest_reward";
    public const string FlowRelationReward = "flow_relation_reward";
    public const string CraftCompletionReward = "craft_completion_reward";
    public const string CraftUnlockReward = "craft_unlock_reward";
    public const string TechnologyReward = "technology_reward";
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
    private readonly Dictionary<FlowTriggerKey, List<LocationTriggerMatch>> _flowTriggers =
        new Dictionary<FlowTriggerKey, List<LocationTriggerMatch>>();
    private readonly Dictionary<string, List<LocationTriggerMatch>> _craftCompletionTriggers =
        new Dictionary<string, List<LocationTriggerMatch>>(StringComparer.Ordinal);
    private readonly Dictionary<string, List<LocationTriggerMatch>> _craftUnlockTriggers =
        new Dictionary<string, List<LocationTriggerMatch>>(StringComparer.Ordinal);
    private readonly Dictionary<string, List<LocationTriggerMatch>> _technologyTriggers =
        new Dictionary<string, List<LocationTriggerMatch>>(StringComparer.Ordinal);
    private readonly Dictionary<string, List<LocationTriggerMatch>> _worldItemTriggers =
        new Dictionary<string, List<LocationTriggerMatch>>(StringComparer.Ordinal);

    public LocationTriggerRegistry(ClientContract contract)
    {
        if (contract == null)
            throw new ArgumentNullException(nameof(contract));

        foreach (var location in contract.Locations.Values)
        {
            foreach (var trigger in location.Triggers)
            {
                var match = new LocationTriggerMatch(location.Name, trigger.Type);
                switch (trigger.Type)
                {
                    case LocationTriggerTypes.FlowQuestReward:
                    case LocationTriggerTypes.FlowRelationReward:
                        Add(_flowTriggers,
                            new FlowTriggerKey(trigger.Graph, trigger.RuntimeNodeID, trigger.AnswerIndex), match);
                        break;
                    case LocationTriggerTypes.CraftCompletionReward:
                        Add(_craftCompletionTriggers, trigger.CraftID, match);
                        break;
                    case LocationTriggerTypes.CraftUnlockReward:
                        Add(_craftUnlockTriggers, trigger.CraftID, match);
                        break;
                    case LocationTriggerTypes.TechnologyReward:
                        Add(_technologyTriggers, trigger.TechnologyID, match);
                        break;
                    case LocationTriggerTypes.WorldItemReward:
                        Add(_worldItemTriggers, trigger.ItemID, match);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported location trigger type '{trigger.Type}'");
                }
            }
        }
    }

    public IReadOnlyList<LocationTriggerMatch> GetFlow(string graph, int nodeID, int? answerIndex = null)
        => Get(_flowTriggers, new FlowTriggerKey(graph, nodeID, answerIndex));

    public IReadOnlyList<LocationTriggerMatch> GetCraftCompletion(string craftID)
        => Get(_craftCompletionTriggers, craftID);

    public IReadOnlyList<LocationTriggerMatch> GetCraftUnlock(string craftID)
        => Get(_craftUnlockTriggers, craftID);

    public IReadOnlyList<LocationTriggerMatch> GetTechnology(string technologyID)
        => Get(_technologyTriggers, technologyID);

    public IReadOnlyList<LocationTriggerMatch> GetWorldItem(string itemID)
        => Get(_worldItemTriggers, itemID);

    private static void Add<TKey>(Dictionary<TKey, List<LocationTriggerMatch>> target, TKey key,
        LocationTriggerMatch match)
    {
        if (key == null)
            throw new ArgumentException($"Trigger for '{match.LocationName}' is missing its identifier");
        if (!target.TryGetValue(key, out var matches))
        {
            matches = new List<LocationTriggerMatch>();
            target.Add(key, matches);
        }
        matches.Add(match);
    }

    private static IReadOnlyList<LocationTriggerMatch> Get<TKey>(
        Dictionary<TKey, List<LocationTriggerMatch>> source, TKey key)
    {
        if (key != null && source.TryGetValue(key, out var matches))
            return matches;
        return Array.Empty<LocationTriggerMatch>();
    }

    private sealed class FlowTriggerKey : IEquatable<FlowTriggerKey>
    {
        private readonly string _graph;
        private readonly int _nodeID;
        private readonly int? _answerIndex;

        public FlowTriggerKey(string graph, int nodeID, int? answerIndex)
        {
            _graph = graph;
            _nodeID = nodeID;
            _answerIndex = answerIndex;
        }

        public bool Equals(FlowTriggerKey other)
            => other != null && string.Equals(_graph, other._graph, StringComparison.Ordinal) &&
               _nodeID == other._nodeID &&
               _answerIndex == other._answerIndex;

        public override bool Equals(object obj) => Equals(obj as FlowTriggerKey);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = StringComparer.Ordinal.GetHashCode(_graph ?? string.Empty);
                hash = (hash * 397) ^ _nodeID;
                hash = (hash * 397) ^ _answerIndex.GetHashCode();
                return hash;
            }
        }
    }
}
