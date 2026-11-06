using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.Technologies;
using Moq;

namespace GraveyardKeepelago.Tests;

public class TechnologyTests
{
    [Fact]
    public void CatalogCoversAllTechnologyChecksWithoutDummyRecipeIDs()
    {
        var catalog = TechnologyCatalog.LoadDefault();
        Assert.Equal(184, catalog.Definitions.Count);
        var locations = catalog.Definitions.Values.SelectMany(definition => definition.Locations).ToList();
        Assert.Equal(487, locations.Count);
        Assert.Equal(487, locations.Distinct().Count());
        Assert.DoesNotContain(locations, location => location.StartsWith("ap_"));
        Assert.Empty(catalog.Definitions["First slice"].Locations);
        Assert.Equal(153, catalog.Definitions.Values.Count(value => value.RecipeItem == null && value.QuestReward == null));
        Assert.All(catalog.Definitions.Values.Where(value => value.RecipeItem == null && value.QuestReward == null),
            value => Assert.Equal(3, value.Locations.Length));
    }

    [Fact]
    public void CatalogRejectsDuplicateLocationMappings()
    {
        var definition = new TechnologyDefinition { Locations = new[] { "A", "B", "C" } };
        Assert.Throws<InvalidDataException>(() => new TechnologyCatalog(new()
            { ["First"] = definition, ["Second"] = definition }));
    }

    [Theory]
    [InlineData("ordinary", 2)]
    [InlineData("book", 0)]
    [InlineData("book", 2)]
    [InlineData("quest", 1)]
    public void CatalogRejectsIncorrectResearchCheckCounts(string source, int count)
    {
        var definition = new TechnologyDefinition
        {
            Locations = Enumerable.Range(1, count).Select(index => $"Research {index}").ToArray(),
            RecipeItem = source == "book" ? "recipe" : null!,
            QuestReward = source == "quest" ? "Quest reward" : null!,
        };
        Assert.Throws<InvalidDataException>(() => new TechnologyCatalog(new() { ["Research"] = definition }));
    }

    [Fact]
    public void ResearchKeepsPurchasedStateAndRunsWorldEffectsOnlyOnce()
    {
        var catalog = TechnologyCatalog.LoadDefault();
        var locations = catalog.Definitions["Faith"].Locations;
        var researched = new List<string>();
        var effects = new List<string>();
        var checks = new HashSet<string>();
        var research = new TechnologyResearch(catalog, locations.ToHashSet(), researched,
            id => effects.Add(id), names => checks.UnionWith(names));
        Assert.True(research.Research("Faith"));
        Assert.True(research.Research("Faith"));
        Assert.Equal(new[] { "Faith" }, researched);
        Assert.Equal(new[] { "Faith" }, effects);
        Assert.Equal(locations.Order(), checks.Order());
        Assert.False(research.Research("Persistence")); // Separate quest reward path.
    }

    [Fact]
    public void QuestTechnologyCompletesNativeProgressWithoutResearchChecks()
    {
        var catalog = TechnologyCatalog.LoadDefault();
        var root = catalog.Definitions["First slice"];
        var researched = new List<string>();
        var checks = new List<string>();
        var research = new TechnologyResearch(catalog, root.Locations.ToHashSet(), researched,
            _ => { }, names => checks.AddRange(names));
        var effects = new List<string>();
        research = new TechnologyResearch(catalog, root.Locations.ToHashSet(), researched,
            effects.Add, names => checks.AddRange(names));
        Assert.False(research.Research("First slice"));
        Assert.True(research.Complete("First slice"));
        Assert.True(research.Complete("First slice"));
        Assert.Empty(checks);
        Assert.Equal(new[] { "First slice" }, effects);
        Assert.Equal(new[] { "First slice" }, researched);
    }

    [Fact]
    public void AllRecipeItemsHaveExactlyOneResearchDefinitionAndCheck()
    {
        var catalog = TechnologyCatalog.LoadDefault();
        var books = catalog.Definitions.Where(pair => pair.Value.RecipeItem != null).ToArray();
        Assert.Equal(28, books.Length);
        Assert.Equal(28, books.Select(pair => pair.Value.RecipeItem).Distinct().Count());
        var activeLocations = catalog.Definitions.Values.SelectMany(value => value.Locations).ToHashSet();
        foreach (var (id, definition) in books)
        {
            var checks = new HashSet<string>();
            var effects = new List<string>();
            var researched = new List<string>();
            var research = new TechnologyResearch(catalog, activeLocations, researched,
                effects.Add, checks.UnionWith);
            Assert.True(research.Research(id));
            Assert.True(research.Research(id));
            Assert.Equal(new[] { definition.Locations[0] }, checks);
            Assert.Equal(new[] { id }, effects);
            Assert.Equal(new[] { id }, researched);
            Assert.Single(definition.Locations);
        }
        Assert.Equal("recipe_2min", catalog.Definitions["2min"].RecipeItem);
        Assert.Equal("recipe_bag_alchemy", catalog.Definitions["tech_bag_alchemy"].RecipeItem);
        Assert.Equal("recipe_advanced_gravestones", catalog.Definitions["Advanced gravestones"].RecipeItem);
    }

