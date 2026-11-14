using System.Collections.Generic;

namespace GraveyardKeepelago.GameModifications;

public interface ISharedGrantActions
{
    void ApplyUnlocks(IEnumerable<(PlayerUtilities.UnlockType Type, string ID)> unlocks);
    void ApplyPermanentBuff(string buffID);
    void ApplyRelation(string npcID, int amount);
    void DeliverItem(string itemID);
    void AddResearchPoints(string color, int amount);
    void UnlockTechnologies(IEnumerable<string> technologyIDs);
    void RunFlowScript(string scriptID);
}
