using Archipelago.MultiClient.Net.Models;
using GraveyardKeepelago.Archipelago;

namespace GraveyardKeepelago.Tests;

public sealed class ReceivedItemHistoryTests
{
    [Fact]
    public void InitialBatchAndRepeatedOverlapsProduceAnOrderedSnapshot()
    {
        var history = new ReceivedItemHistory();
        var first = Item(10);
        var second = Item(20);

        history.Receive(0, [first, first]);
        var initial = Assert.IsType<NetworkItem[]>(history.Snapshot(out _));
        history.Receive(1, [first, second]);
        history.Receive(2, [second]);

        Assert.Equal(new[] { first, first }, initial);
        Assert.Equal(new[] { first, first, second }, history.Snapshot(out _));
    }

    [Fact]
    public void MissedInitialBatchOrGapRequiresFullResynchronization()
    {
        var history = new ReceivedItemHistory();
        var item = Item(10);
        history.Receive(2, [item]);
        Assert.Null(history.Snapshot(out var initialSync));

        history.Receive(0, [item]);
        history.Receive(2, [item]);
        history.Receive(1, [item]);
        Assert.Null(history.Snapshot(out var gapSync));
        Assert.NotEqual(initialSync, gapSync);

        history.Receive(0, [item, item, item]);
        Assert.Equal(3, history.Snapshot(out _)!.Count);
    }

    [Fact]
    public void ConflictingOverlapInvalidatesTheSnapshotUntilAServerReplay()
    {
        var history = new ReceivedItemHistory();
        history.Receive(0, [Item(1), Item(2)]);
        history.Receive(1, [Item(3)]);
        Assert.Null(history.Snapshot(out _));

        history.Receive(0, [Item(1), Item(3)]);
        Assert.Equal(new long[] { 1, 3 }, history.Snapshot(out _)!.Select(item => item.Item));
        Assert.Null(new ReceivedItemHistory().Snapshot(out _));
    }

    internal static NetworkItem Item(long id, long location = -2, int player = 1) =>
        new() { Item = id, Location = location, Player = player };
}
