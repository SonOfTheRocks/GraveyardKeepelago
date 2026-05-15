using System.Collections.Generic;
using GraveyardKeepelago.GameModifications;

namespace GraveyardKeepelago.Items
{
    public class GKIngameItem: GKItem
    {
        private readonly int _amount;
        
        public GKIngameItem(string id, IPlayerActions playerActions = null): base(new List<string>{id}, playerActions)
        {
        }

        public override void Apply()
        {
            var itemID = this.IDs[0];
            PlayerActions.DropItem(itemID);
        }
    }
}