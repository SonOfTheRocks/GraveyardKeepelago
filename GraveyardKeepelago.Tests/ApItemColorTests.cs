using Archipelago.MultiClient.Net.DataPackage;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.GameModifications.UI;
using GraveyardKeepelago.Items.Notifications;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Technologies;
using KaitoKid.ArchipelagoUtilities.Net.Client;
using Moq;

namespace GraveyardKeepelago.Tests;

public class ApItemColorTests
{
    [Theory]
    [InlineData(ItemFlags.None, "00EEEE")]
    [InlineData(ItemFlags.NeverExclude, "6D8BE8")]
    [InlineData(ItemFlags.Advancement, "AF99EF")]
    public void SameRecipeUsesTheGeneratedSeedsFlags(ItemFlags flags, string hex)
    {
        const string name = "Create: Roof tile";
        var received = new ReceivedItem("Check", name, "Friend", locationId: 200, itemId: 100,
            playerId: 2, uniqueId: 0);
        var notification = ItemNotification.Create(name,
            new ClientContractGrant { Type = "craft_unlock", CraftIDs = new List<string> { "test_recipe" } },
            null!, received, 1, ApItemPresentation.ReceivedFlags(received, new[] { NetworkReceipt(flags) }));
        Assert.Equal($"[c][{hex}]{name}[-][/c]", notification.DisplayName);
    }

    [Theory]
    [InlineData(0, "00EEEE", "00EEEE")]
    [InlineData(1, "AF99EF", "79439C")]
    [InlineData(2, "6D8BE8", "6D8BE8")]
    [InlineData(4, "FA8072", "FA8072")]
    [InlineData(3, "AF99EF", "79439C")]
    [InlineData(5, "AF99EF", "79439C")]
    [InlineData(6, "6D8BE8", "6D8BE8")]
    [InlineData(7, "AF99EF", "79439C")]
    [InlineData(8, "00EEEE", "00EEEE")]
    public void ReceiptAndForeignGamePreviewUseTheirBackgroundPaletteAndApFlagPrecedence(int bits, string hex, string previewHex)
    {
        var flags = (ItemFlags)bits;
        var received = new ReceivedItem("Check", "AP item", "Friend", locationId: 200, itemId: 100, playerId: 2, uniqueId: 0);
        var history = new[] { NetworkReceipt(flags) };
        Assert.Equal(100, history[0].ItemId);
        Assert.Equal(200, history[0].LocationId);
        Assert.Equal(2, history[0].Player.Slot);
        var notification = ItemNotification.Create(received.ItemName,
            new ClientContractGrant { Type = "research_points", Color = "red" }, null!, received, 1,
            ApItemPresentation.ReceivedFlags(received, history));
        var preview = new RewardPreview { Item = "AP item", Game = "Another game", Flags = flags };
        var expected = $"[c][{hex}]AP item[-][/c]";
        Assert.Equal(expected, notification.DisplayName);
        Assert.Equal(expected, notification.DisplayEntryName("research_points", "red"));
        Assert.Equal($"[c][{previewHex}]AP item[-][/c]", RewardPreviewPresentation.Name(preview));
        Assert.Equal("Friend sent you:", notification.SourceText);
        Assert.Equal("AP item", notification.Name); // Identity stays free of UI markup.
        history[0] = NetworkReceipt(ItemFlags.None);
        Assert.Equal(expected, notification.DisplayName); // Queue captured the original flags.
    }

    [Fact]
    public void UnknownTechnologyRewardStaysNeutralInsteadOfAssumingProgression()
    {
        var preview = new RewardPreview { Item = "Unknown classification" };
        Assert.Equal("Unknown classification", RewardPreviewPresentation.Name(preview));
        Assert.Equal("Archipelago …", RewardPreviewPresentation.Name(null!));
    }

    [Fact]
    public void RelationAndNativeRecipeNamesKeepTheirLayoutInsideTheColorScope()
    {
        var relation = ItemNotification.Create("Merchant - Happiness +10",
            new ClientContractGrant { Type = "relation", NpcID = "npc_merchant", Amount = 10 }, null!,
            flags: ItemFlags.NeverExclude);
        Assert.Equal("[c][6D8BE8]Merchant\nHappiness +10[-][/c]",
            relation.DisplayEntryName("relation", "Merchant"));
        Assert.Equal("[c][6D8BE8]Stone fence[-][/c]", relation.DisplayEntryName("craft", "Stone fence"));
    }

