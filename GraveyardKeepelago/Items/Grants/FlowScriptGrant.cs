using System.IO;

namespace GraveyardKeepelago.Items.Grants;

internal sealed class FlowScriptGrant : IItemGrant
{
    public string ScriptID { get; }

    public FlowScriptGrant(string scriptID) => ScriptID = scriptID;

    public GrantResult Apply(GrantContext context)
    {
        _ = context.Player;
        if (GS.RunFlowScript(ScriptID) == null)
            throw new InvalidDataException($"Could not start AP flow script '{ScriptID}'.");
        return GrantResult.Applied;
    }
}
