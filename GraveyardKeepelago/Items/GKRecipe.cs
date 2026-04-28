using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.GameModifications;

namespace GraveyardKeepelago.Items
{
    public class GKRecipe: GKItem
    {
        public GKRecipe(string id) : base(id)
        {
        }
        
        public GKRecipe(List<string> ids) : base(ids)
        {
        }

        public override void Apply()
        {
            var unlocks = IDs
                .Select(id => (PlayerUtilities.UnlockType.Craft, id))
                .ToList();
            PlayerUtilities.ApplyUnlocks(unlocks);
        }
    }
}