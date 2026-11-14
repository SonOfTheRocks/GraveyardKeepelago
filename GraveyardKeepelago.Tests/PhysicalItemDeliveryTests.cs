using GraveyardKeepelago.GameModifications;

namespace GraveyardKeepelago.Tests;

public sealed class PhysicalItemDeliveryTests
{
    [Theory]
    [InlineData(true, true, 1, 0)]
    [InlineData(true, false, 1, 1)]
    [InlineData(false, false, 0, 1)]
    public void DeliversToInventoryOrFallsBackToANativeDrop(bool inventoryAllowed, bool inventoryAccepts,
        int expectedInventoryAttempts, int expectedDropAttempts)
    {
        var inventoryAttempts = 0;
        var dropAttempts = 0;

        PhysicalItemDelivery.Deliver("test", inventoryAllowed, () =>
        {
            Assert.True(PlayerUtilities.IsDroppingItems);
            inventoryAttempts++;
            return inventoryAccepts;
        }, () =>
        {
            Assert.True(PlayerUtilities.IsDroppingItems);
            dropAttempts++;
            return true;
        });

        Assert.Equal(expectedInventoryAttempts, inventoryAttempts);
        Assert.Equal(expectedDropAttempts, dropAttempts);
        Assert.False(PlayerUtilities.IsDroppingItems);
    }

    [Fact]
    public void FailedDropThrowsAndRestoresDroppingState()
    {
        var error = Assert.Throws<IOException>(() =>
            PhysicalItemDelivery.Deliver("test", true, () => false, () => false));

        Assert.Contains("'test'", error.Message);
        Assert.False(PlayerUtilities.IsDroppingItems);
    }
}
