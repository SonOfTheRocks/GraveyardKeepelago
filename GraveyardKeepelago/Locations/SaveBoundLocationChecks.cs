using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraveyardKeepelago.Locations;

// GameSave serializes unlocked_phrases and only checks it for known phrase IDs.
internal sealed class SaveBoundLocationChecks : ILocationChecker
{
    internal const string RecordNamespace = "graveyard_keepelago:progress:";
    internal const string RecordPrefix = RecordNamespace + "v1:";

    private readonly IList<string> _records;
    private readonly IReadOnlyDictionary<string, long> _activeLocations;
    private readonly ProgressRecord _progress;
    private Action<long[]> _send;

    private SaveBoundLocationChecks(IList<string> records, IReadOnlyDictionary<string, long> activeLocations,
        ProgressRecord progress)
    {
        _records = records;
        _activeLocations = activeLocations;
        _progress = progress;
    }

    public static SaveBoundLocationChecks Bind(IList<string> records, (string Seed, int Team, int Slot) identity,
        IReadOnlyDictionary<string, long> activeLocations, bool newSave)
    {
        if (string.IsNullOrWhiteSpace(identity.Seed))
            throw new InvalidDataException("The server supplied no seed identity.");

        var saved = records.Where(IsProgressRecord).ToArray();
        if (saved.Length == 0 && newSave)
        {
            var progress = new ProgressRecord
            {
                Version = 1, Seed = identity.Seed, Team = identity.Team, Slot = identity.Slot, Checks = new(),
            };
            var checks = new SaveBoundLocationChecks(records, activeLocations, progress);
            checks.Store(progress);
            return checks;
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

        if (loaded == null || loaded.Version != 1 || loaded.Checks == null || loaded.Checks.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("The Archipelago progress record is incomplete or unsupported.");
        if ((loaded.Seed, loaded.Team, loaded.Slot) != identity)
            throw new InvalidDataException("The save belongs to a different Archipelago seed, team, or slot.");

        return new SaveBoundLocationChecks(records, activeLocations, loaded);
    }

    public bool Connect((string Seed, int Team, int Slot) identity, Action<long[]> send)
    {
        Disconnect();
        if ((_progress.Seed, _progress.Team, _progress.Slot) != identity)
            return false;

        _send = send;
        Send(_progress.Checks.Where(_activeLocations.ContainsKey));
        return true;
    }

    public void Disconnect() => _send = null;

    public void AddCheckedLocation(string locationName) => AddCheckedLocations(new[] { locationName });

    public void AddCheckedLocations(string[] locationNames)
    {
        var added = locationNames.Where(name => name != null && _activeLocations.ContainsKey(name) &&
            !_progress.Checks.Contains(name)).Distinct(StringComparer.Ordinal).ToArray();
        if (added.Length == 0)
            return;

        _progress.Checks.UnionWith(added);
        try
        {
            Store(_progress);
        }
        catch
        {
            _progress.Checks.ExceptWith(added);
            throw;
        }
        Send(added);
    }

    private void Send(IEnumerable<string> names)
    {
        if (_send == null)
            return;

        var ids = names.Select(name => _activeLocations[name]).OrderBy(id => id).ToArray();
        if (ids.Length > 0)
            _send(ids);
    }

    private void Store(ProgressRecord progress)
    {
        var json = JsonConvert.SerializeObject(progress);
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

    private sealed class ProgressRecord
    {
        public int Version { get; set; }
        public string Seed { get; set; }
        [JsonProperty(Required = Required.Always)]
        public int Team { get; set; }
        [JsonProperty(Required = Required.Always)]
        public int Slot { get; set; }
        public HashSet<string> Checks { get; set; }

        // Preserve receipt and ownership data written by the full client.
        [JsonExtensionData]
        public IDictionary<string, JToken> OtherProgress { get; set; }
    }
}
