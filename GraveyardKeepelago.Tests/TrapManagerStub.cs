using System;
using System.Collections.Generic;
using GraveyardKeepelago.Items.Traps;

namespace GraveyardKeepelago.Tests;

public class TrapManagerStub : ITrapManager
{
    private readonly Dictionary<string, bool> _traps = new();
    private readonly List<string> _executedTraps = new();
    private readonly Func<string, bool> _isTrapFunc;
    private readonly Action<string> _executeTrapAction;

    public TrapManagerStub(
        Func<string, bool> isTrapFunc = null,
        Action<string> executeTrapAction = null)
    {
        _isTrapFunc = isTrapFunc ?? (x => false);
        _executeTrapAction = executeTrapAction ?? (x => { });
    }

    public bool IsTrap(string itemName)
    {
        if (_traps.ContainsKey(itemName))
            return _traps[itemName];
        return _isTrapFunc(itemName);
    }

    public void SetTrap(string name, bool isTrap)
    {
        _traps[name] = isTrap;
    }

    public bool WasTrapExecuted(string name) => _executedTraps.Contains(name);

    public bool TryExecuteTrapImmediately(string trapName)
    {
        _executedTraps.Add(trapName);
        _executeTrapAction(trapName);
        return true;
    }
}
