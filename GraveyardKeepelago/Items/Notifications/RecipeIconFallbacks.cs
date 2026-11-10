using System;
using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.Logic;

namespace GraveyardKeepelago.Items.Notifications;

// Presentation only: pair shared organ-workbench recipe items with their normal
// counterparts. Never change the actual grant IDs or native hidden-unlock flags.
public sealed class RecipeIconFallbacks
{
    private static readonly Lazy<RecipeIconFallbacks> DefaultValue =
        new(() => new RecipeIconFallbacks(ClientContractLoader.LoadDefault()));
    public static RecipeIconFallbacks Default => DefaultValue.Value;
    private readonly Dictionary<string, string[]> _normalCrafts = new(StringComparer.Ordinal);

    public RecipeIconFallbacks(ClientContract contract)
    {
        const string suffix = "_ORGAN_WORKBENCH";
        foreach (var entry in contract.Items)
        {
            if (!entry.Key.StartsWith("ITEM_LEARN_", StringComparison.Ordinal) ||
                !entry.Key.EndsWith(suffix, StringComparison.Ordinal) ||
                !contract.Items.TryGetValue(entry.Key.Substring(0, entry.Key.Length - suffix.Length), out var normal) ||
                entry.Value.Grant.Type != "craft_unlock" || normal.Grant.Type != "craft_unlock") continue;
            var candidates = (normal.Grant.CraftIDs ?? new List<string>())
                .Where(id => !string.IsNullOrEmpty(id) && !id.StartsWith("@", StringComparison.Ordinal)).ToArray();
            foreach (var id in entry.Value.Grant.CraftIDs ?? new List<string>())
                if (!string.IsNullOrEmpty(id)) _normalCrafts[id.TrimStart('@')] = candidates;
        }
    }

    public bool IsOrganRecipe(string id) => id != null && _normalCrafts.ContainsKey(id.TrimStart('@'));

    public string SelectCraft(string id, Func<string, bool> hasIcon)
    {
        if (!IsOrganRecipe(id)) return id;
        var canonical = id.TrimStart('@');
        if (hasIcon(canonical)) return canonical;
        return _normalCrafts[canonical].FirstOrDefault(hasIcon) ?? canonical;
    }
}
