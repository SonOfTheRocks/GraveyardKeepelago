using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.GameModifications;
using JetBrains.Annotations;

namespace GraveyardKeepelago.Items
{
    public class GKWork: GKItem
    {
        private readonly List<string> _perks;

        public GKWork([CanBeNull] string id = null, [CanBeNull] string perk = null, IPlayerActions playerActions = null) : base(id, playerActions)
        {
            _perks = perk is not null ? [perk] : [];
        }
        
        public GKWork([CanBeNull] List<string> ids = null, [CanBeNull] List<string> perks = null, IPlayerActions playerActions = null) : base(ids, playerActions)
        {
            _perks = perks is not null ? [..perks] : [];
        }

        public override void Apply()
        {
            var unlocks = IDs
                .Select(id => (PlayerUtilities.UnlockType.Work, id))
                .Concat(_perks.Select(perk => (PlayerUtilities.UnlockType.Perk, perk)))
                .ToList();
            PlayerActions.ApplyUnlocks(unlocks);
        }
    }
}