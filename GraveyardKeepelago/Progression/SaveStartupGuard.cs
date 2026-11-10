using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GraveyardKeepelago.Progression;

// The continuation includes the native scene transition or first save. Never
// run it until identity validation/binding has succeeded. No network writes.
public sealed class SaveStartupGuard
{
    private readonly string _seed;
    private readonly int _team;
    private readonly int _slot;
    public SaveStartupGuard(string seed, int team, int slot) { _seed = seed; _team = team; _slot = slot; }

    public static void ContinueLoadedSave(SaveStartupGuard connectedGuard, IList<string> records, Action continueLoading)
    {
        if (connectedGuard != null)
        {
            connectedGuard.ContinueLoadedSave(records, continueLoading);
            return;
        }
        if (records == null || records.Any(record => record == null))
            throw new InvalidDataException("The selected save's record list is corrupt.");
        // Recognize the namespace even if a newer or damaged record cannot be
        // decoded. Such a save must never fall through to vanilla gameplay.
        if (records.Any(record => record.StartsWith("graveyard_keepelago:progress:", StringComparison.Ordinal)))
            throw new InvalidDataException("This is an Archipelago save. Connect to its Archipelago server and slot before loading it.");
        continueLoading();
    }

    public void ContinueLoadedSave(IList<string> records, Action continueLoading)
    {
        SaveProgress.Load(records, _seed, _team, _slot);
        continueLoading();
    }

    public void ContinueNewSave(IList<string> records, Action firstSave)
    {
        var progress = SaveProgress.Create(records, _seed, _team, _slot);
        progress.Store(records);
        firstSave();
    }
}
