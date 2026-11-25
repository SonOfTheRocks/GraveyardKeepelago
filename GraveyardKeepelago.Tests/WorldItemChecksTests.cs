using System.Reflection;
using System.Runtime.CompilerServices;
using GraveyardKeepelago.Items;
using GraveyardKeepelago.Locations;
using GraveyardKeepelago.Logic;

namespace GraveyardKeepelago.Tests;

public sealed class WorldItemChecksTests
{
    public static IEnumerable<object[]> Rewards()
    {
        var contract = ClientContractLoader.LoadDefault();
        return contract.Locations.Values.SelectMany(location => location.Triggers
            .Where(trigger => trigger.Type == "world_item_reward")
            .Select(trigger => new object[] { location.Name, trigger.ItemID }));
    }

    private static readonly (string Seed, int Team, int Slot) Identity = ("world-rewards", 0, 1);

    [Theory]
    [MemberData(nameof(Rewards))]
    public void EachWorldRewardRecordsBeforeConsumptionAndDeduplicates(string name, string id)
    {
        var active = new Dictionary<string, long> { [name] = 42 };
        var phrases = new List<string>();
        var bound = SaveBoundLocationChecks.Bind(phrases, Identity, active, true);
        var sent = new List<long[]>();
        bound.Connect(Identity, sent.Add);
        var checks = Create(bound, active.Keys);
        var consumed = 0;
        void Consume()
        {
            Assert.Contains(name, bound.Progress.Data.Checks);
            Assert.Single(phrases);
            consumed++;
        }
        Assert.True(WorldItemChecks.TryReplace(checks, id, 1, true, false, Consume));
        Assert.True(WorldItemChecks.TryReplace(checks, id, 1, true, false, Consume));
        Assert.Equal(2, consumed); // Any repeated native copy is consumed, but no second check is sent.
        Assert.Equal([42L], Assert.Single(sent));
    }

    [Theory]
    [MemberData(nameof(Rewards))]
    public void InactiveRewardsAndOtherItemsKeepNativeBehavior(string name, string id)
    {
        var bound = SaveBoundLocationChecks.Bind(new List<string>(), Identity, new Dictionary<string, long>(), true);
        var inactive = Create(bound, []);
        Assert.False(WorldItemChecks.TryReplace(inactive, id, 1, true, false, () => throw new Exception()));
        Assert.False(WorldItemChecks.TryReplace(null, id, 1, true, false, () => throw new Exception()));
        var active = Create(bound, [name]);
        Assert.False(WorldItemChecks.TryReplace(active, "wood", 1, true, false, () => throw new Exception()));
        Assert.False(WorldItemChecks.TryReplace(active, id.ToUpperInvariant(), 1, true, false, () => throw new Exception()));
        Assert.Empty(bound.Progress.Data.Checks);
    }

    [Theory]
    [InlineData(false, false, 1)] // No successful acquisition (e.g. rejected chest transfer, other receiver).
    [InlineData(true, true, 1)] // A received item taken back out of storage.
    [InlineData(true, false, 0)]
    [InlineData(true, false, -1)]
    public void OnlyARealWorldAcquisitionCanClaimTheCheck(bool acquisition, bool received, int count)
    {
        var (name, id) = FirstReward();
        var bound = SaveBoundLocationChecks.Bind(new List<string>(), Identity, new Dictionary<string, long> { [name] = 1 }, true);
        Assert.False(WorldItemChecks.TryReplace(Create(bound, [name]), id, count, acquisition, received,
            () => throw new Exception("Must not consume")));
        Assert.Empty(bound.Progress.Data.Checks);
    }

    [Fact]
    public void ItemApplicationScopeNeverConsumesARewardAndUnwindsAfterFailure()
    {
        var (name, id) = FirstReward();
        var bound = SaveBoundLocationChecks.Bind(new List<string>(), Identity, new Dictionary<string, long> { [name] = 1 }, true);
        var checks = Create(bound, [name]);
        Assert.Throws<IOException>(() => ItemProcessor.ApplyItem(() =>
        {
            Assert.False(WorldItemChecks.TryReplace(checks, id, 1, true, false, () => throw new Exception()));
            throw new IOException("grant failed");
        }));
        Assert.Empty(bound.Progress.Data.Checks);
        Assert.True(WorldItemChecks.TryReplace(checks, id, 1, true, false, () => { }));
    }

