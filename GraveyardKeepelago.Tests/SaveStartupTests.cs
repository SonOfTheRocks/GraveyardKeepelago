using GraveyardKeepelago.Progression;
using GraveyardKeepelago.Technologies;

namespace GraveyardKeepelago.Tests;

public class SaveStartupTests
{
    [Fact]
    public void OfflineLoadAcceptsVanillaAndRejectsApBeforeWorldInitialization()
    {
        var vanilla = new List<string> { "vanilla phrase" };
        var accepted = 0;
        SaveStartupGuard.ContinueLoadedSave(null!, vanilla, () => accepted++);
        Assert.Equal(new[] { "vanilla phrase" }, vanilla);
        var records = BoundRecords();
        var before = records.ToArray();
        var error = Assert.Throws<InvalidDataException>(() =>
            SaveStartupGuard.ContinueLoadedSave(null!, records, () => accepted++));
        Assert.Contains("Connect to its Archipelago server", error.Message);
        Assert.Equal(1, accepted);
        Assert.Equal(before, records);
    }

    [Theory]
    [InlineData("graveyard_keepelago:progress:v1:bad")]
    [InlineData("graveyard_keepelago:progress:v2:future")]
    public void OfflineLoadNeverTreatsAnUnreadableOrFutureApRecordAsVanilla(string record)
    {
        var records = new List<string> { record };
        Assert.Throws<InvalidDataException>(() => SaveStartupGuard.ContinueLoadedSave(null!, records,
            () => Assert.Fail("AP save reached vanilla world initialization")));
        Assert.Contains("Connect to its Archipelago server", SaveSlotEligibility.Reason(null!, false, () => records)!);
        Assert.Equal(new[] { record }, records);
    }

    [Fact]
    public void ConnectionChangesBetweenListAndLoadAreValidatedAgain()
    {
        var guard = new SaveStartupGuard("Seed A", 2, 7);
        var records = BoundRecords();
        Assert.Null(SaveSlotEligibility.Reason(guard, false, () => records));
        Assert.Throws<InvalidDataException>(() => SaveStartupGuard.ContinueLoadedSave(null!, records,
            () => Assert.Fail("Disconnected AP save loaded")));
        Assert.NotNull(SaveSlotEligibility.Reason(null!, false, () => records));
        var accepted = false;
        SaveStartupGuard.ContinueLoadedSave(guard, records, () => accepted = true);
        Assert.True(accepted);
        Assert.Throws<InvalidDataException>(() => SaveStartupGuard.ContinueLoadedSave(
            new SaveStartupGuard("Other", 2, 7), records, () => Assert.Fail("Wrong seed loaded")));
    }

    [Fact]
    public void ConnectedNewGameBindsBeforeItsFirstNativeSaveAndCanThenBeLoaded()
    {
        var records = new List<string> { "vanilla phrase" };
        var guard = new SaveStartupGuard("Seed A", 2, 7);
        var saved = false;
        guard.ContinueNewSave(records, () =>
        {
            var bound = SaveProgress.Load(records, "Seed A", 2, 7);
            Assert.Empty(bound.Receipts);
            Assert.Empty(bound.Checks);
            saved = true;
        });
        Assert.True(saved);
        var loaded = false;
        guard.ContinueLoadedSave(records, () => loaded = true);
        Assert.True(loaded);
        Assert.Equal(2, records.Count);
        Assert.Contains("vanilla phrase", records);
    }

    [Theory]
    [InlineData("Other seed", 2, 7)]
    [InlineData("Seed A", 3, 7)]
    [InlineData("Seed A", 2, 8)]
    public void WrongSeedTeamOrSlotCannotReachWorldInitializationOrNetworkWrites(string seed, int team, int slot)
    {
        var records = BoundRecords();
        var before = records.ToArray();
        var continuation = false;
        var error = Assert.Throws<InvalidDataException>(() =>
            new SaveStartupGuard(seed, team, slot).ContinueLoadedSave(records, () => continuation = true));
        Assert.Contains("connected server", error.Message);
        Assert.Contains("Seed A", error.Message);
        Assert.False(continuation);
        Assert.Equal(before, records);
    }

    [Fact]
    public void ExistingVanillaSaveIsRejectedWithoutCreatingAnApRecordOrStartingTheWorld()
    {
        var records = new List<string> { "completed vanilla quest", "late-game phrase" };
        var before = records.ToArray();
        var error = Assert.Throws<InvalidDataException>(() =>
            new SaveStartupGuard("Seed A", 2, 7).ContinueLoadedSave(records,
                () => Assert.Fail("Unbound save reached world/AP initialization")));
        Assert.Contains("not an Archipelago save", error.Message);
        Assert.Equal(before, records);
        Assert.Throws<InvalidDataException>(() => SaveProgress.Load(records, "Seed A", 2, 7));
    }

    [Fact]
    public void ExistingBindingCannotBeOverwrittenByTheNewGamePath()
    {
        var records = BoundRecords();
        var before = records.ToArray();
        Assert.Throws<InvalidDataException>(() =>
            new SaveStartupGuard("Another seed", 0, 1).ContinueNewSave(records,
                () => Assert.Fail("Existing save was rebound")));
        Assert.Equal(before, records);
    }

