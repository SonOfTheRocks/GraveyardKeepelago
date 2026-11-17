using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net.Models;

namespace GraveyardKeepelago.Archipelago;

// One history per socket. Only a zero-index packet can establish or resynchronize it.
internal sealed class ReceivedItemHistory
{
    private readonly object _gate = new();
    private readonly List<NetworkItem> _items = new();
    private bool _synchronized;
    private int _syncGeneration;
    private NetworkItem[] _snapshot;

    public void Receive(int index, IReadOnlyList<NetworkItem> items)
    {
        lock (_gate)
        {
            if (index == 0)
            {
                _items.Clear();
                _items.AddRange(items);
                _synchronized = true;
                _snapshot = null;
                return;
            }

            if (!_synchronized)
                return;

            if (index < 0 || index > _items.Count)
            {
                LoseSynchronization();
                return;
            }

            var overlap = Math.Min(items.Count, _items.Count - index);
            for (var offset = 0; offset < overlap; offset++)
            {
                var previous = _items[index + offset];
                var current = items[offset];
                if (previous.Item != current.Item || previous.Location != current.Location ||
                    previous.Player != current.Player)
                {
                    LoseSynchronization();
                    return;
                }
            }

            for (var offset = overlap; offset < items.Count; offset++)
                _items.Add(items[offset]);
            if (overlap < items.Count)
                _snapshot = null;
        }
    }

    public IReadOnlyList<NetworkItem> Snapshot(out int syncGeneration)
    {
        lock (_gate)
        {
            syncGeneration = _syncGeneration;
            return _synchronized ? _snapshot ??= _items.ToArray() : null;
        }
    }

    private void LoseSynchronization()
    {
        _syncGeneration++;
        _synchronized = false;
        _snapshot = null;
    }
}
