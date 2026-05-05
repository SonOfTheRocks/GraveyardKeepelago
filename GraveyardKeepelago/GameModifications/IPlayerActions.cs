using System.Collections.Generic;

namespace GraveyardKeepelago.GameModifications;

public interface IPlayerActions
{
    void ApplyPermanentBuff(string buffID);
    void ApplyUnlocks(List<(PlayerUtilities.UnlockType, string)> unlocks);
    void ApplyRelation(string npcID, int amount);
    void DropItem(string itemID, int amount = 1);
    void SkipIntro();
}
