using GraveyardKeepelago.Items;
using GraveyardKeepelago.Locations;
using GraveyardKeepelago.Progression;
using static GraveyardKeepelago.Tests.ReceivedItemHistoryTests;

namespace GraveyardKeepelago.Tests;

public sealed class SaveBoundItemDeliveryTests
{
    private static readonly (string Seed, int Team, int Slot) Identity = ("Seed", 0, 1);
    private static readonly Dictionary<string, long> Locations = new() { ["Research"] = 17 };

    [Fact]
    public void InitialItemsWaitForReadinessAndRejectAChangedSessionMidBatch()
    {
        var save = SaveProgress.Bind(new List<string>(), Identity, newSave: true);
        var delivery = new SaveBoundItemDelivery(save);
        var history = new[] { Item(1), Item(2) };
        var active = false;
        var grants = new List<string>();

        GrantResult Apply(string name, int count, Action<string> queueTrap)
        {
            grants.Add(name);
            active = false;
            return GrantResult.Applied;
        }

        delivery.Deliver(history, () => active, Name, Apply);
        Assert.Empty(grants);

        active = true;
        delivery.Deliver(history, () => active, Name, Apply);
        Assert.Equal("Item 1", Assert.Single(grants));
        Assert.Single(save.Data.Receipts);

        active = true;
        delivery.Deliver(history, () => active, Name, Apply);
        Assert.Equal(new[] { "Item 1", "Item 2" }, grants);
    }

    [Fact]
    public void ReloadAndReplaySkipConfirmedIndicesButPreserveProgressiveStages()
    {
        var records = new List<string>();
        var save = SaveProgress.Bind(records, Identity, newSave: true);
        var stages = new List<int>();
        GrantResult Apply(string name, int count, Action<string> queueTrap)
        {
            stages.Add(count);
            return GrantResult.Applied;
        }

        var delivery = new SaveBoundItemDelivery(save);
        delivery.Deliver([Item(1), Item(1)], Ready, Name, Apply);
        delivery.Deliver([Item(1), Item(1)], Ready, Name, Apply);

        var reloaded = SaveProgress.Bind(records.ToList(), Identity, newSave: false);
        new SaveBoundItemDelivery(reloaded).Deliver([Item(1), Item(1), Item(1)], Ready, Name, Apply);
        Assert.Equal(new[] { 0, 1, 2 }, stages);
        Assert.Equal(new[] { 0, 1, 2 }, reloaded.Data.Receipts.Select(receipt => receipt.Index));
    }

    [Theory]
    [InlineData(2, -2, 1)]
    [InlineData(1, -1, 1)]
    [InlineData(1, -2, 2)]
    public void ConflictingPastReceiptPreventsAnyNewGrant(long id, long location, int player)
    {
        var save = SaveProgress.Bind(new List<string>(), Identity, newSave: true);
        var delivery = new SaveBoundItemDelivery(save);
        delivery.Deliver([Item(1)], Ready, Name, Applied);

        Assert.Throws<InvalidDataException>(() =>
            delivery.Deliver([Item(id, location, player), Item(3)], Ready, Name, UnexpectedGrant));
        Assert.Throws<InvalidDataException>(() => delivery.Deliver([], Ready, Name, UnexpectedGrant));
        Assert.Single(save.Data.Receipts);
    }

    [Fact]
    public void ExplicitFailureRetriesTheSameStageBeforeDeliveringLaterItems()
    {
        var records = new List<string>();
        var save = SaveProgress.Bind(records, Identity, newSave: true);
        new SaveBoundItemDelivery(save).Deliver([Item(1)], Ready, Name, Applied);

        var attempts = new List<int>();
        var history = new[] { Item(1), Item(1), Item(1) };
        var delivery = new SaveBoundItemDelivery(save);
        delivery.Deliver(history, Ready, Name, (_, stage, _) =>
        {
            attempts.Add(stage);
            return GrantResult.Retry;
        });
        Assert.Single(SaveProgress.Bind(records, Identity, newSave: false).Data.Receipts);

        delivery.Deliver(history, Ready, Name, (_, stage, _) =>
        {
            attempts.Add(stage);
            return GrantResult.Applied;
        });
        Assert.Equal(new[] { 1, 1, 2 }, attempts);
    }

    [Fact]
    public void UncertainNativeFailureStopsTheSameSaveAfterReconnectAndReload()
    {
        var records = new List<string>();
        var save = SaveProgress.Bind(records, Identity, newSave: true);
        var delivery = new SaveBoundItemDelivery(save);
        var history = new[] { Item(1) };

        Assert.Throws<IOException>(() =>
            delivery.Deliver(history, Ready, Name, (_, _, _) => throw new IOException("Partly applied")));
        Assert.Empty(save.Data.Receipts);
        Assert.Throws<InvalidDataException>(() => delivery.Deliver(history, Ready, Name, UnexpectedGrant));
        var reloaded = SaveProgress.Bind(records.ToList(), Identity, newSave: false);
        Assert.Throws<InvalidDataException>(() =>
            new SaveBoundItemDelivery(reloaded).Deliver(history, Ready, Name, UnexpectedGrant));
    }

