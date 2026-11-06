using System.IO;
using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.GameModifications;
using GraveyardKeepelago.Items;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Progression;
using GraveyardKeepelago.Tests.TestData;
using KaitoKid.ArchipelagoUtilities.Net.Client;

namespace GraveyardKeepelago.Tests;

public class ProgressionTests
{
    private static ReceivedItem Receipt(int index, string name = "Extract: Flesh", long itemID = 10)
        => new("Location", name, "Player", index + 100, itemID, 1, index);

    [Fact]
    public void FleshRequiresItsActualReceiptAndUnlocksBothAutopsyTables()
    {
        var contract = ClientContractLoader.LoadDefault();
        var state = new SaveProgress();
        var policy = new UnlockPolicy(contract.Items.Values, state);
        var flesh = contract.Items["ITEM_LEARN_EXTRACT_FLESH"].Grant.CraftIDs;
        Assert.All(flesh, id => Assert.True(policy.CraftLocked(id)));
        Assert.False(policy.CraftLocked("uncontrolled vanilla craft"));
        // A different tutorial reward does not grant the flesh recipe.
        Assert.Empty(state.Crafts);
        var target = new UnlockTarget();
        policy.Apply(flesh.Select(id => (PlayerUtilities.UnlockType.Craft, id)), target);
        Assert.Equal(flesh.Order(), target.Crafts.Order());
        Assert.All(flesh, id => Assert.False(policy.CraftLocked(id)));
    }

    [Fact]
    public void HiddenVariantsAndCompositePerksUseCanonicalIDsAndTypedTargets()
    {
        var state = new SaveProgress();
        var policy = new UnlockPolicy(Array.Empty<ClientContractItem>(), state);
        var target = new UnlockTarget();
        var grants = new[] { (PlayerUtilities.UnlockType.Craft, "@recipe"),
            (PlayerUtilities.UnlockType.Work, "work"), (PlayerUtilities.UnlockType.Perk, "@perk"),
            (PlayerUtilities.UnlockType.Phrase, "@phrase") };
        policy.Apply(grants, target);
        policy.Apply(grants, target);
        Assert.Equal(new[] { "recipe" }, state.Crafts);
        Assert.Equal(new[] { "work" }, state.Works);
        Assert.Equal(new[] { "perk" }, state.Perks);
        Assert.Equal(1, target.PerkEffects);
        Assert.Equal(new[] { "phrase" }, target.Phrases);
    }

    [Fact]
    public void EveryDirectAndProgressiveRecipeIsControlledBeforeItsReceipt()
    {
        var contract = ClientContractLoader.LoadDefault();
        var policy = new UnlockPolicy(contract.Items.Values, new SaveProgress());
        foreach (var definition in contract.Items.Values)
        {
            var grant = definition.Grant;
            foreach (var id in grant.CraftIDs ?? new()) Assert.True(policy.CraftLocked(id), definition.Name);
            if (grant.Type != "progressive_craft_unlock") continue;
            foreach (var stage in grant.Stages)
                foreach (var id in stage) Assert.True(policy.CraftLocked(id), definition.Name);
        }
        Assert.Equal(505, policy.ControlledCrafts.Count);
    }

    [Fact]
    public void SaveRoundTripKeepsIdentityOwnershipChecksAndReceiptOrder()
    {
        var phrases = new List<string> { "@vanilla_phrase" };
        var state = SaveProgress.Create(phrases, "Seed", 2, 3);
        state.Crafts.Add("recipe"); state.Works.Add("work"); state.Perks.Add("perk"); state.Checks.Add("check");
        state.Receipts.Add(ProcessedReceipt.From(Receipt(0)));
        state.Store(phrases); state.Store(phrases);
        Assert.Equal(2, phrases.Count);
        Assert.Contains("@vanilla_phrase", phrases);
        var loaded = SaveProgress.Load(phrases, "Seed", 2, 3);
        Assert.Equal(state.Crafts, loaded.Crafts);
        Assert.Equal(state.Works, loaded.Works);
        Assert.Equal(state.Perks, loaded.Perks);
        Assert.Equal(state.Checks, loaded.Checks);
        loaded.ValidateHistory(new[] { Receipt(0) });
    }

    [Theory]
    [InlineData("Other seed", 1, 1)]
    [InlineData("Seed", 2, 1)]
    [InlineData("Seed", 1, 2)]
    public void SaveCannotBeUsedWithAnotherSeedTeamOrSlot(string seed, int team, int slot)
    {
        var phrases = new List<string>();
        SaveProgress.Create(phrases, "Seed", 1, 1).Store(phrases);
        Assert.Throws<InvalidDataException>(() => SaveProgress.Load(phrases, seed, team, slot));
    }

    [Fact]
    public void CorruptOrAmbiguousMetadataIsNeverSilentlyReset()
    {
        Assert.ThrowsAny<Exception>(() => SaveProgress.Load(new[] { SaveProgress.RecordPrefix + "bad" }, "Seed", 1, 1));
        var records = new List<string>();
        SaveProgress.Create(records, "Seed", 1, 1).Store(records);
        records.Add(records[0]);
        Assert.Throws<InvalidDataException>(() => SaveProgress.Load(records, "Seed", 1, 1));
    }

    [Fact]
    public void ReconnectingToAnotherSeedIsRejectedBeforeChecksAreResent()
    {
        var state = SaveProgress.Create(new List<string>(), "Seed", 1, 2);
        state.ValidateIdentity("Seed", 1, 2);
        Assert.Throws<InvalidDataException>(() => state.ValidateIdentity("Other seed", 1, 2));
    }

