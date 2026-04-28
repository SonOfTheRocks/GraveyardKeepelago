using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.GameModifications;

namespace GraveyardKeepelago.Items
{
    public class GKProgressiveWork : GKProgressiveItem
    {
        public GKProgressiveWork(List<List<string>> ids) : base(ids)
        {
        }

        public override void Apply()
        {
            var index = this.applyCount;
            var ids = this.progressiveIDs[index];
            
            
            var unlocks = ids
                .Select(id => (PlayerUtilities.UnlockType.Work, id))
                .ToList();
            PlayerUtilities.ApplyUnlocks(unlocks);

            this.applyCount++;
        }
    }
}