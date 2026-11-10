using GraveyardKeepelago.GameModifications;
using GraveyardKeepelago.Items;
using GraveyardKeepelago.Items.Traps;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Tests.TestData;
using KaitoKid.ArchipelagoUtilities.Net.Client;

namespace GraveyardKeepelago.Tests;

public class ItemParserProgressiveTestsFixture : IDisposable
{
    private FakePlayerActions _playerActions;

    public FakeLogger Logger { get; }
    public FakePlayerActions PlayerActions => _playerActions;
    public GKItemRegistry Registry { get; }
    public ITrapManager TrapManager { get; private set; }
    public ItemProcessor Processor { get; }

    public ItemParserProgressiveTestsFixture()
    {
        _playerActions = new FakePlayerActions();
        Logger = new FakeLogger();
        
        var registry = new GKItemRegistry();
        var factory = new GKItemFactory(_playerActions);
        factory.BuildAll(registry);
        Registry = registry;
        
        TrapManager = new TrapManagerStub();
        Processor = new ItemProcessor(Logger, Registry, TrapManager);
    }

    public void Dispose()
    {
    }

    public ReceivedItem CreateReceivedItem(string itemName)
    {
        return new ReceivedItem("dummy", itemName, "dummy", 0L, 0L, 0L, 0);
    }
}

public class ItemParserProgressiveTests : IClassFixture<ItemParserProgressiveTestsFixture>
{
    private readonly ItemParserProgressiveTestsFixture _fixture;

    public ItemParserProgressiveTests(ItemParserProgressiveTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void ProcessItem_ProgressiveRecipe_ApplyIncrementsStage()
    {
        var progressiveIDs = new List<List<string>>
        {
            new() { "morgue_builddesk:p:corpse_bed_place" },
            new() { "morgue_builddesk:p:corpse_bed_big_place" },
            new() { "morgue_builddesk:p:corpse_fridge_place" },
        };
        var progressiveItem = new GKProgressiveRecipe(progressiveIDs, _fixture.PlayerActions);

        var registry = new GKItemRegistry();
        registry.Add("Test progressive recipe", progressiveItem);
        var processor = new ItemProcessor(_fixture.Logger, registry, _fixture.TrapManager);

        // Act - first Apply
        processor.ProcessItem(_fixture.CreateReceivedItem("Test progressive recipe"));

        // Assert - stage 0
        Assert.NotNull(_fixture.PlayerActions.LastUnlocks);
        Assert.NotEmpty(_fixture.PlayerActions.LastUnlocks);
        Assert.Equal(PlayerUtilities.UnlockType.Craft, _fixture.PlayerActions.LastUnlocks[0].Item1);
        Assert.Equal("morgue_builddesk:p:corpse_bed_place", _fixture.PlayerActions.LastUnlocks[0].Item2);

        // Act - second Apply (simulating another item received)
        _fixture.PlayerActions.Clear();
        processor.ProcessItem(_fixture.CreateReceivedItem("Test progressive recipe"));

        // Assert - stage 1
        Assert.NotNull(_fixture.PlayerActions.LastUnlocks);
        Assert.Equal(PlayerUtilities.UnlockType.Craft, _fixture.PlayerActions.LastUnlocks[0].Item1);
        Assert.Equal("morgue_builddesk:p:corpse_bed_big_place", _fixture.PlayerActions.LastUnlocks[0].Item2);

        // Act - third Apply
        _fixture.PlayerActions.Clear();
        processor.ProcessItem(_fixture.CreateReceivedItem("Test progressive recipe"));

        // Assert - stage 2
        Assert.NotNull(_fixture.PlayerActions.LastUnlocks);
        Assert.Equal(PlayerUtilities.UnlockType.Craft, _fixture.PlayerActions.LastUnlocks[0].Item1);
        Assert.Equal("morgue_builddesk:p:corpse_fridge_place", _fixture.PlayerActions.LastUnlocks[0].Item2);
    }

    [Fact]
    public void ProcessItem_ProgressiveWork_ApplyIncrementsStage()
    {
        // Arrange - Progressive Tree felling: stage 0 = "t_wood_small", stage 1 = "t_wood_big"
        var progressiveIDs = new List<List<string>>
        {
            new() { "t_wood_small" },
            new() { "t_wood_big" },
        };
        var progressiveItem = new GKProgressiveWork(progressiveIDs, _fixture.PlayerActions);

        var registry = new GKItemRegistry();
        registry.Add("Test progressive work", progressiveItem);
        var processor = new ItemProcessor(_fixture.Logger, registry, _fixture.TrapManager);

        // Act - first Apply
        processor.ProcessItem(_fixture.CreateReceivedItem("Test progressive work"));

        // Assert - stage 0
        Assert.NotNull(_fixture.PlayerActions.LastUnlocks);
        Assert.NotEmpty(_fixture.PlayerActions.LastUnlocks);
        Assert.Equal(PlayerUtilities.UnlockType.Work, _fixture.PlayerActions.LastUnlocks[0].Item1);
        Assert.Equal("t_wood_small", _fixture.PlayerActions.LastUnlocks[0].Item2);

        // Act - second Apply
        _fixture.PlayerActions.Clear();
        processor.ProcessItem(_fixture.CreateReceivedItem("Test progressive work"));

        // Assert - stage 1
        Assert.NotNull(_fixture.PlayerActions.LastUnlocks);
        Assert.Equal(PlayerUtilities.UnlockType.Work, _fixture.PlayerActions.LastUnlocks[0].Item1);
        Assert.Equal("t_wood_big", _fixture.PlayerActions.LastUnlocks[0].Item2);
    }
}
