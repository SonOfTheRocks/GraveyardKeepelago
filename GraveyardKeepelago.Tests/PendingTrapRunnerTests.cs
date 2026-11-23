using System.Collections.ObjectModel;
using Archipelago.MultiClient.Net.Models;
using GraveyardKeepelago.Items;
using GraveyardKeepelago.Items.Grants;
using GraveyardKeepelago.Items.Traps;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Progression;
using KaitoKid.Utilities.Interfaces;
using Moq;
using Receiver = GraveyardKeepelago.Items.Receiving.APItemManager;

namespace GraveyardKeepelago.Tests;

public sealed class PendingTrapRunnerTests
{
    private static readonly (string Seed, int Team, int Slot) Identity = ("Seed", 0, 1);
    private static SaveProgress NewSave(IList<string>? records = null) =>
        SaveProgress.Bind(records ?? new List<string>(), Identity, true);
    private static bool Ready() => true;

    [Fact]
    public void SharedGrantsReceiptsReloadAndReplayFeedOneOrderedQueue()
    {
        var records = new List<string>();
        var save = NewSave(records);
        var factory = new ItemGrantFactory(ClientContractLoader.LoadDefault().Items.Values, Mock.Of<ILogger>());
        var history = new[] { new NetworkItem { Item = 488, Location = -1, Player = 1 },
            new NetworkItem { Item = 489, Location = -2, Player = 1 } };
        string Name(long id) => id == 488 ? "Bats Trap" : "Green Slime Trap";
        GrantResult Apply(IItemGrant grant, Action<string> queue)
        {
            queue(Assert.IsType<TrapGrant>(grant).TrapID);
            return GrantResult.Applied;
        }
        var receiver = new Receiver(save, factory.Create);
        receiver.ProcessReceivedItems(history, Ready, Name, Apply);
        Assert.Equal(new[] { "bats", "green_slimes" }, save.Data.PendingTraps);
        var spawned = new List<string>();
        var runner = new PendingTrapRunner(save, save.Store, id => () => spawned.Add(id), Assert.Fail, 0);
        runner.Update(5, Ready);
        Assert.Equal("bats", Assert.Single(spawned));

        var reload = SaveProgress.Bind(records.ToList(), Identity, false);
        new Receiver(reload, factory.Create).ProcessReceivedItems(history, Ready, Name, Apply);
        Assert.Equal("green_slimes", Assert.Single(reload.Data.PendingTraps));
        Assert.Equal(2, reload.Data.Receipts.Count);
        var reloadedRunner = new PendingTrapRunner(reload, reload.Store, id => () => spawned.Add(id), Assert.Fail, 100);
        reloadedRunner.Update(104, Ready);
        Assert.Single(spawned);
        reloadedRunner.Update(105, Ready);
        Assert.Equal(new[] { "bats", "green_slimes" }, spawned);
        Assert.Empty(reload.Data.PendingTraps);
    }

    [Fact]
    public void UnsafeGameplayAndInactiveSaveDelayActivationAndStartGracePeriod()
    {
        var save = NewSave();
        save.Data.PendingTraps.AddRange(["bats", "green_slimes"]);
        var count = 0;
        var runner = new PendingTrapRunner(save, save.Store, _ => () => count++, Assert.Fail, 0);
        runner.Update(4, Ready);
        runner.Update(20, () => false); // Death, menu, loading, disconnect or a different active save.
        runner.Update(21, Ready);
        Assert.Equal(0, count);
        runner.Update(22, Ready);
        runner.Update(31, Ready);
        Assert.Equal(1, count);
        runner.Update(32, Ready);
        Assert.Equal(2, count);
    }

    [Fact]
    public void ContextChangeDuringPreparationDoesNotConsumeOrSpawn()
    {
        var save = NewSave();
        save.Data.PendingTraps.Add("bats");
        var active = true;
        var runner = new PendingTrapRunner(save, save.Store, _ =>
        {
            active = false;
            return () => Assert.Fail("Spawned outside original save/slot.");
        }, Assert.Fail, 0);
        runner.Update(5, () => active);
        Assert.Single(save.Data.PendingTraps);
    }

