using System;

namespace GraveyardKeepelago.GameModifications;

public static class RelationReward
{
    public static void Apply(string npcID, int amount, Action<string> ensureKnown,
        Action<string, int> increase, Action<string, int> showBubble, Action<Exception> reportPresentationFailure)
    {
        // Required save changes must succeed before reporting this item as applied.
        ensureKnown(npcID);
        increase(npcID, amount);
        // An NPC can be absent/unspawned. A cosmetic failure must not leave the
        // receipt unrecorded after the relation value has already increased.
        try { showBubble?.Invoke(npcID, amount); }
        catch (Exception ex) { reportPresentationFailure?.Invoke(ex); }
    }
}
