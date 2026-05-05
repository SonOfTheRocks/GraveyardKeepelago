using System;
using System.Collections.Generic;
using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.GameModifications;
using GraveyardKeepelago.Items;
using GraveyardKeepelago.Items.Traps;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Tests.TestData;
using KaitoKid.ArchipelagoUtilities.Net;
using KaitoKid.ArchipelagoUtilities.Net.Client;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace GraveyardKeepelago.Tests;

public class ItemParserRoutingTestsFixture : IDisposable
{
    private FakePlayerActions _playerActions;

    public FakeLogger Logger { get; }
    public FakePlayerActions PlayerActions => _playerActions;
    public GKItemManager ItemManager { get; }
    public ITrapManager TrapManager { get; private set; }
    public ItemParser Parser { get; }

    public ItemParserRoutingTestsFixture()
    {
        _playerActions = new FakePlayerActions();
        Logger = new FakeLogger();
        ItemManager = new GKItemManager(Logger);
        ItemManager.SetPlayerActions(_playerActions);
        TrapManager = new TrapManagerStub();
        Parser = new ItemParser(Logger, ItemManager, TrapManager);
    }

    public void Dispose()
    {
    }

    public ReceivedItem CreateReceivedItem(string itemName)
    {
        return new ReceivedItem("dummy", "dummy", itemName, 0L, 0L, 0L, 0);
    }
}

