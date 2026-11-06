using GraveyardKeepelago.Progression;

namespace GraveyardKeepelago.Tests;

public class SaveSlotEligibilityTests
{
    [Fact]
    public void NewGameRowsStaySelectableWithoutReadingAFile()
    {
        Assert.Null(SaveSlotEligibility.Reason(null!, true, () => throw new Exception("New-game file was read")));
        Assert.Null(SaveSlotEligibility.Reason(new SaveStartupGuard("Seed", 0, 1), true,
            () => throw new Exception("New-game file was read")));
    }

    [Theory]
    [InlineData("Seed", 0, 1, true)]
    [InlineData("Other seed", 0, 1, false)]
    [InlineData("Seed", 1, 1, false)]
    [InlineData("Seed", 0, 2, false)]
    public void ListAvailabilityMatchesTheLoadGuardWithoutChangingTheSave(string seed, int team, int slot, bool enabled)
    {
        var records = new List<string>();
        new SaveStartupGuard("Seed", 0, 1).ContinueNewSave(records, () => { });
        var before = records.ToArray();
        var reason = SaveSlotEligibility.Reason(new SaveStartupGuard(seed, team, slot), false, () => records);
        Assert.Equal(enabled, reason == null);
        if (!enabled) Assert.Contains("connected server", reason!);
        Assert.Equal(before, records);
    }

    [Fact]
    public void VanillaCorruptAndUnreadableSavesHaveAHoverReasonInsteadOfBeingSelectable()
    {
        var guard = new SaveStartupGuard("Seed", 0, 1);
        Assert.Contains("not an Archipelago save", SaveSlotEligibility.Reason(guard, false, () => new List<string>())!);
        Assert.Contains("corrupt", SaveSlotEligibility.Reason(guard, false,
            () => new List<string> { SaveProgress.RecordPrefix + "bad" })!);
        Assert.Contains("Missing file", SaveSlotEligibility.Reason(guard, false, () => throw new IOException("Missing file"))!);
    }

    [Fact]
    public void ReopeningWithAnotherServerReevaluatesAvailability()
    {
        var records = new List<string>();
        new SaveStartupGuard("Seed", 0, 1).ContinueNewSave(records, () => { });
        Assert.Null(SaveSlotEligibility.Reason(new SaveStartupGuard("Seed", 0, 1), false, () => records));
        Assert.NotNull(SaveSlotEligibility.Reason(new SaveStartupGuard("Other", 0, 1), false, () => records));
        Assert.Contains("Connect to its Archipelago server", SaveSlotEligibility.Reason(null!, false, () => records)!);
    }

    [Fact]
    public void OfflineListAllowsVanillaButBlocksApSavesWithoutChangingRecords()
    {
        var vanilla = new List<string> { "vanilla phrase" };
        Assert.Null(SaveSlotEligibility.Reason(null!, false, () => vanilla));
        var records = new List<string>();
        new SaveStartupGuard("Seed", 0, 1).ContinueNewSave(records, () => { });
        var before = records.ToArray();
        Assert.Contains("Connect to its Archipelago server", SaveSlotEligibility.Reason(null!, false, () => records)!);
        Assert.Equal(before, records);
        Assert.Contains("Missing file", SaveSlotEligibility.Reason(null!, false, () => throw new IOException("Missing file"))!);
    }
}
