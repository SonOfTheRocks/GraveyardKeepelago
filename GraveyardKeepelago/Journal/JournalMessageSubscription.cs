using System;
using System.Threading;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;

namespace GraveyardKeepelago.Journal;

// One subscription per actual network session. An old delegate can never adopt a new party.
public sealed class JournalMessageSubscription : IDisposable
{
    private readonly IMessageLogHelper _messages;
    private readonly JournalService _journal;
    private readonly JournalIdentity _identity;
    private readonly Action<string> _warning;
    private int _disposed;

    public JournalMessageSubscription(IMessageLogHelper messages, JournalService journal,
        JournalIdentity identity, Action<string> warning)
    {
        _messages = messages;
        _journal = journal;
        _identity = identity;
        _warning = warning;
        _messages.OnMessageReceived += Receive;
    }

    private void Receive(LogMessage message)
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        try { _journal?.Enqueue(JournalMessageAdapter.Convert(message), _identity); }
        catch (Exception ex) { _warning?.Invoke($"Could not capture AP journal message: {ex.GetType().Name}"); }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0) _messages.OnMessageReceived -= Receive;
    }
}