public class ItemParserRoutingTests : IClassFixture<ItemParserRoutingTestsFixture>
{
    private readonly ItemParserRoutingTestsFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ItemParserRoutingTests(ItemParserRoutingTestsFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    #region Buff Tests

    [Theory]
    [MemberData(nameof(ItemData.BuffTestCases), MemberType = typeof(ItemData))]
    public void ProcessItem_Buff_RoutesCorrectly(string itemName, string expectedBuffId, string expectedResTypes, string expectedIcon)
    {
        // Act
        _fixture.Parser.ProcessItem(_fixture.CreateReceivedItem(itemName));

        // Assert
        Assert.Equal(expectedBuffId, _fixture.PlayerActions.LastAppliedBuffId);
        Assert.Empty(_fixture.Logger.ErrorLogs);
    }

    #endregion

    #region Recipe Tests

    [Theory]
    [MemberData(nameof(ItemData.RecipeTestCases), MemberType = typeof(ItemData))]
    public void ProcessItem_Recipe_RoutesCorrectly(string itemName, string expectedRecipeId, string expectedType)
    {
        // Act
        _fixture.Parser.ProcessItem(_fixture.CreateReceivedItem(itemName));

        // Assert
        Assert.NotNull(_fixture.PlayerActions.LastUnlocks);
        Assert.NotEmpty(_fixture.PlayerActions.LastUnlocks);
        Assert.Equal(expectedType == "ProgressiveCraft" ? "ProgressiveCraft" : "Craft",
            _fixture.PlayerActions.LastUnlocks[0].Item1.ToString());
        Assert.Empty(_fixture.Logger.ErrorLogs);
    }

    #endregion

    #region Perk Tests

    [Theory]
    [MemberData(nameof(ItemData.PerkTestCases), MemberType = typeof(ItemData))]
    public void ProcessItem_Perk_RoutesCorrectly(string itemName, string expectedPerkId, string[] expectedRecipes, string[] expectedWorks)
    {
        // Act
        _fixture.Parser.ProcessItem(_fixture.CreateReceivedItem(itemName));

        // Assert
        Assert.NotNull(_fixture.PlayerActions.LastUnlocks);
        Assert.NotEmpty(_fixture.PlayerActions.LastUnlocks);

        // First unlock should always be the Perk type
        Assert.Equal(PlayerUtilities.UnlockType.Perk, _fixture.PlayerActions.LastUnlocks[0].Item1);
        Assert.Equal(expectedPerkId, _fixture.PlayerActions.LastUnlocks[0].Item2);

        // Remaining unlocks should be Crafts or Works
        var remaining = _fixture.PlayerActions.LastUnlocks.Skip(1).ToList();
        if (expectedRecipes != null)
        {
            Assert.Equal(expectedRecipes.Length, remaining.Count(c => c.Item1 == PlayerUtilities.UnlockType.Craft));
        }
        if (expectedWorks != null)
        {
            Assert.Equal(expectedWorks.Length, remaining.Count(c => c.Item1 == PlayerUtilities.UnlockType.Work));
        }
        Assert.Empty(_fixture.Logger.ErrorLogs);
    }

    #endregion

    #region Work Tests

    [Theory]
    [MemberData(nameof(ItemData.WorkTestCases), MemberType = typeof(ItemData))]
    public void ProcessItem_Work_RoutesCorrectly(string itemName, string expectedWorkId, string expectedPerk)
    {
        // Act
        _fixture.Parser.ProcessItem(_fixture.CreateReceivedItem(itemName));

        // Assert
        Assert.NotNull(_fixture.PlayerActions.LastUnlocks);
        Assert.NotEmpty(_fixture.PlayerActions.LastUnlocks);

        // For progressive work, first unlock should be ProgressiveWork type
        if (expectedPerk == "ProgressiveWork")
        {
            Assert.Equal("ProgressiveWork", _fixture.PlayerActions.LastUnlocks[0].Item1.ToString());
        }
        else
        {
            Assert.Equal(PlayerUtilities.UnlockType.Work, _fixture.PlayerActions.LastUnlocks[0].Item1);
            Assert.Equal(expectedWorkId, _fixture.PlayerActions.LastUnlocks[0].Item2);
        }
        Assert.Empty(_fixture.Logger.ErrorLogs);
    }

    #endregion

    #region Relation Tests

    [Theory]
    [MemberData(nameof(ItemData.RelationTestCases), MemberType = typeof(ItemData))]
    public void ProcessItem_Relation_RoutesCorrectly(string npcName, string expectedNpcId, int expectedAmount)
    {
        // Arrange
        var fullItemName = npcName + " - Happiness +10";

        // Act
        _fixture.Parser.ProcessItem(_fixture.CreateReceivedItem(fullItemName));

        // Assert
        Assert.Equal(expectedNpcId, _fixture.PlayerActions.LastRelationNpcId);
        Assert.Equal(expectedAmount, _fixture.PlayerActions.LastRelationAmount);
        Assert.Empty(_fixture.Logger.ErrorLogs);
    }

    #endregion

    #region IngameItem Tests

    [Theory]
    [MemberData(nameof(ItemData.IngameItemTestCases), MemberType = typeof(ItemData))]
    public void ProcessItem_IngameItem_RoutesCorrectly(string itemName, string expectedItemId)
    {
        // Act
        _fixture.Parser.ProcessItem(_fixture.CreateReceivedItem(itemName));

        // Assert
        Assert.Equal(expectedItemId, _fixture.PlayerActions.LastDropItemId);
        Assert.Equal(1, _fixture.PlayerActions.LastDropItemAmount);
        Assert.Empty(_fixture.Logger.ErrorLogs);
    }

    #endregion

    #region Trap Tests

    [Fact]
    public void ProcessItem_Trap_RoutesToTrapManager()
    {
        // Arrange
        ((TrapManagerStub)_fixture.TrapManager).SetTrap("Monsters Trap", true);

        // Act
        _fixture.Parser.ProcessItem(_fixture.CreateReceivedItem("Monsters Trap"));

        // Assert
        Assert.True(((TrapManagerStub)_fixture.TrapManager).WasTrapExecuted("Monsters Trap"));
        Assert.Null(_fixture.PlayerActions.LastAppliedBuffId);
        Assert.Empty(_fixture.Logger.ErrorLogs);
    }

    #endregion

    #region Unknown Item Tests

    [Theory]
    [MemberData(nameof(ItemData.IngameItemTestCases), MemberType = typeof(ItemData))]
    public void ProcessItem_UnknownItem_LogsError(string itemName, string _)
    {
        // Act
        _fixture.Parser.ProcessItem(_fixture.CreateReceivedItem(itemName));

        // Assert
        Assert.Single(_fixture.Logger.ErrorLogs);
        Assert.Contains($"Could not process item {itemName}", _fixture.Logger.ErrorLogs[0]);
    }

    #endregion
}
