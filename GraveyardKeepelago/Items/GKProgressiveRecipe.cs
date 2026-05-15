using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.GameModifications;

namespace GraveyardKeepelago.Items
{
    public class GKProgressiveRecipe : GKProgressiveItem
    {
        public GKProgressiveRecipe(List<List<string>> ids, GameModifications.IPlayerActions playerActions = null) : base(ids, playerActions)
        {
        }

        public override void Apply()
        {
            var index = this.applyCount;
            var ids = this.progressiveIDs[index];
            
            
            var unlocks = ids
                .Select(id => ((PlayerUtilities.UnlockType, string))(PlayerUtilities.UnlockType.Craft, id))
                .ToList();
            PlayerActions.ApplyUnlocks(unlocks);

            this.applyCount++;
        }
    }
}