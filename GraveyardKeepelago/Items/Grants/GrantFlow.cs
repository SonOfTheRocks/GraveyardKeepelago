using System;
using System.IO;

namespace GraveyardKeepelago.Items.Grants;

// Owns one native script while its grant waits for the end-of-script callback.
internal sealed class GrantFlow : IDisposable
{
    private CustomFlowScript _script;
    private bool _started;
    private bool _finished;

    public bool Started => _started;

    public bool TryComplete(string scriptID)
    {
        if (!_started)
        {
            _script = GS.RunFlowScript(scriptID, _ => _finished = true);
            _started = true;
            if (_script == null)
                throw new InvalidDataException($"Could not start AP flow script '{scriptID}'.");
        }

        if (!_finished && _script == null)
            throw new InvalidDataException($"AP flow script '{scriptID}' stopped before completion.");

        return _finished;
    }

    public void Dispose()
    {
        if (!_finished && _script != null)
            _script.TerminateMe();
        _script = null;
    }
}
