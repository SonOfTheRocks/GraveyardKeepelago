using System;
using System.Collections.Generic;
using GraveyardKeepelago.GameModifications;

namespace GraveyardKeepelago.Tests.TestData;

public class FakePlayerActions : IPlayerActions
{
    public string? LastTrapID { get; private set; }
    public Action<string>? TrapQueueCallback { get; set; }
    public void QueueTrap(string trapID) { LastFunctionCalled = "QueueTrap"; LastTrapID = trapID; TrapQueueCallback?.Invoke(trapID); }
    public string LastFunctionCalled { get; private set; }
    public string LastAppliedBuffId { get; private set; }
    public List<(PlayerUtilities.UnlockType, string)> LastUnlocks { get; private set; }
    public string LastRelationNpcId { get; private set; }
    public int LastRelationAmount { get; private set; }
    public string LastDropItemId { get; private set; }
    public int LastDropItemAmount { get; private set; }
    public bool LastDropItemIsTechPoints => LastDropItemId != null && LastDropItemId.StartsWith("techpoints");
    public string LastDropItemTechColor { get; private set; }
    public string LastResearchPointColor { get; private set; }
    public int LastResearchPointAmount { get; private set; }
    public List<string> LastUnlockedTechnologies { get; private set; }
    public string LastFlowScriptId { get; private set; }

    public void Clear()
    {
        LastTrapID = null;
        LastFunctionCalled = null;
        LastAppliedBuffId = null;
        LastUnlocks = null;
        LastRelationNpcId = null;
        LastRelationAmount = 0;
        LastDropItemId = null;
        LastDropItemAmount = 0;
        LastDropItemTechColor = null;
        LastResearchPointColor = null;
        LastResearchPointAmount = 0;
        LastUnlockedTechnologies = null;
        LastFlowScriptId = null;
    }

    public void ApplyPermanentBuff(string buffID)
    {
        LastFunctionCalled = "ApplyPermanentBuff";
        LastAppliedBuffId = buffID;
        LastUnlocks = null;
        LastRelationNpcId = null;
        LastRelationAmount = 0;
        LastDropItemId = null;
        LastDropItemAmount = 0;
        LastDropItemTechColor = null;
    }

    public void ApplyUnlocks(List<(PlayerUtilities.UnlockType, string)> unlocks)
    {
        LastFunctionCalled = "ApplyUnlocks";
        LastAppliedBuffId = null;
        LastUnlocks = unlocks;
        LastRelationNpcId = null;
        LastRelationAmount = 0;
        LastDropItemId = null;
        LastDropItemAmount = 0;
        LastDropItemTechColor = null;
    }

    public void ApplyRelation(string npcID, int amount)
    {
        LastFunctionCalled = "ApplyRelation";
        LastAppliedBuffId = null;
        LastUnlocks = null;
        LastRelationNpcId = npcID;
        LastRelationAmount = amount;
        LastDropItemId = null;
        LastDropItemAmount = 0;
        LastDropItemTechColor = null;
    }

    public void DropItem(string itemID, int amount = 1)
    {
        LastFunctionCalled = "DropItem";
        LastAppliedBuffId = null;
        LastUnlocks = null;
        LastRelationNpcId = null;
        LastRelationAmount = 0;
        LastDropItemId = itemID;
        LastDropItemAmount = amount;
        LastDropItemTechColor = itemID.StartsWith("techpoints") ?
            (itemID.EndsWith("red") ? "red" : itemID.EndsWith("green") ? "green" : "blue") : null;
    }

    public void AddResearchPoints(string color, int amount)
    {
        Clear();
        LastFunctionCalled = "AddResearchPoints";
        LastResearchPointColor = color;
        LastResearchPointAmount = amount;
    }

    public void UnlockTechnologies(List<string> technologyIDs)
    {
        Clear();
        LastFunctionCalled = "UnlockTechnologies";
        LastUnlockedTechnologies = technologyIDs;
    }

    public void RunFlowScript(string scriptID)
    {
        Clear();
        LastFunctionCalled = "RunFlowScript";
        LastFlowScriptId = scriptID;
    }

    public void SkipIntro()
    {
        LastFunctionCalled = "SkipIntro";
        LastAppliedBuffId = null;
        LastUnlocks = null;
        LastRelationNpcId = null;
        LastRelationAmount = 0;
        LastDropItemId = null;
        LastDropItemAmount = 0;
        LastDropItemTechColor = null;
    }
}
