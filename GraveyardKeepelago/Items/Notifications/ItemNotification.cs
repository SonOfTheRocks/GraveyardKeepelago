using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Archipelago.MultiClient.Net.Enums;
using GraveyardKeepelago.GameModifications.UI;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Journal;
using KaitoKid.ArchipelagoUtilities.Net.Client;

namespace GraveyardKeepelago.Items.Notifications;

public sealed class ItemNotification
{
    public string Name { get; }
    public string SourceText { get; }
    public string HudText { get; }
    public int? RelationAmount { get; }
    public ItemFlags? Flags { get; }
    public string DisplayName => ApItemPresentation.Name(Name, Flags);
    public IReadOnlyList<(string Type, string ID)> Entries { get; }

    public ItemNotification(string name, IEnumerable<(string Type, string ID)> entries, string sourceText = "You received:",
        int? relationAmount = null, ItemFlags? flags = null)
    {
        Name = name;
        SourceText = sourceText;
        RelationAmount = relationAmount;
        Flags = flags;
        HudText = "[c][DDDDDD]" + JournalEntry.Escape(sourceText) + "[-][/c]\n" +
            ApItemPresentation.Name(JournalEntry.Escape(name), flags);
        Entries = entries.ToArray();
    }

    // Capture the applied progressive stage now, rather than when it is eventually displayed.
    public static ItemNotification Create(string name, ClientContractGrant grant, IAPItem item,
        ReceivedItem received = null, long localSlot = -1, ItemFlags? flags = null)
    {
        var entries = new List<(string, string)>();
        void Add(string type, IEnumerable<string> ids)
        {
            if (ids != null)
                entries.AddRange(ids.Where(id => !string.IsNullOrEmpty(id) && !id.StartsWith("@"))
                    .Select(id => (type, id)));
        }

        if (grant != null)
        {
            Add("craft", grant.CraftIDs);
            Add("work", grant.WorkIDs);
            Add("perk", grant.PerkIDs);
            if (grant.PerkID != null) Add("perk", new[] { grant.PerkID });
            if (grant.ItemID != null) Add("item", new[] { grant.ItemID });
            if (grant.BuffID != null) Add("buff", new[] { grant.BuffID });
            if (grant.Type == "relation" && grant.NpcID != null) Add("relation", new[] { NpcIdentity.Canonical(grant.NpcID) });
            if (grant.Type == "research_points") Add("research_points", new[] { grant.Color });
            if (grant.Type == "trap") Add("trap", new[] { grant.TrapID });
            Add("technology", grant.TechnologyIDs);
            if (grant.Type == "technology_unlock" && grant.TechnologyIDs?.Count == 0)
                Add("ap_item", new[] { name });
            if (item is GKProgressiveItem progressive && progressive.applyCount > 0 &&
                progressive.applyCount <= grant.Stages?.Count)
                Add(grant.Type == "progressive_craft_unlock" ? "craft" : "work",
                    grant.Stages[progressive.applyCount - 1]);
            // A hidden-only organ recipe still represents a received AP item.
            // Give its preview an icon source, without changing the hidden grant.
            if (entries.Count == 0 && grant.Type == "craft_unlock" && grant.CraftIDs != null)
            {
                var organ = grant.CraftIDs.FirstOrDefault(id => id != null && id.StartsWith("@") &&
                    RecipeIconFallbacks.Default.IsOrganRecipe(id));
                if (organ != null) entries.Add(("craft", organ.TrimStart('@')));
            }
        }
        return new ItemNotification(name, entries, ReceiptSourceText(received, localSlot),
            grant?.Type == "relation" ? grant.Amount : (int?)null, flags);
    }

    public string RelationText(string npcName) => RelationAmount.HasValue
        ? $"{npcName}\nHappiness {RelationAmount.Value.ToString("+0;-0;0", CultureInfo.InvariantCulture)}"
        : Name;

    public string EntryName(string type, string nativeName) => type == "relation" ? RelationText(nativeName) :
        type == "technology" || type == "research_points" || type == "ap_item" || type == "trap" ? Name : nativeName;

    public string DisplayEntryName(string type, string nativeName) =>
        ApItemPresentation.Name(EntryName(type, nativeName), Flags);

    // The technology window uses the gem-shaped point symbols, not the round pickup icons.
    public static string ResearchPointSpriteName(string color) => color == "red" ? "techno_point_icon04" :
        color == "green" ? "techno_point_icon03" : color == "blue" ? "techno_point_icon02" : null;

    public static string ItemSpriteName(string itemID, string nativeSprite) =>
        itemID == "preach_1" ? "i_b_empty" : nativeSprite;

    public static string ReceiptSourceText(ReceivedItem received, long localSlot)
    {
        if (received == null) return "You received:";
        if (received.PlayerId == localSlot) return "You found:";
        if (received.PlayerId <= 0) return "The server sent you:";
        return string.IsNullOrWhiteSpace(received.PlayerName) ? "Another player sent you:" :
            $"{received.PlayerName} sent you:";
    }

    // Matches NPCItemGUI.Draw in the native NPC/relation overview.
    public static string PortraitSpriteName(string npcID, string alias) =>
        "char_" + (string.IsNullOrEmpty(alias) ? NpcIdentity.Canonical(npcID) : alias);
}

