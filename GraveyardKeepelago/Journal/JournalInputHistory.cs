using System;
using System.Collections.Generic;

namespace GraveyardKeepelago.Journal;

public sealed class JournalInputHistory
{
    public const int Capacity = 100;
    private readonly List<string> _sent = new();
    private int _index;
    private bool _browsing;
    private string _draft = string.Empty, _lastRecall;
    public int Count => _sent.Count;

    public void Record(string text)
    {
        text = text?.Trim();
        if (string.IsNullOrEmpty(text)) return;
        if (_sent.Count == 0 || _sent[_sent.Count - 1] != text) _sent.Add(text);
        if (_sent.Count > Capacity) _sent.RemoveAt(0);
        Reset();
    }

    public void Reset() { _browsing = false; _index = _sent.Count; _lastRecall = null; }

    public string Move(int direction, string current)
    {
        current ??= string.Empty;
        if (_sent.Count == 0 || direction == 0) return current;
        // Editing a recalled line starts a new draft; Down can recover that edit as well.
        if (_browsing && current != _lastRecall) Reset();
        if (!_browsing)
        {
            if (direction > 0) return current;
            _draft = current;
            _index = _sent.Count;
            _browsing = true;
        }
        _index = Math.Max(0, Math.Min(_sent.Count, _index + (direction < 0 ? -1 : 1)));
        if (_index == _sent.Count) { _browsing = false; return _draft; }
        return _lastRecall = _sent[_index];
    }
}
