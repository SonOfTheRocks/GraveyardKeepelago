using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KaitoKid.ArchipelagoUtilities.Net.Client;
using Newtonsoft.Json;

namespace GraveyardKeepelago.Progression;

// The namespaced record is serialized with the game's phrase list. The game only tests
// membership of known phrase IDs; it never interprets arbitrary entries as definitions.
public sealed class SaveProgress
{
    public const string RecordPrefix = "graveyard_keepelago:progress:v1:";
    public int Version { get; set; } = 1;
    public string Seed { get; set; }
    public int Team { get; set; }
    public int Slot { get; set; }
    public List<ProcessedReceipt> Receipts { get; set; } = new();
    public HashSet<string> Crafts { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> Works { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> Perks { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> Checks { get; set; } = new(StringComparer.Ordinal);
    public List<string> PendingTraps { get; set; } = new();

    // Only the native new-game creation path may call this. Loading an existing
    // save must never silently adopt the currently connected server identity.
    public static SaveProgress Create(IList<string> records, string seed, int team, int slot)
    {
        if (string.IsNullOrWhiteSpace(seed)) throw new InvalidDataException("The server supplied no seed identity.");
        if (records == null || records.Any(record => record == null))
            throw new InvalidDataException("The save's Archipelago record list is corrupt.");
        if (records.Any(record => record.StartsWith(RecordPrefix, StringComparison.Ordinal)))
            throw new InvalidDataException("This save already has an Archipelago binding and cannot be rebound.");
        return new SaveProgress { Seed = seed, Team = team, Slot = slot };
    }

    public static SaveProgress Load(IList<string> records, string seed, int team, int slot)
    {
        if (string.IsNullOrWhiteSpace(seed)) throw new InvalidDataException("The server supplied no seed identity.");
        if (records == null || records.Any(record => record == null))
            throw new InvalidDataException("The save's Archipelago record list is corrupt.");
        var saved = records.Where(record => record.StartsWith(RecordPrefix, StringComparison.Ordinal)).ToArray();
        if (saved.Length > 1) throw new InvalidDataException("Multiple AP progress records in this save.");
        if (saved.Length == 0)
            throw new InvalidDataException("This is not an Archipelago save. While connected to Archipelago, " +
                "load a save belonging to this seed and slot, or start a new game. Disconnect to play a non-AP save.");
        SaveProgress state;
        try
        {
            state = JsonConvert.DeserializeObject<SaveProgress>(Encoding.UTF8.GetString(
                Convert.FromBase64String(saved[0].Substring(RecordPrefix.Length))));
        }
        catch (Exception ex) when (ex is FormatException || ex is JsonException)
        {
            throw new InvalidDataException("The Archipelago progress record is corrupt. Load an intact backup of this save.", ex);
        }
        if (state == null || state.Version != 1)
            throw new InvalidDataException("This save has an unsupported Archipelago progress format.");
        state.ValidateIdentity(seed, team, slot);
        if (state.Receipts == null || state.Crafts == null || state.Works == null || state.Perks == null || state.Checks == null ||
            state.PendingTraps == null || state.PendingTraps.Any(string.IsNullOrWhiteSpace) ||
            state.Receipts.Any(receipt => receipt == null) ||
            state.Receipts.Select(receipt => receipt.Index).Distinct().Count() != state.Receipts.Count ||
            state.Receipts.Where((receipt, index) => receipt.Index != index).Any())
            throw new InvalidDataException("The AP progress record is incomplete or corrupt.");
        return state;
    }

    public void ValidateIdentity(string seed, int team, int slot)
    {
        if (Seed != seed || Team != team || Slot != slot)
            throw new InvalidDataException($"This Archipelago save belongs to seed '{Seed}', team {Team}, slot {Slot}. " +
                $"The connected server uses seed '{seed}', team {team}, slot {slot}. Load the matching save or connect to its server and slot.");
    }

    public string[] RememberChecks(IEnumerable<string> names, IReadOnlyDictionary<string, long> activeLocations)
        => names.Where(activeLocations.ContainsKey).Distinct().Where(Checks.Add).ToArray();

    public void Store(IList<string> records)
    {
        var record = RecordPrefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this)));
        for (var index = records.Count - 1; index >= 0; index--)
            if (records[index].StartsWith(RecordPrefix, StringComparison.Ordinal)) records.RemoveAt(index);
        records.Add(record);
    }

    public void ValidateHistory(IReadOnlyList<ReceivedItem> history)
    {
        // A partial initial packet is allowed; a conflicting prefix is never replayed.
        for (var index = 0; index < Math.Min(history.Count, Receipts.Count); index++)
            if (!Receipts[index].Matches(history[index]))
                throw new InvalidDataException($"AP item history differs from this save at index {index}. Use the matching server progress.");
    }
}

public sealed class ProcessedReceipt
{
    public int Index { get; set; }
    public long ItemID { get; set; }
    public long LocationID { get; set; }
    public long PlayerID { get; set; }
    public string Name { get; set; }
    public bool Matches(ReceivedItem item) => Index == item.UniqueId && ItemID == item.ItemId &&
        LocationID == item.LocationId && PlayerID == item.PlayerId && Name == item.ItemName;
    public static ProcessedReceipt From(ReceivedItem item) => new()
    {
        Index = item.UniqueId, ItemID = item.ItemId, LocationID = item.LocationId,
        PlayerID = item.PlayerId, Name = item.ItemName,
    };
}