    [Fact]
    public void ReentrantUpdateCannotConsumeTheNextTrap()
    {
        var save = NewSave();
        save.Data.PendingTraps.AddRange(["bats", "green_slimes"]);
        PendingTrapRunner? runner = null;
        var spawned = new List<string>();
        runner = new PendingTrapRunner(save, save.Store, id => () =>
        {
            spawned.Add(id);
            runner!.Update(100, Ready);
        }, Assert.Fail, 0);
        runner.Update(5, Ready);
        Assert.Equal("bats", Assert.Single(spawned));
        Assert.Equal("green_slimes", Assert.Single(save.Data.PendingTraps));
    }

    [Fact]
    public void SafePreparationFailureRetriesWithBackoffAndDeduplicatesErrors()
    {
        var save = NewSave();
        save.Data.PendingTraps.Add("bats");
        var blocked = true;
        var spawned = 0;
        var errors = new List<string>();
        var attempts = 0;
        var runner = new PendingTrapRunner(save, save.Store, _ =>
        {
            attempts++;
            if (blocked) throw new InvalidOperationException("No clear positions");
            return () => spawned++;
        }, errors.Add, 0);
        runner.Update(5, Ready);
        runner.Update(6, Ready);
        runner.Update(15, Ready);
        Assert.Equal(2, attempts);
        Assert.Single(errors);
        Assert.Single(save.Data.PendingTraps);
        blocked = false;
        runner.Update(25, Ready);
        Assert.Equal(1, spawned);
        Assert.Empty(save.Data.PendingTraps);
    }

    [Fact]
    public void FailedStorageRestoresQueueAndNeverStartsNativeSpawn()
    {
        var records = new FailingRecords();
        var save = NewSave(records);
        save.Data.PendingTraps.Add("bats");
        save.Store();
        records.Fail = true;
        var spawned = 0;
        var errors = new List<string>();
        var runner = new PendingTrapRunner(save, save.Store, _ => () => spawned++, errors.Add, 0);
        runner.Update(5, Ready);
        Assert.Equal(0, spawned);
        Assert.Single(errors);
        Assert.Equal("bats", Assert.Single(save.Data.PendingTraps));
        Assert.Single(SaveProgress.Bind(records, Identity, false).Data.PendingTraps);
        records.Fail = false;
        runner.Update(15, Ready);
        Assert.Equal(1, spawned);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void UncertainOrPartialSpawnIsConsumedBeforeEffectAndNextTrapCanProceed(int partialCount)
    {
        var records = new List<string>();
        var save = NewSave(records);
        save.Data.PendingTraps.AddRange(["bats", "green_slimes"]);
        save.Store();
        var spawned = 0;
        var errors = new List<string>();
        var runner = new PendingTrapRunner(save, save.Store, id => () =>
        {
            Assert.DoesNotContain(id, SaveProgress.Bind(records, Identity, false).Data.PendingTraps);
            if (id == "bats")
            {
                spawned += partialCount;
                throw new InvalidOperationException("Uncertain native failure");
            }
            spawned += 4;
        }, errors.Add, 0);
        runner.Update(5, Ready);
        Assert.Equal(partialCount, spawned);
        Assert.Single(errors);
        runner.Update(15, Ready);
        Assert.Equal(partialCount + 4, spawned);
        Assert.Empty(SaveProgress.Bind(records, Identity, false).Data.PendingTraps);
    }

    [Theory]
    [InlineData("bats", "bat_test")]
    [InlineData("green_slimes", "slime_test")]
    public void ConcreteTrapResolvesExactNativeGroup(string id, string nativeID)
    {
        var trap = MonsterTrapDefinition.Get(id);
        Assert.Equal(nativeID, trap.MonsterID);
        Assert.Equal(4, trap.Count);
    }

    [Theory]
    [InlineData("Monsters")]
    [InlineData("Bats Trap")]
    [InlineData("unknown")]
    public void UnknownIDsAreRejectedWithoutRandomFallback(string id) =>
        Assert.Throws<InvalidDataException>(() => MonsterTrapDefinition.Get(id));

    private sealed class FailingRecords : Collection<string>
    {
        public bool Fail { get; set; }
        protected override void SetItem(int index, string item)
        {
            if (Fail) throw new IOException("Storage failure");
            base.SetItem(index, item);
        }
    }
}
