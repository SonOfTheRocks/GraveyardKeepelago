using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace GraveyardKeepelago.Logic;

public sealed class ClientContract
{
    [JsonProperty("schema_version")]
    public int SchemaVersion { get; set; }

    [JsonProperty("game")]
    public string Game { get; set; }

    [JsonProperty("items")]
    public Dictionary<string, ClientContractItem> Items { get; set; }

    [JsonProperty("locations")]
    public Dictionary<string, ClientContractLocation> Locations { get; set; }
}

public sealed class ClientContractItem
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("dlc")]
    public string DLC { get; set; }

    [JsonProperty("classification")]
    public string Classification { get; set; }

    [JsonProperty("amount")]
    public int Amount { get; set; }

    [JsonProperty("grant")]
    public ClientContractGrant Grant { get; set; }
}

public sealed class ClientContractGrant
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("item_id")]
    public string ItemID { get; set; }

    [JsonProperty("craft_ids")]
    public List<string> CraftIDs { get; set; }

    [JsonProperty("work_ids")]
    public List<string> WorkIDs { get; set; }

    [JsonProperty("perk_ids")]
    public List<string> PerkIDs { get; set; }

    [JsonProperty("perk_id")]
    public string PerkID { get; set; }

    [JsonProperty("stages")]
    public List<List<string>> Stages { get; set; }

    [JsonProperty("buff_id")]
    public string BuffID { get; set; }

    [JsonProperty("resource_types")]
    public List<string> ResourceTypes { get; set; }

    [JsonProperty("icon")]
    public string Icon { get; set; }

    [JsonProperty("npc_id")]
    public string NpcID { get; set; }

    [JsonProperty("amount")]
    public int Amount { get; set; }

    [JsonProperty("color")]
    public string Color { get; set; }

    [JsonProperty("technology_ids")]
    public List<string> TechnologyIDs { get; set; }

    [JsonProperty("script_id")]
    public string ScriptID { get; set; }
}

public sealed class ClientContractLocation
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("dlc")]
    public string DLC { get; set; }

    [JsonProperty("parent_region")]
    public string ParentRegion { get; set; }

    [JsonProperty("triggers")]
    public List<ClientContractLocationTrigger> Triggers { get; set; }
}

public sealed class ClientContractLocationTrigger
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("graph")]
    public string Graph { get; set; }

    [JsonProperty("node_id")]
    public string NodeID { get; set; }

    [JsonProperty("answer_index")]
    public int? AnswerIndex { get; set; }

    [JsonProperty("craft_id")]
    public string CraftID { get; set; }

    [JsonProperty("item_id")]
    public string ItemID { get; set; }

    [JsonProperty("technology_id")]
    public string TechnologyID { get; set; }
}

public static class ClientContractLoader
{
    public const int SupportedSchemaVersion = 1;
    public const string FileName = "graveyard_keeper_contract.json";

    public static ClientContract LoadDefault()
    {
        var assemblyDirectory = Path.GetDirectoryName(typeof(ClientContractLoader).Assembly.Location);
        return Load(Path.Combine(assemblyDirectory ?? string.Empty, "Data", FileName));
    }

    public static ClientContract Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("The Graveyard Keeper client contract is missing", path);

        var contract = JsonConvert.DeserializeObject<ClientContract>(File.ReadAllText(path));
        if (contract == null)
            throw new InvalidDataException("The Graveyard Keeper client contract is empty");
        if (contract.SchemaVersion != SupportedSchemaVersion)
            throw new InvalidDataException(
                $"Unsupported client contract schema {contract.SchemaVersion}; expected {SupportedSchemaVersion}"
            );
        if (contract.Game != "Graveyard Keeper")
            throw new InvalidDataException($"Unexpected client contract game '{contract.Game}'");
        if (contract.Items == null || contract.Locations == null)
            throw new InvalidDataException("The client contract must define items and locations");

        var itemNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in contract.Items)
        {
            var item = entry.Value;
            if (string.IsNullOrWhiteSpace(item.Name) || item.Grant == null || string.IsNullOrWhiteSpace(item.Grant.Type))
                throw new InvalidDataException($"Contract item '{entry.Key}' needs a name and grant");
            if (string.IsNullOrWhiteSpace(item.DLC) || string.IsNullOrWhiteSpace(item.Classification) || item.Amount < 1)
                throw new InvalidDataException($"Contract item '{entry.Key}' has invalid metadata");
            if (!itemNames.Add(item.Name))
                throw new InvalidDataException($"Duplicate contract item name '{item.Name}'");
        }

        var locationNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in contract.Locations)
        {
            var location = entry.Value;
            if (string.IsNullOrWhiteSpace(location.Name) || string.IsNullOrWhiteSpace(location.DLC) ||
                string.IsNullOrWhiteSpace(location.ParentRegion) || location.Triggers == null)
                throw new InvalidDataException($"Contract location '{entry.Key}' has invalid metadata");
            if (!locationNames.Add(location.Name))
                throw new InvalidDataException($"Duplicate contract location name '{location.Name}'");
        }

        return contract;
    }
}
