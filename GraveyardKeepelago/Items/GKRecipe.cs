using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.GameModifications;

namespace GraveyardKeepelago.Items
{
    public class GKRecipe: GKItem
    {
        public GKRecipe(string id, IPlayerActions playerActions = null) : base(id, playerActions)
        {
        }
        
        public GKRecipe(List<string> ids, IPlayerActions playerActions = null) : base(ids, playerActions)
        {
        }

        public override void Apply()
        {
            var unlocks = IDs
                .Select(id => (PlayerUtilities.UnlockType.Craft, id))
                .ToList();
            PlayerActions.ApplyUnlocks(unlocks);
        }
    }
}