    [Theory]
    [InlineData("bad")]
    [InlineData("e30=")]
    [InlineData("bnVsbA==")]
    [InlineData("e3Z9")]
    public void CorruptMetadataFailsClosedBeforeWorldInitialization(string payload)
    {
        var records = new List<string> { SaveProgress.RecordPrefix + payload };
        var before = records.ToArray();
        Assert.Throws<InvalidDataException>(() =>
            new SaveStartupGuard("Seed A", 2, 7).ContinueLoadedSave(records,
                () => Assert.Fail("Corrupt save reached initialization")));
        Assert.Equal(before, records);
    }

    [Fact]
    public void DuplicateRecordsAndUnsupportedVersionsAreRejectedWithoutRewritingThem()
    {
        var duplicate = BoundRecords();
        duplicate.Add(duplicate[0]);
        Assert.Throws<InvalidDataException>(() =>
            new SaveStartupGuard("Seed A", 2, 7).ContinueLoadedSave(duplicate, () => Assert.Fail("Duplicate accepted")));
        var records = new List<string>();
        new SaveProgress { Seed = "Seed A", Team = 2, Slot = 7, Version = 99 }.Store(records);
        var before = records.ToArray();
        Assert.Throws<InvalidDataException>(() =>
            new SaveStartupGuard("Seed A", 2, 7).ContinueLoadedSave(records, () => Assert.Fail("Unsupported version accepted")));
        Assert.Equal(before, records);
    }

    [Fact]
    public void ReselectingDifferentSavesAlwaysRevalidatesTheActualRecords()
    {
        var guard = new SaveStartupGuard("Seed A", 2, 7);
        var accepted = 0;
        guard.ContinueLoadedSave(BoundRecords(), () => accepted++);
        Assert.Throws<InvalidDataException>(() => guard.ContinueLoadedSave(new List<string>(), () => accepted++));
        var other = new List<string>();
        new SaveStartupGuard("Seed B", 2, 7).ContinueNewSave(other, () => { });
        Assert.Throws<InvalidDataException>(() => guard.ContinueLoadedSave(other, () => accepted++));
        guard.ContinueLoadedSave(BoundRecords(), () => accepted++);
        Assert.Equal(2, accepted);
    }

    [Fact]
    public void ReconnectingStillChecksSeedAndSlotAndKeepsExplicitProgress()
    {
        var records = BoundRecords();
        var progress = SaveProgress.Load(records, "Seed A", 2, 7);
        progress.Checks.Add("A quest check");
        progress.PendingTraps.Add("bats");
        progress.Store(records);
        var guard = new SaveStartupGuard("Seed A", 2, 7);
        guard.ContinueLoadedSave(records, () =>
        {
            var loaded = SaveProgress.Load(records, "Seed A", 2, 7);
            Assert.Equal(new[] { "A quest check" }, loaded.Checks);
            Assert.Equal(new[] { "bats" }, loaded.PendingTraps);
            Assert.Throws<InvalidDataException>(() => loaded.ValidateIdentity("Seed B", 2, 7));
        });
    }

    [Fact]
    public void PreexistingTechnologiesProduceNoChecksUntilAnActualResearchEvent()
    {
        var catalog = TechnologyCatalog.LoadDefault();
        var active = catalog.Definitions["Faith"].Locations.ToHashSet();
        var researched = new List<string> { "Faith" };
        var emitted = new List<string>();
        var research = new TechnologyResearch(catalog, active, researched, _ => { }, emitted.AddRange);
        Assert.Empty(emitted);
        Assert.True(research.Research("Faith"));
        Assert.Equal(active.Order(), emitted.Order());
    }

    private static List<string> BoundRecords()
    {
        var records = new List<string>();
        new SaveStartupGuard("Seed A", 2, 7).ContinueNewSave(records, () => { });
        return records;
    }

    [Fact]
    public void EmptyServerIdentityNeverCreatesOrLoadsAnApBinding()
    {
        var records = new List<string>();
        var guard = new SaveStartupGuard("", 0, 1);
        Assert.Throws<InvalidDataException>(() => guard.ContinueNewSave(records, () => Assert.Fail("Missing seed created a save")));
        Assert.Empty(records);
        Assert.Throws<InvalidDataException>(() => guard.ContinueLoadedSave(BoundRecords(), () => Assert.Fail("Missing seed accepted a save")));
    }

    [Fact]
    public void NullRecordListsAndEntriesAreRejectedWithAnActionableError()
    {
        var guard = new SaveStartupGuard("Seed A", 2, 7);
        foreach (var records in new List<string>?[] { null, new() { null! } })
        {
            Assert.Throws<InvalidDataException>(() => guard.ContinueLoadedSave(records!, () => Assert.Fail("Corrupt records accepted")));
            Assert.Throws<InvalidDataException>(() => guard.ContinueNewSave(records!, () => Assert.Fail("Corrupt records rebound")));
        }
    }
}
