using System;
using System.Collections.Generic;

namespace GraveyardKeepelago.Progression;

public static class SaveSlotEligibility
{
    // Null means selectable. Only the new-game row skips file inspection.
    // Validation is read-only and shares the actual load guard.
    public static string Reason(SaveStartupGuard guard, bool newGame, Func<IList<string>> readRecords)
    {
        if (newGame) return null;
        try { SaveStartupGuard.ContinueLoadedSave(guard, readRecords(), () => { }); return null; }
        catch (Exception ex) { return "This save cannot be selected.\n\n" + ex.Message; }
    }
}
