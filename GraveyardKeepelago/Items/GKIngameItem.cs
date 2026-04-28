using System.Collections.Generic;
using GraveyardKeepelago.GameModifications;

namespace GraveyardKeepelago.Items
{
    public class GKIngameItem: GKItem
    {
        private readonly int _amount;
        
        public GKIngameItem(string id): base(new List<string>{id})
        {
        }

        public override void Apply()
        {
            var itemID = this.IDs[0];
            PlayerUtilities.DropItem(itemID);
        }
    }
}