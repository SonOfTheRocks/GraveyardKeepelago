using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace GraveyardKeepelago.Journal;

public interface IJournalStore
{
    IReadOnlyList<JournalEntry> Load(JournalIdentity identity);
    void Save(JournalIdentity identity, IReadOnlyList<JournalEntry> entries);
}

public sealed class JournalStore : IJournalStore
{
    private readonly string _directory;
    public JournalStore(string directory) => _directory = directory;
    public string PathFor(JournalIdentity identity) => Path.Combine(_directory, identity.Key + ".json");

    public IReadOnlyList<JournalEntry> Load(JournalIdentity identity)
    {
        var path = PathFor(identity);
        if (!File.Exists(path)) return Array.Empty<JournalEntry>();
        try { return Read(path, identity); }
        catch (Exception ex) when (ex is JsonException || ex is InvalidDataException)
        {
            // Keep the damaged primary file; a previous atomic save may still be usable.
            if (File.Exists(path + ".bak")) return Read(path + ".bak", identity);
            throw;
        }
    }

    private static IReadOnlyList<JournalEntry> Read(string path, JournalIdentity identity)
    {
        if (new FileInfo(path).Length > 96 * 1024 * 1024)
            throw new InvalidDataException("Journal exceeds the supported file size.");
        var document = JsonConvert.DeserializeObject<Document>(File.ReadAllText(path));
        if (document?.Version != 1 || document.Identity?.Key != identity.Key || document.Entries == null)
            throw new InvalidDataException("Unsupported journal format or party identity.");
        var entries = document.Entries.Skip(Math.Max(0, document.Entries.Count - JournalHistory.Capacity)).ToArray();
        if (entries.Any(entry => entry == null || string.IsNullOrEmpty(entry.ID) ||
                !Enum.IsDefined(typeof(JournalKind), entry.Kind) || entry.Parts == null ||
                entry.Parts.Count > 256 || entry.Parts.Any(part => part?.Text == null ||
                    !Enum.IsDefined(typeof(JournalRole), part.Role)) ||
                entry.Parts.Sum(part => (long)part.Text.Length) > 16384) ||
            entries.Select(entry => entry.ID).Distinct().Count() != entries.Length)
            throw new InvalidDataException("Journal contains invalid entries.");
        return entries;
    }

    public void Save(JournalIdentity identity, IReadOnlyList<JournalEntry> entries)
    {
        Directory.CreateDirectory(_directory);
        var path = PathFor(identity);
        var temporary = path + ".tmp";
        var data = JsonConvert.SerializeObject(new Document { Version = 1, Identity = identity,
            Entries = entries.Skip(Math.Max(0, entries.Count - JournalHistory.Capacity)).ToList() });
        File.WriteAllText(temporary, data);
        if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
        else File.Move(temporary, path);
    }

    private sealed class Document
    {
        public int Version { get; set; }
        public JournalIdentity Identity { get; set; }
        public List<JournalEntry> Entries { get; set; }
    }
}
