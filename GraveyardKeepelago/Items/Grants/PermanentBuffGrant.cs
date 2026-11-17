namespace GraveyardKeepelago.Items.Grants;

internal sealed class PermanentBuffGrant : IItemGrant
{
    public string BuffID { get; }

    public PermanentBuffGrant(string buffID) => BuffID = buffID;

    public GrantResult Apply(GrantContext context)
    {
        _ = context.Player;
        context.LegacyActions.ApplyPermanentBuff(BuffID);
        return GrantResult.Applied;
    }
}
