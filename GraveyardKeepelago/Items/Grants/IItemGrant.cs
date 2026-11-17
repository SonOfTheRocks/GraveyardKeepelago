namespace GraveyardKeepelago.Items.Grants;

internal enum GrantResult
{
    Retry,
    Pending,
    Applied,
}

internal interface IItemGrant
{
    GrantResult Apply(GrantContext context);
}