    [Fact]
    public void AsyncGrantWaitsForCompletionAndBlocksLaterItems()
    {
        var records = new List<string>();
        var save = SaveProgress.Bind(records, Identity, newSave: true);
        var delivery = new SaveBoundItemDelivery(save);
        var history = new[] { Item(1), Item(2) };
        var finished = false;
        var grants = new List<string>();
        GrantResult Apply(string name, int count, Action<string> queueTrap)
        {
            grants.Add(name);
            return finished ? GrantResult.Applied : GrantResult.Pending;
        }

        delivery.Deliver(history, Ready, Name, Apply);
        Assert.Equal("Item 1", Assert.Single(grants));
        Assert.Empty(save.Data.Receipts);
        var during = SaveProgress.Bind(records.ToList(), Identity, newSave: false);
        Assert.Throws<InvalidDataException>(() =>
            new SaveBoundItemDelivery(during).Deliver(history, Ready, Name, UnexpectedGrant));

        finished = true;
        delivery.Deliver(history, Ready, Name, Apply);
        Assert.Equal(new[] { "Item 1", "Item 1", "Item 2" }, grants);
        Assert.Equal(2, save.Data.Receipts.Count);
        Assert.Null(save.Data.InterruptedReceipt);
    }

    [Fact]
    public void APartialRecordWriteCannotConfirmOrReplayAnAppliedGrant()
    {
        var records = new FailingRecordList();
        var save = SaveProgress.Bind(records, Identity, newSave: true);
        var delivery = new SaveBoundItemDelivery(save);
        var history = new[] { Item(1) };
        Assert.Throws<IOException>(() => delivery.Deliver(history, Ready, Name, (_, _, queueTrap) =>
        {
            queueTrap("bats");
            records.FailWrites = true;
            return GrantResult.Applied;
        }));
        records.FailWrites = false;

        var reloaded = SaveProgress.Bind(records, Identity, newSave: false);
        Assert.Empty(reloaded.Data.PendingTraps);
        Assert.Empty(reloaded.Data.Receipts);
        Assert.Equal(0, reloaded.Data.InterruptedReceipt);
        Assert.Throws<InvalidDataException>(() => delivery.Deliver(history, Ready, Name, UnexpectedGrant));
    }

    [Fact]
    public void TrapQueueAndReceiptsShareTheCheckRecordAndSurviveReloadInOrder()
    {
        var records = new List<string>();
        var checks = SaveBoundLocationChecks.Bind(records, Identity, Locations, newSave: true);
        var history = new[] { Item(1), Item(2), Item(3) };
        new SaveBoundItemDelivery(checks.Progress).Deliver(history, Ready, Name, (name, _, queueTrap) =>
        {
            if (name == "Item 1") queueTrap("bats");
            if (name == "Item 3") queueTrap("green_slimes");
            return GrantResult.Applied;
        });

        checks.AddCheckedLocation("Research");
        Assert.Single(records);
        var reloaded = SaveBoundLocationChecks.Bind(records.ToList(), Identity, Locations, newSave: false);
        new SaveBoundItemDelivery(reloaded.Progress).Deliver(history, Ready, Name, UnexpectedGrant);
        Assert.Equal(new[] { "bats", "green_slimes" }, reloaded.Progress.Data.PendingTraps);
        Assert.Equal("Research", Assert.Single(reloaded.Progress.Data.Checks));
        Assert.Equal(3, reloaded.Progress.Data.Receipts.Count);
    }

    [Fact]
    public void FailureToRecordTheStartOfAnAttemptCanBeRetriedWithoutAnyGrantHavingRun()
    {
        var records = new FailingRecordList();
        var save = SaveProgress.Bind(records, Identity, newSave: true);
        var delivery = new SaveBoundItemDelivery(save);
        var history = new[] { Item(1) };

        records.FailWrites = true;
        Assert.Throws<IOException>(() => delivery.Deliver(history, Ready, Name, UnexpectedGrant));
        Assert.Null(save.Data.InterruptedReceipt);

        records.FailWrites = false;
        delivery.Deliver(history, Ready, Name, Applied);
        Assert.Single(save.Data.Receipts);
    }

    private static bool Ready() => true;
    private static string Name(long id) => $"Item {id}";
    private static GrantResult Applied(string name, int count, Action<string> queueTrap) => GrantResult.Applied;
    private static GrantResult UnexpectedGrant(string name, int count, Action<string> queueTrap) =>
        throw new Xunit.Sdk.XunitException($"Unexpected grant '{name}'.");

    private sealed class FailingRecordList : System.Collections.ObjectModel.Collection<string>
    {
        public bool FailWrites { get; set; }

        protected override void SetItem(int index, string item)
        {
            if (FailWrites) throw new IOException("Could not write the AP record.");
            base.SetItem(index, item);
        }
    }
}
