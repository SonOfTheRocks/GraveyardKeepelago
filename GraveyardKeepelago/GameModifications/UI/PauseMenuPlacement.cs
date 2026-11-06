using System;
using System.Collections.Generic;

namespace GraveyardKeepelago.GameModifications.UI;

public static class PauseMenuPlacement
{
    public static int ConnectionIndex(IReadOnlyList<string> actions)
    {
        var continueIndex = -1;
        for (var i = 0; i < actions.Count; i++)
        {
            if (actions[i] == "OnPressedContinue") continueIndex = i;
            if (actions[i] == "OnPressedOptions" && continueIndex >= 0) return i;
        }
        throw new InvalidOperationException("Pause menu must contain Continue followed by Options.");
    }
}
