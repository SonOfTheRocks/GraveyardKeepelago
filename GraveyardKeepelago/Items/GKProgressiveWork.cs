using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.GameModifications;

namespace GraveyardKeepelago.Items
{
    public class GKProgressiveWork : GKProgressiveItem
    {
        public GKProgressiveWork(List<List<string>> ids, GameModifications.IPlayerActions playerActions = null) : base(ids, playerActions)
        {
        }

        public override void Apply()
        {
            var index = this.applyCount;
            var ids = this.progressiveIDs[index];
            
            
            var unlocks = ids
                .Select(id => ((PlayerUtilities.UnlockType, string))(PlayerUtilities.UnlockType.Work, id))
                .ToList();
            PlayerActions.ApplyUnlocks(unlocks);

            this.applyCount++;
        }
    }
}