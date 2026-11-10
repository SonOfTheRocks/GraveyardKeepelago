using System.Collections.Generic;

namespace GraveyardKeepelago.GameModifications;

public interface IPlayerActions
{
    void ApplyPermanentBuff(string buffID);
    void ApplyUnlocks(List<(PlayerUtilities.UnlockType, string)> unlocks);
    void ApplyRelation(string npcID, int amount);
    void AddResearchPoints(string color, int amount);
    void UnlockTechnologies(List<string> technologyIDs);
    void RunFlowScript(string scriptID);
    void DropItem(string itemID, int amount = 1);
    void SkipIntro();
    void QueueTrap(string trapID);
}
