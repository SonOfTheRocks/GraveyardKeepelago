using System.Linq;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.MessageLog.Parts;

namespace GraveyardKeepelago.Journal;

public static class JournalMessageAdapter
{
    public static JournalEntry Convert(LogMessage message)
    {
        if (message == null) return null;
        var entry = new JournalEntry
        {
            Kind = message is ChatLogMessage || message is ServerChatLogMessage ? JournalKind.Chat : JournalKind.System,
            Parts = message.Parts.Take(256).Select(part => new JournalPart
            {
                Text = part.Text ?? string.Empty,
                Role = part is ItemMessagePart ? JournalRole.Item : part is PlayerMessagePart ? JournalRole.Player : JournalRole.Text,
                Flags = part is ItemMessagePart item ? item.Flags : 0,
            }).ToList(),
        };
        if (message is ItemSendLogMessage transfer && !(message is HintItemSendLogMessage))
        {
            entry.Kind = JournalKind.Item;
            entry.RelatedToMe = transfer.IsRelatedToActivePlayer;
            // Real locations are unique per sender. Negative locations (cheats/start inventory)
            // can legitimately repeat and must not be deduplicated by item name or item ID.
            if (transfer.Item.LocationId >= 0 && !(message is ItemCheatLogMessage))
                entry.TransferKey = $"{transfer.Sender.Team}:{transfer.Sender.Slot}:{transfer.Receiver.Slot}:" +
                                    $"{transfer.Item.LocationId}:{transfer.Item.ItemId}";
        }
        // Bound individual messages independently of the history's entry count.
        var remaining = 4096;
        foreach (var part in entry.Parts)
        {
            if (part.Text.Length > remaining) part.Text = part.Text.Substring(0, remaining);
            remaining -= part.Text.Length;
        }
        return entry;
    }
}
