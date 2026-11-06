using GraveyardKeepelago.GameModifications;
using GraveyardKeepelago.Progression;
using KaitoKid.ArchipelagoUtilities.Net.Client;

namespace GraveyardKeepelago.Tests;

public class PhysicalItemDeliveryTests
{
    [Fact]
    public void InventorySuccessDoesNotCreateAnAdditionalGroundItem()
    {
        PhysicalItemDelivery.Deliver(true, () => true,
            () => { Assert.Fail("Already delivered to the inventory"); return false; });
    }

    [Fact]
    public void FullInventoryFallsBackToASuccessfulNativeGroundDrop()
    {
        var dropped = 0;
        PhysicalItemDelivery.Deliver(true, () => false, () => { dropped++; return true; });
        Assert.Equal(1, dropped);
    }

    [Fact]
    public void NativeDropOnlyItemsKeepTheirExistingDeliveryPath()
    {
        var dropped = 0;
        PhysicalItemDelivery.Deliver(false,
            () => { Assert.Fail("Native drop-only item was inserted into inventory"); return false; },
            () => { dropped++; return true; });
        Assert.Equal(1, dropped);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SuccessfulAndFailedDeliveryRestoreThePreviousDroppingScope(bool previous)
    {
        var original = PlayerUtilities.IsDroppingItems;
        try
        {
            PlayerUtilities.IsDroppingItems = previous;
            PhysicalItemDelivery.Deliver(true, () => true, () => false);
            Assert.Equal(previous, PlayerUtilities.IsDroppingItems);
            Assert.Throws<IOException>(() => PhysicalItemDelivery.Deliver(true, () => false, () => false));
            Assert.Equal(previous, PlayerUtilities.IsDroppingItems);
            Assert.Throws<InvalidOperationException>(() =>
                PhysicalItemDelivery.Deliver(true, () => throw new InvalidOperationException(), () => true));
            Assert.Equal(previous, PlayerUtilities.IsDroppingItems);
        }
        finally { PlayerUtilities.IsDroppingItems = original; }
    }

    [Fact]
    public void FailedDropDoesNotConfirmTheReceiptAndReconnectCanDeliverItOnce()
    {
        var progress = new SaveProgress();
        var stores = 0;
        var delivered = 0;
        var ledger = new ItemReceiptLedger(progress, () => stores++);
        var history = new[] { new ReceivedItem("Location", "Quest item", "Sender", 100, 10, 1, 0) };
        Assert.Throws<IOException>(() => ledger.ApplyHistory(history, _ =>
        {
            PhysicalItemDelivery.Deliver(true, () => false, () => false);
            return true;
        }));
        Assert.Empty(progress.Receipts);
        Assert.Equal(0, stores);
        ledger.ApplyHistory(history, _ =>
        {
            PhysicalItemDelivery.Deliver(true, () => false, () => { delivered++; return true; });
            return true;
        });
        ledger.ApplyHistory(history, _ => { Assert.Fail("Confirmed receipt replayed"); return true; });
        Assert.Single(progress.Receipts);
        Assert.Equal(1, stores);
        Assert.Equal(1, delivered);
    }
}