    [Fact]
    public void PersistenceFailureLeavesTheWorldSourceUntouched()
    {
        var (name, id) = FirstReward();
        var phrases = new List<string>();
        var bound = SaveBoundLocationChecks.Bind(phrases, Identity, new Dictionary<string, long> { [name] = 1 }, true);
        var checks = Create(bound, [name]);
        var loaded = SaveBoundLocationChecks.Bind(phrases.AsReadOnly(), Identity, new Dictionary<string, long> { [name] = 1 }, false);
        Assert.Throws<NotSupportedException>(() => WorldItemChecks.TryReplace(Create(loaded, [name]), id, 1, true, false,
            () => throw new Exception("Must not consume before storage")));
        Assert.Empty(loaded.Progress.Data.Checks);
        Assert.True(WorldItemChecks.TryReplace(checks, id, 1, true, false, () => { }));
    }

    [Fact]
    public void OfflineRewardSurvivesReloadAndCannotMoveToAnotherSlot()
    {
        var (name, id) = FirstReward();
        var active = new Dictionary<string, long> { [name] = 88 };
        var phrases = new List<string>();
        var bound = SaveBoundLocationChecks.Bind(phrases, Identity, active, true);
        Assert.True(WorldItemChecks.TryReplace(Create(bound, [name]), id, 1, true, false, () => { }));
        var saved = phrases.ToList();
        Assert.Throws<InvalidDataException>(() => SaveBoundLocationChecks.Bind(saved, (Identity.Seed, 0, 2), active, false));
        var loaded = SaveBoundLocationChecks.Bind(saved, Identity, active, false);
        var sent = new List<long[]>();
        loaded.Connect(Identity, sent.Add);
        Assert.True(WorldItemChecks.TryReplace(Create(loaded, [name]), id, 1, true, false, () => { }));
        Assert.Equal([88L], Assert.Single(sent));
    }

    [Fact]
    public void PlayerInventoryOwnershipUsesIdentityAndIncludesNestedBags()
    {
        var player = BareItem();
        var bag = BareItem();
        var nested = BareItem();
        player.inventory.Add(bag);
        bag.inventory.Add(nested);
        Assert.True(WorldItemChecks.ContainsItem(player, player));
        Assert.True(WorldItemChecks.ContainsItem(player, bag));
        Assert.True(WorldItemChecks.ContainsItem(player, nested));
        Assert.False(WorldItemChecks.ContainsItem(player, BareItem()));
        Assert.False(WorldItemChecks.ContainsItem(null!, player));
        Assert.False(WorldItemChecks.ContainsItem(player, null!));
    }

    [Fact]
    public void ReceivedMarkerLivesInNativeParametersAndDoesNotDependOnApplicationScope()
    {
        var item = BareItem();
        Assert.False(ReceivedItemProvenance.IsReceived(item));
        ItemProcessor.ApplyItem(() => ReceivedItemProvenance.Mark(item));
        Assert.True(ReceivedItemProvenance.IsReceived(item));
        Assert.False(ItemProcessor.IsApplyingItem);
        var reloaded = BareItem();
        typeof(Item).GetField("_params", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(reloaded, new GameRes(item.GetParams()));
        Assert.True(ReceivedItemProvenance.IsReceived(reloaded));
        Assert.False(ReceivedItemProvenance.IsReceived(BareItem()));
        Assert.False(ReceivedItemProvenance.IsReceived(null!));
        Assert.True(ReceivedItemProvenance.IsReceived(new Item(item)));
    }

    [Fact]
    public void ConsumingAWorldCopyKeepsTheReceivedCopyOfTheSameID()
    {
        var world = BareItem();
        var received = BareItem();
        ReceivedItemProvenance.Mark(received);
        var chest = BareItem();
        chest.inventory.Add(received);
        chest.inventory.Add(world);
        var source = new MultiInventory(new Inventory(chest));
        Assert.True(source.TryRemoveSpecificItemNoCheck(world));
        Assert.Same(received, Assert.Single(chest.inventory));
        Assert.True(ReceivedItemProvenance.IsReceived(received));
    }

    private static Item BareItem()
    {
        // Avoid Unity/GameBalance initialization. Exercise the real native parameter storage.
        var item = (Item)RuntimeHelpers.GetUninitializedObject(typeof(Item));
        item.inventory = [];
        item.id = "empty";
        typeof(Item).GetField("_params", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(item, new GameRes());
        return item;
    }

    private static (string Name, string ID) FirstReward()
    {
        var row = Rewards().First();
        return ((string)row[0], (string)row[1]);
    }

    private static LocationTriggerService Create(ILocationChecker recorder, IEnumerable<string> active)
        => new(recorder, new LocationTriggerRegistry(ClientContractLoader.LoadDefault(), active));
}
