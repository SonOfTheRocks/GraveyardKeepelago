using System.Collections.Generic;
using GraveyardKeepelago.GameModifications;
using GraveyardKeepelago.Logic;

namespace GraveyardKeepelago.Items
{
    public class GKRelation: GKItem
    {
        public readonly int Amount;
        
        public GKRelation(string npcID, int amount, IPlayerActions playerActions = null): base(new List<string>{NpcIdentity.Canonical(npcID)}, playerActions)
        {
            Amount = amount;
        }

        public GKRelation WithAmount(int amount)
        {
            return new GKRelation(IDs[0], amount, PlayerActions);
        }

        public override void Apply()
        {
            PlayerActions.ApplyRelation(IDs[0], Amount);
        }
    }
}