    [Fact]
    public void ResearchAndPreviewUseTheSameReducedLocationsAfterReload()
    {
        var catalog = TechnologyCatalog.LoadDefault();
        var definition = catalog.Definitions["2min"];
        var active = definition.Locations.ToHashSet();
        var alreadyResearched = new List<string> { "2min" };
        var checks = new List<string>();
        var research = new TechnologyResearch(catalog, active, alreadyResearched,
            _ => throw new Exception("Do not repeat world effects on reload"), checks.AddRange);
        Assert.True(research.Research("2min"));
        Assert.Equal(new[] { "Simple and Tasty - Unlock 1" }, checks);
        Assert.Equal(checks, catalog.GetLocations("2min", active));
    }

    [Fact]
    public void QuestRootsHaveNoExtraResearchChecksForRecoveryOrPreview()
    {
        var catalog = TechnologyCatalog.LoadDefault();
        var all = catalog.Definitions.Values.SelectMany(value => value.Locations).ToHashSet();
        Assert.Equal(487, catalog.Definitions.Keys.SelectMany(id => catalog.GetLocations(id, all)).Count());
        foreach (var id in new[] { "First slice", "Grave plate", "Iron" })
            Assert.Empty(catalog.GetLocations(id, all));
        Assert.Equal(3, catalog.GetLocations("Faith", all).Length);
        Assert.Contains("\u0421omfort of faith", catalog.Definitions.Keys);
    }

    [Fact]
    public void QuestRootPreviewShowsItsRealQuestRewardWithoutAddingResearchChecks()
    {
        var catalog = TechnologyCatalog.LoadDefault();
        var active = catalog.Definitions.Values.SelectMany(value => value.Locations).ToHashSet();
        foreach (var id in new[] { "First slice", "Grave plate", "Iron" })
        {
            var source = catalog.Definitions[id].QuestReward;
            Assert.Empty(catalog.GetPreviewLocations(id, active));
            active.Add(source);
            Assert.Equal(new[] { source }, catalog.GetPreviewLocations(id, active));
            Assert.Empty(catalog.GetLocations(id, active));
        }
        Assert.Equal(catalog.GetLocations("2min", active), catalog.GetPreviewLocations("2min", active));
    }

    [Fact]
    public void DisabledAndMissingLocationsAreNeverCheckedOrPreviewed()
    {
        var catalog = TechnologyCatalog.LoadDefault();
        var effects = 0;
        var checks = new List<string>();
        var research = new TechnologyResearch(catalog, new HashSet<string>(), new List<string>(),
            _ => effects++, names => checks.AddRange(names));
        // No AP locations means the adapter must defer to vanilla, not consume this research.
        Assert.False(research.Research("soul_sins_1"));
        Assert.Empty(catalog.GetLocations("soul_sins_1", new HashSet<string>()));
        Assert.Empty(checks);
        Assert.Equal(0, effects);
        Assert.False(research.Research(null!));
    }

    [Fact]
    public async Task PreviewReadsUseNoHintCreationAndOnlyEnabledLocationIDs()
    {
        var helper = new Mock<ILocationCheckHelper>(MockBehavior.Strict);
        helper.Setup(value => value.ScoutLocationsAsync(HintCreationPolicy.None,
                It.Is<long[]>(ids => ids.SequenceEqual(new long[] { 123 }))))
            .ReturnsAsync(new Dictionary<long, ScoutedItemInfo>());
        var source = new ServerRewardPreviewSource(() => helper.Object,
            () => new Dictionary<string, long> { ["Enabled check"] = 123 });
        var cache = new RewardPreviewCache(source);
        await cache.FetchAsync(new[] { "Enabled check", "Enabled check", "Disabled check" });
        helper.VerifyAll();
        helper.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PreviewCacheRetainsRealItemRecipientAndGameUntilMainThreadAccepts()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<RewardPreview>>();
        var source = new Mock<IRewardPreviewSource>();
        source.Setup(value => value.ReadPlacementsAsync(It.IsAny<IEnumerable<string>>())).Returns(pending.Task);
        var cache = new RewardPreviewCache(source.Object);
        var request = cache.FetchAsync(new[] { "Check" });
        Assert.Null(cache.Get("Check"));
        pending.SetResult(new[] { new RewardPreview
            { Location = "Check", Item = "Bow", Recipient = "Link", Game = "Another game" } });
        var result = await request;
        Assert.Null(cache.Get("Check"));
        cache.Accept(result);
        Assert.Equal("Bow", cache.Get("Check").Item);
        Assert.Equal("Link", cache.Get("Check").Recipient);
        Assert.Equal("Another game", cache.Get("Check").Game);
    }

    [Fact]
    public async Task FailedPreviewFetchDoesNotInventAnItemAndCanRetry()
    {
        var source = new Mock<IRewardPreviewSource>();
        source.SetupSequence(value => value.ReadPlacementsAsync(It.IsAny<IEnumerable<string>>()))
            .ThrowsAsync(new IOException("Disconnected"))
            .ReturnsAsync(new[] { new RewardPreview { Location = "Check", Item = "Real reward" } });
        var cache = new RewardPreviewCache(source.Object);
        await Assert.ThrowsAsync<IOException>(() => cache.FetchAsync(new[] { "Check" }));
        Assert.Null(cache.Get("Check"));
        cache.Accept(await cache.FetchAsync(new[] { "Check" }));
        Assert.Equal("Real reward", cache.Get("Check").Item);
    }
}
