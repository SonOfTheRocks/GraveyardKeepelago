using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.GameModifications;

namespace GraveyardKeepelago.Items
{
    public class GKProgressiveRecipe : GKProgressiveItem
    {
        public GKProgressiveRecipe(List<List<string>> ids) : base(ids)
        {
        }

        public override void Apply()
        {
            var index = this.applyCount;
            var ids = this.progressiveIDs[index];
            
            
            var unlocks = ids
                .Select(id => (PlayerUtilities.UnlockType.Craft, id))
                .ToList();
            PlayerUtilities.ApplyUnlocks(unlocks);

            this.applyCount++;
        }
    }
}