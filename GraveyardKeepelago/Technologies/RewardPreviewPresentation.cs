using GraveyardKeepelago.GameModifications.UI;
using Archipelago.MultiClient.Net.Enums;
using System.Collections.Generic;
using GraveyardKeepelago.Logic;

namespace GraveyardKeepelago.Technologies;

public static class RewardPreviewPresentation
{
    public static bool IsLocalPermanentBuff(RewardPreview preview, string localGame,
        IReadOnlyDictionary<string, ClientContractItem> definitions) =>
        LocalDefinition(preview, localGame, definitions)?.Grant?.Type == "permanent_buff";

    public const string UnlockTitle = "AP items unlocked";
    public const string UnlockSubtitle = "The following AP items were unlocked:";

    // Foreign games retain the Archipelago main icon, even for a matching name.
    // Category flags select the corner badge independently.
    public static ClientContractItem LocalDefinition(RewardPreview preview, string localGame,
        IReadOnlyDictionary<string, ClientContractItem> definitions) =>
        preview != null && preview.Game == localGame && !string.IsNullOrEmpty(preview.Item) &&
        definitions.TryGetValue(preview.Item, out var definition) ? definition : null;

    // Reuse the native quality-corner widget, including its layout and redraw
    // cleanup. AP flag precedence matches the existing item-name palette.
    // Unknown placement flags must not be mistaken for a confirmed filler.
    public static string CategorySpriteName(ItemFlags? flags) => !flags.HasValue ? null :
        (flags.Value & ItemFlags.Advancement) != 0 ? "item_star_3" :
        (flags.Value & ItemFlags.NeverExclude) != 0 ? "item_star_2" :
        (flags.Value & ItemFlags.Trap) != 0 ? "i_skull" : "item_star_1";

    public static string Name(RewardPreview item) => item == null ? "Archipelago …" :
        ApItemPresentation.Name(item.Item, item.Flags, lightBackground: true);
    public static string Description(RewardPreview item, string location) => item == null ? location :
        $"{item.Recipient} · {item.Game}\n{location}";
}
