using System;
using System.Collections.Generic;
using GraveyardKeepelago.GameModifications;

namespace GraveyardKeepelago.Tests.TestData;

public class FakePlayerActions : IPlayerActions
{
    public string LastFunctionCalled { get; private set; }
    public string LastAppliedBuffId { get; private set; }
    public List<(PlayerUtilities.UnlockType, string)> LastUnlocks { get; private set; }
    public string LastRelationNpcId { get; private set; }
    public int LastRelationAmount { get; private set; }
    public string LastDropItemId { get; private set; }
    public int LastDropItemAmount { get; private set; }
    public bool LastDropItemIsTechPoints => LastDropItemId != null && LastDropItemId.StartsWith("techpoints");
    public string LastDropItemTechColor { get; private set; }

    public void Clear()
    {
        LastFunctionCalled = null;
        LastAppliedBuffId = null;
        LastUnlocks = null;
        LastRelationNpcId = null;
        LastRelationAmount = 0;
        LastDropItemId = null;
        LastDropItemAmount = 0;
        LastDropItemTechColor = null;
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
