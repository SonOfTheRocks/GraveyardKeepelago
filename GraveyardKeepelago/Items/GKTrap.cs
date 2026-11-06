using GraveyardKeepelago.GameModifications;

namespace GraveyardKeepelago.Items;

public sealed class GKTrap : GKItem
{
    public GKTrap(string trapID, IPlayerActions actions) : base(trapID, actions) { }
    public override void Apply() => PlayerActions.QueueTrap(IDs[0]);
}