    [Theory]
    [InlineData(-1, 100, 200, 2)]
    [InlineData(1, 100, 200, 2)]
    [InlineData(0, 999, 200, 2)]
    [InlineData(0, 100, 999, 2)]
    [InlineData(0, 100, 200, 3)]
    public void MissingOrMismatchedNetworkReceiptDoesNotGuessAColor(int index, long item, long location, long sender)
    {
        var received = new ReceivedItem("Check", "AP item", "Friend", locationId: location, itemId: item,
            playerId: sender, uniqueId: index);
        var flags = ApItemPresentation.ReceivedFlags(received, new[] { NetworkReceipt(ItemFlags.Advancement) });
        Assert.Null(flags);
        Assert.Equal("AP item", ItemNotification.Create("AP item", null!, null!, flags: flags).DisplayName);
    }

    [Fact]
    public async Task ScoutingRetainsServerFlagsThroughThePreviewCacheWithoutPublishingHints()
    {
        var player = new PlayerInfo(0, 2, "Friend", "Alias", "Another game", Array.Empty<NetworkSlot>(), Array.Empty<int>());
        var players = new Mock<IPlayerHelper>();
        players.SetupGet(value => value.ActivePlayer).Returns(player);
        players.Setup(value => value.GetPlayerInfo(It.IsAny<int>())).Returns(player);
        players.Setup(value => value.GetPlayerInfo(It.IsAny<int>(), It.IsAny<int>())).Returns(player);
        var resolver = new Mock<IItemInfoResolver>();
        resolver.Setup(value => value.GetItemName(100, "Another game")).Returns("Foreign reward");
        resolver.Setup(value => value.GetLocationName(200, "Graveyard Keeper")).Returns("Check");
        var scouted = new ScoutedItemInfo(new NetworkItem
            { Item = 100, Location = 200, Player = 2, Flags = ItemFlags.Trap },
            "Another game", "Graveyard Keeper", resolver.Object, players.Object, player);
        var helper = new Mock<ILocationCheckHelper>(MockBehavior.Strict);
        helper.Setup(value => value.ScoutLocationsAsync(HintCreationPolicy.None,
                It.Is<long[]>(ids => ids.SequenceEqual(new long[] { 200 }))))
            .ReturnsAsync(new Dictionary<long, ScoutedItemInfo> { [200] = scouted });
        var source = new ServerRewardPreviewSource(() => helper.Object,
            () => new Dictionary<string, long> { ["Check"] = 200 });
        var cache = new RewardPreviewCache(source);
        var result = await cache.FetchAsync(new[] { "Check" });
        Assert.Null(cache.Get("Check"));
        cache.Accept(result);
        var preview = cache.Get("Check");
        Assert.Equal(ItemFlags.Trap, preview.Flags);
        Assert.Equal("Foreign reward", preview.Item);
        Assert.Equal("[c][FA8072]Foreign reward[-][/c]", RewardPreviewPresentation.Name(preview));
        Assert.Equal("Alias · Another game\nCheck", RewardPreviewPresentation.Description(preview, "Check"));
        helper.VerifyAll();
        helper.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void HudAndJournalUseTheSameItemColorIncludingCombinedServerFlags(int bits)
    {
        var flags = (ItemFlags)bits;
        var name = "Create: [example]";
        var hud = new ItemNotification(name, Array.Empty<(string, string)>(), flags: flags);
        var journal = new GraveyardKeepelago.Journal.JournalEntry
        {
            Parts = new List<GraveyardKeepelago.Journal.JournalPart>
            {
                new() { Text = name, Role = GraveyardKeepelago.Journal.JournalRole.Item, Flags = flags },
            },
        };
        Assert.EndsWith("\n" + journal.FormatBody(lightBackground: false), hud.HudText);
    }
    private static ItemInfo NetworkReceipt(ItemFlags flags) => new(
        new NetworkItem { Item = 100, Location = 200, Player = 2, Flags = flags },
        "Graveyard Keeper", "Graveyard Keeper", Mock.Of<IItemInfoResolver>(),
        new PlayerInfo(0, 2, "Friend", "Friend", "Graveyard Keeper", Array.Empty<NetworkSlot>(), Array.Empty<int>()));
}

