using System;
using System.Collections.Generic;
using System.IO;
using GraveyardKeepelago.GameModifications;

namespace GraveyardKeepelago.Items.Grants;

internal sealed class UnlockGrant : IItemGrant
{
    public IReadOnlyList<(PlayerUtilities.UnlockType Type, string ID)> Unlocks { get; }

    public UnlockGrant(IReadOnlyList<(PlayerUtilities.UnlockType Type, string ID)> unlocks) => Unlocks = unlocks;

    public GrantResult Apply(GrantContext context)
    {
        var save = context.Save;
        _ = context.Player;
        foreach (var (type, rawID) in Unlocks)
        {
            var id = Technologies.TechnologyGameChecks.Canonical(rawID);
            if (string.IsNullOrEmpty(id))
                throw new InvalidDataException($"Missing ID for AP {type} unlock.");

            switch (type)
            {
                case PlayerUtilities.UnlockType.Craft:
                    save.UnlockCraft(id);
                    break;
                case PlayerUtilities.UnlockType.Work:
                    if (!save.unlocked_works.Contains(id))
                        save.unlocked_works.Add(id);
                    break;
                case PlayerUtilities.UnlockType.Perk:
                    save.UnlockPerk(id);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Unlocks), type, "Unsupported AP unlock type.");
            }
        }
        return GrantResult.Applied;
    }
}
