using Archipelago.MultiClient.Net.Models;
using GraveyardKeepelago.Archipelago;

namespace GraveyardKeepelago.Tests;

public sealed class ReceivedItemHistoryTests
{
    [Fact]
    public void InitialBatchAndConsecutiveAppendPreserveTheServerOrder()
    {
        var history = new ReceivedItemHistory();
        var first = Item(10);
        var second = Item(20);

        history.Receive(0, [first, first]);
        var initial = Assert.IsType<NetworkItem[]>(history.Snapshot);
        history.Receive(2, [second]);

        Assert.Equal(new[] { first, first }, initial);
        Assert.Equal(new[] { first, first, second }, history.Snapshot);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void DuplicateOrMissingIndexWaitsForFullResynchronization(int index)
    {
        var history = new ReceivedItemHistory();
        var item = Item(10);
        history.Receive(2, [item]);
        Assert.Null(history.Snapshot);

        history.Receive(0, [item, item]);
        history.Receive(index, [item]);
        history.Receive(2, [item]);
        Assert.Null(history.Snapshot);

        history.Receive(0, [item, item, item]);
        Assert.Equal(3, history.Snapshot!.Count);
    }

    [Fact]
    public void IndexZeroReplacesThePreviousHistoryEvenWhenEmpty()
    {
        var history = new ReceivedItemHistory();
        history.Receive(0, [Item(1), Item(2)]);
        history.Receive(0, []);
        Assert.Empty(history.Snapshot!);

        history.Receive(0, [Item(1), Item(3)]);
        Assert.Equal(new long[] { 1, 3 }, history.Snapshot!.Select(item => item.Item));
    }

    internal static NetworkItem Item(long id, long location = -2, int player = 1) =>
        new() { Item = id, Location = location, Player = player };
}
