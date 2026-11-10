using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GraveyardKeepelago.Logic;
using Newtonsoft.Json;

namespace GraveyardKeepelago.Technologies;

public sealed class TechnologyDefinition
{
    [JsonProperty("region")] public string Region { get; set; }
    [JsonProperty("dlc")] public string DLC { get; set; }
    [JsonProperty("locations")] public string[] Locations { get; set; }
    [JsonProperty("parents")] public string[] Parents { get; set; } = Array.Empty<string>();
    [JsonProperty("recipe_item")] public string RecipeItem { get; set; }
    [JsonProperty("quest_reward")] public string QuestReward { get; set; }
}

public sealed class TechnologyCatalog
{
    public IReadOnlyDictionary<string, TechnologyDefinition> Definitions { get; }
    public TechnologyCatalog(Dictionary<string, TechnologyDefinition> definitions)
    {
        if (definitions == null || definitions.Count == 0 || definitions.Any(entry =>
                string.IsNullOrEmpty(entry.Key) || entry.Value == null || entry.Value.Locations == null ||
                entry.Value.Locations.Length != (entry.Value.QuestReward != null ? 0 : entry.Value.RecipeItem != null ? 1 : 3) ||
                entry.Value.Locations.Any(string.IsNullOrWhiteSpace) ||
                entry.Value.Locations.Distinct().Count() != entry.Value.Locations.Length ||
                entry.Value.RecipeItem != null && string.IsNullOrWhiteSpace(entry.Value.RecipeItem) ||
                entry.Value.QuestReward != null && string.IsNullOrWhiteSpace(entry.Value.QuestReward) ||
                entry.Value.RecipeItem != null && entry.Value.QuestReward != null ||
                entry.Value.Parents == null || entry.Value.Parents.Any(parent => parent == entry.Key || !definitions.ContainsKey(parent))) ||
            definitions.Values.SelectMany(definition => definition.Locations).Distinct().Count() !=
                definitions.Values.Sum(definition => definition.Locations.Length))
            throw new InvalidDataException("Invalid technology-to-location mapping.");
        Definitions = definitions;
    }
    public static TechnologyCatalog LoadDefault()
    {
        var definitions = SharedGameData.Read<Dictionary<string, TechnologyDefinition>>("technologies.json");
        return new TechnologyCatalog(definitions);
    }
}
