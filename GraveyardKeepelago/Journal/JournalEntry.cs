using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Archipelago.MultiClient.Net.Enums;
using GraveyardKeepelago.GameModifications.UI;
using Newtonsoft.Json;

namespace GraveyardKeepelago.Journal;

public enum JournalKind { Item, Chat, System }
public enum JournalFilter { All, MyItems, Chat, System }
public enum JournalRole { Text, Player, Item }

public sealed class JournalIdentity
{
    public string Game { get; set; } = "Graveyard Keeper";
    public string Seed { get; set; }
    public int Team { get; set; }
    public int Slot { get; set; }

    [JsonIgnore]
    public string Key
    {
        get
        {
            using var hash = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new object[] { Game, Seed, Team, Slot }));
            return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
}

public sealed class JournalPart
{
    public string Text { get; set; } = string.Empty;
    public JournalRole Role { get; set; }
    public ItemFlags Flags { get; set; }
}

public sealed class JournalEntry
{
    public string ID { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public JournalKind Kind { get; set; }
    public bool RelatedToMe { get; set; }
    public bool Read { get; set; }
    public string TransferKey { get; set; }
    public List<JournalPart> Parts { get; set; } = new();

    public static JournalEntry System(string text) => new()
    {
        Kind = JournalKind.System,
        Parts = new List<JournalPart> { new() { Text = text } },
    };

    public bool Matches(JournalFilter filter, bool includeOtherItems) => filter switch
    {
        JournalFilter.MyItems => Kind == JournalKind.Item && RelatedToMe,
        JournalFilter.Chat => Kind == JournalKind.Chat,
        JournalFilter.System => Kind == JournalKind.System,
        _ => Kind != JournalKind.Item || RelatedToMe || includeOtherItems,
    };

    public string Format() => "[777777]" + Timestamp.ToLocalTime().ToString("HH:mm") + "[-]  " + FormatBody();

    public string FormatBody(bool lightBackground = true)
    {
        var parts = Parts.Select(part => part.Role switch
        {
            JournalRole.Item => ApItemPresentation.Name(Escape(part.Text), part.Flags, lightBackground),
            JournalRole.Player => "[c][" + (lightBackground ? "795522" : "FFE0A0") + "]" + Escape(part.Text) + "[-][/c]",
            _ => Escape(part.Text),
        });
        return string.Concat(parts);
    }

    // NGUI has rich-text commands, including links and alpha. Network text must be literal.
    public static string Escape(string value) => (value ?? string.Empty).Replace("[", "［").Replace("]", "］");
}