    [Fact]
    public void OfflineChecksSurviveReloadAndIgnoreDisabledOrDuplicateLocations()
    {
        var records = new List<string>();
        var state = SaveProgress.Create(records, "Seed", 1, 1);
        var enabled = new Dictionary<string, long> { ["Enabled"] = 42 };
        Assert.Equal(new[] { "Enabled" }, state.RememberChecks(new[] { "Enabled", "Enabled", "Disabled" }, enabled));
        state.Store(records);
        var loaded = SaveProgress.Load(records, "Seed", 1, 1);
        Assert.Equal(new[] { "Enabled" }, loaded.Checks);
        Assert.Empty(loaded.RememberChecks(new[] { "Enabled" }, enabled));
    }

    [Fact]
    public void ReconnectAndReloadDoNotRepeatRewardsOrNotifications()
    {
        var records = new List<string>();
        var state = SaveProgress.Create(records, "Seed", 1, 1);
        var ledger = new ItemReceiptLedger(state, () => state.Store(records));
        var history = new[] { Receipt(0), Receipt(1) };
        var applied = new List<int>();
        bool Apply(ReceivedItem receipt) { applied.Add(receipt.UniqueId); return true; }
        ledger.ApplyHistory(history, Apply);
        ledger.ApplyHistory(history, Apply);
        var loaded = SaveProgress.Load(records, "Seed", 1, 1);
        var reloaded = new ItemReceiptLedger(loaded, () => loaded.Store(records));
        reloaded.ApplyHistory(history, Apply);
        reloaded.ApplyHistory(new[] { Receipt(0), Receipt(1), Receipt(2) }, Apply);
        Assert.Equal(new[] { 0, 1, 2 }, applied);
    }

    [Fact]
    public void FailedItemDoesNotAdvanceReceiptIndexOrLoseTheFollowingItem()
    {
        var state = new SaveProgress();
        var ledger = new ItemReceiptLedger(state, () => { });
        var history = new[] { Receipt(0), Receipt(1) };
        Assert.Throws<InvalidDataException>(() => ledger.ApplyHistory(history, receipt => receipt.UniqueId == 0));
        Assert.Single(state.Receipts);
        var retry = new List<int>();
        ledger.ApplyHistory(history, receipt => { retry.Add(receipt.UniqueId); return true; });
        Assert.Equal(new[] { 1 }, retry);
    }

    [Fact]
    public void ConflictingHistoryAndOutOfOrderReceiptsFailBeforeGrantingAnything()
    {
        var state = new SaveProgress();
        var ledger = new ItemReceiptLedger(state, () => { });
        ledger.ApplyHistory(new[] { Receipt(0) }, _ => true);
        Assert.Throws<InvalidDataException>(() => ledger.ApplyHistory(new[] { Receipt(0, itemID: 20) },
            _ => throw new Exception("Must not apply conflicting item")));
        Assert.Throws<InvalidDataException>(() => new ItemReceiptLedger(new SaveProgress(), () => { })
            .ApplyHistory(new[] { Receipt(2) }, _ => throw new Exception("Must not apply out-of-order item")));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ProgressiveStageRestoresAfterReloadAndExtraCopiesAreSafe(bool craft)
    {
        var actions = new FakePlayerActions();
        var stages = new List<List<string>> { new() { "first" }, new() { "second" } };
        GKProgressiveItem item = craft ? new GKProgressiveRecipe(stages, actions) : new GKProgressiveWork(stages, actions);
        var registry = new GKItemRegistry(); registry.Add("Progressive", item);
        var state = new SaveProgress();
        state.Receipts.Add(ProcessedReceipt.From(Receipt(0, "Progressive")));
        new ItemReceiptLedger(state, () => { }).RestoreProgressiveStages(registry);
        item.Apply();
        Assert.Equal("second", Assert.Single(actions.LastUnlocks).Item2);
        item.Apply();
        Assert.Equal(2, item.applyCount);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public void DLCFilteringOnlyControlsItemsEnabledForThatSlot(int mask)
    {
        var keys = new[] { SlotData.DLC_STRANGER_SINS_KEY, SlotData.DLC_GAME_OF_CRONE_KEY, SlotData.DLC_BETTER_SAVE_SOUL_KEY };
        var active = keys.Where((_, index) => (mask & (1 << index)) != 0).ToList();
        var dlcs = new DLCManager(new FakeLogger(), active);
        Assert.True(dlcs.IsContentEnabled("BASE_GAME"));
        Assert.Equal((mask & 1) != 0, dlcs.IsContentEnabled("STRANGER_SINS"));
        Assert.Equal((mask & 2) != 0, dlcs.IsContentEnabled("GAME_OF_CRONE"));
        Assert.Equal((mask & 4) != 0, dlcs.IsContentEnabled("BETTER_SAVE_SOUL"));
        var items = ClientContractLoader.LoadDefault().Items.Values.Where(item => dlcs.IsContentEnabled(item.DLC)).ToList();
        var policy = new UnlockPolicy(items, new SaveProgress());
        Assert.True(policy.CraftLocked("ex:mf_preparation_1:flesh"));
        if ((mask & 4) == 0) Assert.False(policy.CraftLocked("keeper_room_builddesk::soul_totem_keeper_room"));
    }

    private sealed class UnlockTarget : IUnlockTarget
    {
        public HashSet<string> Crafts { get; } = new();
        public HashSet<string> Works { get; } = new();
        public HashSet<string> Perks { get; } = new();
        public HashSet<string> Phrases { get; } = new();
        public int PerkEffects { get; private set; }
        public void UnlockCraft(string id) => Crafts.Add(id);
        public void UnlockWork(string id) => Works.Add(id);
        public void UnlockPerk(string id) { if (Perks.Add(id)) PerkEffects++; }
        public void UnlockPhrase(string id) => Phrases.Add(id);
    }
}
