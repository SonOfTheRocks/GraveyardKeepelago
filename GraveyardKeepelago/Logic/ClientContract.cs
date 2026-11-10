using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace GraveyardKeepelago.Logic;

public sealed class ClientContract
{
    public int DataVersion { get; set; }
    public Dictionary<string, ClientContractItem> Items { get; set; }
    public Dictionary<string, ClientContractLocation> Locations { get; set; }
}

public sealed class ClientContractItem
{
    public string Name { get; set; }
    public string DLC { get; set; }
    public string Classification { get; set; }
    public int Amount { get; set; }
    public ClientContractGrant Grant { get; set; }
}

public sealed class ClientContractGrant
{
    [JsonProperty("trap_id")] public string TrapID { get; set; }
    [JsonProperty("type")] public string Type { get; set; }
    [JsonProperty("item_id")] public string ItemID { get; set; }
    [JsonProperty("craft_ids")] public List<string> CraftIDs { get; set; }
    [JsonProperty("work_ids")] public List<string> WorkIDs { get; set; }
    [JsonProperty("perk_ids")] public List<string> PerkIDs { get; set; }
    [JsonProperty("perk_id")] public string PerkID { get; set; }
    [JsonProperty("stages")] public List<List<string>> Stages { get; set; }
    [JsonProperty("buff_id")] public string BuffID { get; set; }
    [JsonProperty("resource_types")] public List<string> ResourceTypes { get; set; }
    [JsonProperty("icon")] public string Icon { get; set; }
    [JsonProperty("npc_id")] public string NpcID { get; set; }
    [JsonProperty("amount")] public int Amount { get; set; }
    [JsonProperty("color")] public string Color { get; set; }
    [JsonProperty("technology_ids")] public List<string> TechnologyIDs { get; set; }
    [JsonProperty("script_id")] public string ScriptID { get; set; }
}

public sealed class ClientContractLocation
{
    public string Name { get; set; }
    public string ParentRegion { get; set; }
    public List<ClientContractLocationTrigger> Triggers { get; set; }
}

public sealed class ClientContractLocationTrigger
{
    [JsonProperty("type")] public string Type { get; set; }
    [JsonProperty("graph")] public string Graph { get; set; }
    [JsonProperty("node_id")] public string NodeID { get; set; }
    [JsonProperty("runtime_node_id")] public int RuntimeNodeID { get; set; }
    [JsonProperty("answer_index")] public int? AnswerIndex { get; set; }
    [JsonProperty("craft_id")] public string CraftID { get; set; }
    [JsonProperty("item_id")] public string ItemID { get; set; }
    [JsonProperty("technology_id")] public string TechnologyID { get; set; }
}

internal sealed class SharedManifest
{
    [JsonProperty("data_version")] public int DataVersion { get; set; }
}

internal sealed class SharedItemDefinition
{
    [JsonProperty("label")] public string Label { get; set; }
    [JsonProperty("dlc")] public string DLC { get; set; }
    [JsonProperty("classification")] public string Classification { get; set; }
    [JsonProperty("amount")] public int? Amount { get; set; }
}

internal sealed class SharedLocationDefinition
{
    [JsonProperty("label")] public string Label { get; set; }
    [JsonProperty("parent_region")] public string ParentRegion { get; set; }
}

public static class ClientContractLoader
{
    public static ClientContract LoadDefault()
    {
        var manifest = SharedGameData.Read<SharedManifest>("manifest.json");
        if (manifest.DataVersion < 1)
            throw new InvalidDataException("Shared Graveyard Keeper data_version must be positive");

        var definitions = new Dictionary<string, SharedItemDefinition>(StringComparer.Ordinal);
        foreach (var path in SharedGameData.Files.Where(path => path.StartsWith("items/", StringComparison.Ordinal)))
        {
            foreach (var entry in SharedGameData.Read<Dictionary<string, SharedItemDefinition>>(path))
            {
                if (definitions.ContainsKey(entry.Key))
                    throw new InvalidDataException($"Shared item '{entry.Key}' is defined more than once");
                definitions.Add(entry.Key, entry.Value);
            }
        }

        var grants = SharedGameData.Read<Dictionary<string, ClientContractGrant>>("item_grants.json");
        if (definitions.Count != grants.Count)
            throw new InvalidDataException("Shared item definitions and item grants have different counts");

        var items = new Dictionary<string, ClientContractItem>(StringComparer.Ordinal);
        var itemNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in definitions)
        {
            if (!grants.TryGetValue(entry.Key, out var grant))
                throw new InvalidDataException($"Shared item '{entry.Key}' has no client grant");
            var definition = entry.Value;
            var amount = definition.Amount ?? 1;
            if (string.IsNullOrWhiteSpace(definition.Label) || string.IsNullOrWhiteSpace(definition.DLC) ||
                string.IsNullOrWhiteSpace(definition.Classification) || amount < 1 ||
                grant == null || string.IsNullOrWhiteSpace(grant.Type))
                throw new InvalidDataException($"Shared item '{entry.Key}' has invalid metadata");
            if (!itemNames.Add(definition.Label))
                throw new InvalidDataException($"Duplicate shared item name '{definition.Label}'");
            items.Add(entry.Key, new ClientContractItem
            {
                Name = definition.Label, DLC = definition.DLC, Classification = definition.Classification,
                Amount = amount, Grant = grant,
            });
        }

        var locationDefinitions = SharedGameData.Read<Dictionary<string, SharedLocationDefinition>>("locations.json");
        var triggers = SharedGameData.Read<Dictionary<string, List<ClientContractLocationTrigger>>>("quest_reward_triggers.json");
        var locations = new Dictionary<string, ClientContractLocation>(StringComparer.Ordinal);
        var locationNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in locationDefinitions)
        {
            if (string.IsNullOrWhiteSpace(entry.Value.Label) || string.IsNullOrWhiteSpace(entry.Value.ParentRegion))
                throw new InvalidDataException($"Shared location '{entry.Key}' has invalid metadata");
            if (!locationNames.Add(entry.Value.Label))
                throw new InvalidDataException($"Duplicate shared location name '{entry.Value.Label}'");
            locations.Add(entry.Key, new ClientContractLocation
            {
                Name = entry.Value.Label,
                ParentRegion = entry.Value.ParentRegion,
                Triggers = triggers.TryGetValue(entry.Key, out var locationTriggers)
                    ? locationTriggers
                    : new List<ClientContractLocationTrigger>(),
            });
        }
        foreach (var key in triggers.Keys)
            if (!locations.ContainsKey(key))
                throw new InvalidDataException($"Shared triggers reference unknown location '{key}'");

        return new ClientContract { DataVersion = manifest.DataVersion, Items = items, Locations = locations };
    }
}
