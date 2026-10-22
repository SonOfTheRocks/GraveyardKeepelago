using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Tests.TestData;

namespace GraveyardKeepelago.Tests;

public class ClientContractTests
{
    [Fact]
    public void LoadDefault_ReadsTheSharedSourceFiles()
    {
        var contract = ClientContractLoader.LoadDefault();

        Assert.Equal(1, contract.DataVersion);
        Assert.Equal(486, contract.Items.Count);
        Assert.Equal(286, contract.Locations.Count);
        Assert.Equal(273, contract.Locations.Values.Count(location => location.Triggers.Count > 0));
        Assert.All(contract.Items.Values, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.DLC));
            Assert.False(string.IsNullOrWhiteSpace(item.Classification));
            Assert.True(item.Amount > 0);
        });
        Assert.All(contract.Locations.Values, location =>
        {
            Assert.False(string.IsNullOrWhiteSpace(location.ParentRegion));
        });
    }

    [Fact]
    public void Factory_RegistersEveryContractItemByItsUniqueNetworkName()
    {
        var contract = ClientContractLoader.LoadDefault();
        var registry = new GKItemRegistry();

        new GKItemFactory(new FakePlayerActions(), contract).BuildAll(registry);

        Assert.Equal(contract.Items.Count, registry.Count);
        foreach (var item in contract.Items.Values)
            Assert.True(registry.TryGet(item.Name, out _), item.Name);
    }

    [Fact]
    public void Load_RejectsMissingSharedDataDirectory()
    {
        var missingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Assert.Throws<FileNotFoundException>(() => ClientContractLoader.Load(missingDirectory));
    }

    [Fact]
    public void ResearchPointGrant_UsesDedicatedResearchPointAction()
    {
        var (contract, registry, actions) = BuildRegistry();

        registry.TryGet(contract.Items["ITEM_RED_RESEARCH_POINTS"].Name, out var item);
        item.Apply();

        Assert.Equal("AddResearchPoints", actions.LastFunctionCalled);
        Assert.Equal("red", actions.LastResearchPointColor);
        Assert.Equal(100, actions.LastResearchPointAmount);
    }

    [Fact]
    public void TechnologyGrant_UnlocksTheDeclaredRootTechnologies()
    {
        var (contract, registry, actions) = BuildRegistry();

        registry.TryGet(contract.Items["ITEM_UNLOCK_ANATOMY"].Name, out var item);
        item.Apply();

        Assert.Equal("UnlockTechnologies", actions.LastFunctionCalled);
        Assert.Equal(new[] { "First slice" }, actions.LastUnlockedTechnologies);
    }

    [Fact]
    public void CrateFactoryGrant_RunsTheVanillaPlacementFlowScript()
    {
        var (contract, registry, actions) = BuildRegistry();

        registry.TryGet(contract.Items["ITEM_BP_CRATE_FACTORY"].Name, out var item);
        item.Apply();

        Assert.Equal("RunFlowScript", actions.LastFunctionCalled);
        Assert.Equal("elevator_and_supplies_place", actions.LastFlowScriptId);
    }

    [Theory]
    [InlineData("ITEM_GARDEN_CERTIFICATE", "Garden certificate", "scroll_garden_prop")]
    [InlineData("ITEM_BANK_CHECKS", "Bank checks", "bank_check")]
    [InlineData("ITEM_MEMORY_TINCTURE", "Memory tincture", "memory_potion")]
    public void CorrectedQuestItemNames_DeliverTheirOwnGameItem(
        string itemKey,
        string expectedName,
        string expectedGameItemId)
    {
        var (contract, registry, actions) = BuildRegistry();
        var contractItem = contract.Items[itemKey];

        registry.TryGet(contractItem.Name, out var item);
        item.Apply();

        Assert.Equal(expectedName, contractItem.Name);
        Assert.Equal("DropItem", actions.LastFunctionCalled);
        Assert.Equal(expectedGameItemId, actions.LastDropItemId);
    }

    private static (ClientContract Contract, GKItemRegistry Registry, FakePlayerActions Actions) BuildRegistry()
    {
        var contract = ClientContractLoader.LoadDefault();
        var actions = new FakePlayerActions();
        var registry = new GKItemRegistry();
        new GKItemFactory(actions, contract).BuildAll(registry);
        return (contract, registry, actions);
    }
}
