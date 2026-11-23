using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GraveyardKeepelago.Items;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraveyardKeepelago.Progression;

// The game serializes this record with its native unlocked phrase list.
internal sealed class SaveProgress
{
    internal const string RecordPrefix = "graveyard_keepelago:progress:v1:";
    private const string RecordNamespace = "graveyard_keepelago:progress:";

    private readonly IList<string> _records;

    public (string Seed, int Team, int Slot) Identity => (Data.Seed, Data.Team, Data.Slot);
    public ProgressRecord Data { get; }

    private SaveProgress(IList<string> records, ProgressRecord data)
    {
        _records = records;
        Data = data;
    }

    public static SaveProgress Bind(IList<string> records, (string Seed, int Team, int Slot) identity, bool newSave)
    {
        if (string.IsNullOrWhiteSpace(identity.Seed) || identity.Team < 0 || identity.Slot <= 0)
            throw new InvalidDataException("The server supplied an invalid Archipelago save identity.");

        var saved = records.Where(IsProgressRecord).ToArray();
        if (saved.Length == 0 && newSave)
        {
            var created = new SaveProgress(records, new ProgressRecord
            {
                Version = 1, Seed = identity.Seed, Team = identity.Team, Slot = identity.Slot, Checks = new(),
            });
            created.Store();
            return created;
        }

        if (saved.Length != 1 || !saved[0].StartsWith(RecordPrefix, StringComparison.Ordinal))
            throw new InvalidDataException("The save must contain one supported Archipelago progress record.");

        ProgressRecord loaded;
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(saved[0].Substring(RecordPrefix.Length)));
            loaded = JsonConvert.DeserializeObject<ProgressRecord>(json);
        }
        catch (Exception ex) when (ex is FormatException || ex is JsonException)
        {
            throw new InvalidDataException("The Archipelago progress record is corrupt.", ex);
        }

        if (loaded == null || loaded.Version != 1 || loaded.Checks == null || loaded.Checks.Any(string.IsNullOrWhiteSpace) ||
            loaded.Receipts == null || loaded.Receipts.Where((receipt, index) =>
                receipt == null || receipt.Index != index || string.IsNullOrWhiteSpace(receipt.Name)).Any() ||
            loaded.PendingTraps == null || loaded.PendingTraps.Any(string.IsNullOrWhiteSpace) ||
            (loaded.InterruptedReceipt.HasValue && loaded.InterruptedReceipt.Value != loaded.Receipts.Count))
            throw new InvalidDataException("The Archipelago progress record is incomplete or unsupported.");

        var progress = new SaveProgress(records, loaded);
        if (progress.Identity != identity)
            throw new InvalidDataException("The save belongs to a different Archipelago seed, team, or slot.");

        return progress;
    }

    public void Store()
    {
        var json = JsonConvert.SerializeObject(Data);
        var record = RecordPrefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        for (var index = 0; index < _records.Count; index++)
        {
            if (!IsProgressRecord(_records[index]))
                continue;

            _records[index] = record;
            return;
        }

        _records.Add(record);
    }

    private static bool IsProgressRecord(string record)
        => record?.StartsWith(RecordNamespace, StringComparison.Ordinal) == true;

    internal sealed class ProgressRecord
    {
        public int Version { get; set; }
        public string Seed { get; set; }
        [JsonProperty(Required = Required.Always)]
        public int Team { get; set; }
        [JsonProperty(Required = Required.Always)]
        public int Slot { get; set; }
        public HashSet<string> Checks { get; set; }
        public List<ItemReceipt> Receipts { get; set; } = new();
        public List<string> PendingTraps { get; set; } = new();
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? InterruptedReceipt { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> OtherProgress { get; set; }
    }
}
