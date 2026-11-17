using System;

namespace GraveyardKeepelago.Items.Grants;

internal sealed class RelationGrant : IItemGrant
{
    public string NpcID { get; }
    public int Amount { get; }

    public RelationGrant(string npcID, int amount) => (NpcID, Amount) = (npcID, amount);

    public GrantResult Apply(GrantContext context)
    {
        var player = context.Player;
        context.Save.known_npcs.GetOrCreateNPC(NpcID);
        player.data.AddToParams("_rel_" + NpcID, (float)Amount);
        try
        {
            WorldMap.GetWorldGameObjectByObjId(NpcID, ignore_not_found_error: true)?.ShowRelationChangeBubble(Amount);
        }
        catch (Exception ex)
        {
            context.Logger.LogWarning($"Could not show relation change for '{NpcID}': {ex}");
        }
        return GrantResult.Applied;
    }
}
