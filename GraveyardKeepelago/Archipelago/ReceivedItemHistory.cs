using System.Collections.Generic;
using Archipelago.MultiClient.Net.Models;

namespace GraveyardKeepelago.Archipelago;

// The socket callback is the sole writer. Unity reads a complete packet's snapshot.
internal sealed class ReceivedItemHistory
{
    private readonly List<NetworkItem> _items = new();
    private volatile NetworkItem[] _snapshot;

    public IReadOnlyList<NetworkItem> Snapshot => _snapshot;

    public void Receive(int index, IReadOnlyList<NetworkItem> items)
    {
        if (index == 0)
        {
            _items.Clear();
        }
        else if (_snapshot == null || index != _items.Count)
        {
            // MultiClient requests a full replay on a nonzero index mismatch.
            _snapshot = null;
            return;
        }

        _items.AddRange(items);
        _snapshot = _items.ToArray();
    }

    public void RefreshItemNames()
    {
        if (_snapshot != null)
            _snapshot = _items.ToArray();
    }
}
