using System.Collections.Generic;
using GraveyardKeepelago.GameModifications;

namespace GraveyardKeepelago.Items
{
    public class GKRelation: GKItem
    {
        public readonly int Amount;
        
        public GKRelation(string npcID, int amount): base(new List<string>{npcID})
        {
            Amount = amount;
        }

        public override void Apply()
        {
            PlayerUtilities.ApplyRelation(IDs[0], Amount);
        }
    }
}