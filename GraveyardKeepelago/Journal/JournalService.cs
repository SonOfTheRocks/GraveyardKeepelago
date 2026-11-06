using System;
using System.Collections.Generic;
using System.Linq;

namespace GraveyardKeepelago.Journal;

public sealed class JournalService
{
    private readonly object _gate = new();
    private readonly Queue<PendingEntry> _pending = new();
    private readonly IJournalStore _store;
    private readonly Action<string> _warning;
    private bool _accepting;
    private bool _storageFailed;
    private int _savedRevision;
    public JournalIdentity Identity { get; private set; }
    public JournalHistory History { get; } = new();
    private sealed class PendingEntry
    {
        public JournalEntry Entry;
        public JournalIdentity Source;
    }

    public JournalService(IJournalStore store, Action<string> warning)
    {
        _store = store;
        _warning = warning;
    }

    // Called by the explicit menu connection, before callbacks can deliver an initial batch.
    public void BeginConnection()
    {
        Flush();
        lock (_gate) { _accepting = true; _pending.Clear(); }
        Identity = null;
        History.Load(Array.Empty<JournalEntry>());
        _storageFailed = false;
    }

    public void Bind(JournalIdentity identity)
    {
        Identity = identity;
        try { History.Load(_store.Load(identity)); }
        catch (Exception ex) { StorageError("read", ex); History.Load(Array.Empty<JournalEntry>()); }
        _savedRevision = History.Revision;
        Enqueue(JournalEntry.System("Connected to Archipelago."));
        Pump();
    }

    public void CancelConnection()
    {
        lock (_gate) { _accepting = false; _pending.Clear(); }
        Identity = null;
        History.Load(Array.Empty<JournalEntry>());
    }

    public void Enqueue(JournalEntry entry, JournalIdentity source = null)
    {
        if (entry == null) return;
        lock (_gate)
        {
            if (!_accepting) return;
            if (_pending.Count == JournalHistory.Capacity) _pending.Dequeue();
            _pending.Enqueue(new PendingEntry { Entry = entry, Source = source });
        }
    }

    public void Pump()
    {
        if (Identity == null) return;
        lock (_gate)
            while (_pending.Count > 0)
            {
                var pending = _pending.Dequeue();
                if (pending.Source == null || pending.Source.Key == Identity.Key) History.Add(pending.Entry);
            }
    }

    public void Flush()
    {
        Pump();
        if (Identity == null || _storageFailed || History.Revision == _savedRevision) return;
        try { _store.Save(Identity, History.Entries); _savedRevision = History.Revision; }
        catch (Exception ex) { StorageError("save", ex); }
    }

    private void StorageError(string operation, Exception ex)
    {
        _storageFailed = true; // Once per connection, never interrupt play or log every frame.
        _warning?.Invoke($"Could not {operation} the AP journal ({ex.GetType().Name}). In-memory history remains available.");
    }
}

public static class JournalChat
{
    public static bool TrySend(string value, bool connected, Action<string> send, out string error)
    {
        error = string.Empty;
        if (!connected) { error = "Connect to Archipelago before sending a message."; return false; }
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text)) { error = "Enter a message or server command."; return false; }
        if (text.Length > 500 || text.Any(char.IsControl))
        { error = "Use a single line of at most 500 characters."; return false; }
        try { send(text); return true; }
        catch (Exception) { error = "The message could not be sent. Check the connection and retry."; return false; }
    }
}
