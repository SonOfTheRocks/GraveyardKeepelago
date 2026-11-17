namespace GraveyardKeepelago.Items.Grants;

internal sealed class TrapGrant : IItemGrant
{
    public string TrapID { get; }

    public TrapGrant(string trapID) => TrapID = trapID;

    public GrantResult Apply(GrantContext context)
    {
        context.QueueTrap(TrapID);
        return GrantResult.Applied;
    }
}
