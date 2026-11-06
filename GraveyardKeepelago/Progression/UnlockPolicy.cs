using System;
using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.GameModifications;
using GraveyardKeepelago.Logic;

namespace GraveyardKeepelago.Progression;

public sealed class UnlockPolicy
{
    private readonly SaveProgress _progress;
    public HashSet<string> ControlledCrafts { get; } = new(StringComparer.Ordinal);
    public HashSet<string> ControlledWorks { get; } = new(StringComparer.Ordinal);
    public HashSet<string> ControlledPerks { get; } = new(StringComparer.Ordinal);

    public UnlockPolicy(IEnumerable<ClientContractItem> items, SaveProgress progress)
    {
        _progress = progress;
        foreach (var item in items)
        {
            var grant = item.Grant;
            Add(ControlledCrafts, grant.CraftIDs);
            Add(ControlledWorks, grant.WorkIDs);
            Add(ControlledPerks, grant.PerkIDs);
            if (grant.PerkID != null) ControlledPerks.Add(Canonical(grant.PerkID));
            if (grant.Stages == null) continue;
            foreach (var stage in grant.Stages)
                Add(grant.Type == "progressive_craft_unlock" ? ControlledCrafts : ControlledWorks, stage);
        }
    }

    public static string Canonical(string id) => id?.TrimStart('@');
    private static void Add(HashSet<string> target, IEnumerable<string> ids)
    {
        if (ids != null) foreach (var id in ids) target.Add(Canonical(id));
    }

    public bool CraftLocked(string id) => ControlledCrafts.Contains(Canonical(id)) && !_progress.Crafts.Contains(Canonical(id));
    public bool WorkLocked(string id) => ControlledWorks.Contains(Canonical(id)) && !_progress.Works.Contains(Canonical(id));
    public bool PerkLocked(string id) => ControlledPerks.Contains(Canonical(id)) && !_progress.Perks.Contains(Canonical(id));

    public void Apply(IEnumerable<(PlayerUtilities.UnlockType Type, string ID)> unlocks, IUnlockTarget target)
    {
        foreach (var (type, rawID) in unlocks)
        {
            var id = Canonical(rawID);
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("An unlock ID must not be empty.");
            switch (type)
            {
                case PlayerUtilities.UnlockType.Craft:
                    target.UnlockCraft(id); _progress.Crafts.Add(id); break;
                case PlayerUtilities.UnlockType.Work:
                    target.UnlockWork(id); _progress.Works.Add(id); break;
                case PlayerUtilities.UnlockType.Perk:
                    target.UnlockPerk(id); _progress.Perks.Add(id); break;
                case PlayerUtilities.UnlockType.Phrase:
                    target.UnlockPhrase(id); break;
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}

public interface IUnlockTarget
{
    void UnlockCraft(string id);
    void UnlockWork(string id);
    void UnlockPerk(string id);
    void UnlockPhrase(string id);
}
