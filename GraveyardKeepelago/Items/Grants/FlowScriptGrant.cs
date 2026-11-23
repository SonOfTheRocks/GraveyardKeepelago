using System;
using System.IO;

namespace GraveyardKeepelago.Items.Grants;

internal sealed class FlowScriptGrant : IItemGrant, IDisposable
{
    private readonly GrantFlow _flow = new();
    public string ScriptID { get; }

    public FlowScriptGrant(string scriptID) => ScriptID = scriptID;

    public GrantResult Apply(GrantContext context)
    {
        _ = context.Player;
        // The v9 blueprint's cutscene places this tagged world object.
        if (ScriptID != "elevator_and_supplies_place")
            throw new InvalidDataException($"No completion check for AP flow script '{ScriptID}'.");

        if (!_flow.Started && ElevatorExists())
            return GrantResult.Applied;
        if (!_flow.TryComplete(ScriptID))
            return GrantResult.Pending;
        if (!ElevatorExists())
            throw new InvalidDataException($"AP flow script '{ScriptID}' ended without placing the elevator.");

        return GrantResult.Applied;
    }

    public void Dispose() => _flow.Dispose();

    private static bool ElevatorExists() =>
        WorldMap.GetWorldGameObjectByCustomTag("elevator_top", ignore_not_found_error: true) != null;
}
