using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Technologies;
using KaitoKid.ArchipelagoUtilities.Net.Client.ConnectionResults;
using KaitoKid.Utilities.Interfaces;
using Moq;

namespace GraveyardKeepelago.Tests;

public class SharedDataTests
{
    [Fact]
    public void EmbeddedContractContainsExpectedGrantsAndTriggerTypes()
    {
        var contract = ClientContractLoader.LoadDefault();
        Assert.Equal(488, contract.Items.Count);
        Assert.Equal(
            new[] { ("Bats Trap", "bats"), ("Green Slime Trap", "green_slimes") },
            contract.Items.Values.Where(item => item.Grant.Type == "trap")
                .OrderBy(item => item.Name, StringComparer.Ordinal).Select(item => (item.Name, item.Grant.TrapID)));
        Assert.Equal(
            new[] { "craft_completion_reward", "craft_unlock_reward", "flow_quest_reward",
                "flow_relation_reward", "technology_reward", "world_item_reward" },
            contract.Locations.Values.SelectMany(location => location.Triggers)
                .Select(trigger => trigger.Type).Distinct().Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ResearchLocationsAndQuestTechnologyRewardsUseTheirDeclaredSources()
    {
        var contract = ClientContractLoader.LoadDefault();
        var locationNames = contract.Locations.Values.Select(location => location.Name).ToHashSet();
        var technologies = TechnologyCatalog.LoadDefault().Definitions.Values;
        var researchChecks = technologies.SelectMany(technology => technology.Locations).ToArray();
        Assert.Equal(487, researchChecks.Length);
        Assert.DoesNotContain(researchChecks, locationNames.Contains);
        var questTechnologies = technologies.Where(technology => technology.QuestReward != null).ToArray();
        Assert.Equal(3, questTechnologies.Length);
        Assert.All(questTechnologies, technology => Assert.Contains(technology.QuestReward, locationNames));
        Assert.Equal(28, technologies.Count(technology => technology.RecipeItem != null));
    }

    [Fact]
    public void UnknownResourceFailsWithItsEmbeddedPath()
    {
        var exception = Assert.Throws<FileNotFoundException>(() => SharedGameData.ReadText("missing.json"));
        Assert.Equal("missing.json", exception.FileName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void DeclaredVersionIsRejectedRegardlessOfTheEmbeddedVersion(int versionOffset)
    {
        AssertRejectedVersion(ClientContractLoader.LoadDefault().DataVersion + versionOffset);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    public void InvalidVersionDeclarationIsRejected(object? version)
    {
        AssertRejectedVersion(version);
    }

    private static void AssertRejectedVersion(object? version)
    {
        var slot = new SlotData("Keeper", new() { [SlotData.DATA_VERSION_KEY] = version! }, Mock.Of<ILogger>());

        var rejection = Assert.IsType<FailedConnectionResult>(slot.GetDataVersionRejection());
        Assert.False(rejection.Success);
        Assert.Contains($"data version {version ?? "(missing value)"}", rejection.Message);
    }

    [Fact]
    public void UnversionedSlotsKeepTheLegacyConnectionPath()
    {
        var slot = new SlotData("Keeper", new(), Mock.Of<ILogger>());

        Assert.Null(slot.GetDataVersionRejection());
    }
}
