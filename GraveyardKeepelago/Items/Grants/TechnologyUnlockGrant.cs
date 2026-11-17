using System.Collections.Generic;
using System.IO;

namespace GraveyardKeepelago.Items.Grants;

internal sealed class TechnologyUnlockGrant : IItemGrant
{
    public IReadOnlyList<string> TechnologyIDs { get; }

    public TechnologyUnlockGrant(IReadOnlyList<string> technologyIDs) => TechnologyIDs = technologyIDs;

    public GrantResult Apply(GrantContext context)
    {
        _ = context.Player;
        foreach (var id in TechnologyIDs)
        {
            context.Save.UnlockTech(id);
            if (!context.Save.unlocked_techs.Contains(id))
                throw new InvalidDataException($"Could not unlock AP technology '{id}'.");
        }
        return GrantResult.Applied;
    }
}
