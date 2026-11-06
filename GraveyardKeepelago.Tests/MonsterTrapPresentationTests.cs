using Archipelago.MultiClient.Net.Enums;
using GraveyardKeepelago.Items.Traps;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Technologies;

namespace GraveyardKeepelago.Tests;

public class MonsterTrapPresentationTests
{
    [Theory]
    [InlineData("ITEM_BATS_TRAP", "bats", "bat_test", "300_bat_face_fly_1")]
    [InlineData("ITEM_GREEN_SLIME_TRAP", "green_slimes", "slime_test", "320_slime_jump_1")]
    public void LocalTrapUsesItsOwnMonsterAsTheMainIconAndASkullAsTheCornerBadge(
        string key, string id, string monster, string sprite)
    {
        var item = ClientContractLoader.LoadDefault().Items[key];
        var preview = new RewardPreview { Game = "Graveyard Keeper", Item = item.Name, Flags = ItemFlags.Trap };
        var local = RewardPreviewPresentation.LocalDefinition(preview, "Graveyard Keeper",
            new Dictionary<string, ClientContractItem> { [item.Name] = item });
        Assert.Same(item, local);
        var trap = MonsterTrapDefinition.Get(local.Grant.TrapID);
        Assert.Equal(id, trap.ID);
        Assert.Equal(monster, trap.MonsterID);
        Assert.Equal(4, trap.Count);
        Assert.Equal(sprite, trap.SpriteName);
        Assert.NotEqual("i_skull", trap.SpriteName);
        Assert.Equal("i_skull", RewardPreviewPresentation.CategorySpriteName(preview.Flags));
    }

    [Theory]
    [InlineData("Bats Trap")]
    [InlineData("Green Slime Trap")]
    public void ForeignTrapWithAMatchingNameKeepsTheApMainIconAndSkullCorner(string name)
    {
        var contract = ClientContractLoader.LoadDefault();
        var definitions = contract.Items.Values.ToDictionary(i => i.Name);
        var foreign = new RewardPreview { Game = "Another game", Item = name, Flags = ItemFlags.Trap };
        Assert.Null(RewardPreviewPresentation.LocalDefinition(foreign, "Graveyard Keeper", definitions));
        Assert.Equal("i_skull", RewardPreviewPresentation.CategorySpriteName(foreign.Flags));
    }

    [Fact]
    public void UnknownOrUnscoutedRewardsKeepTheApFallback()
    {
        var definitions = new Dictionary<string, ClientContractItem>();
        Assert.Null(RewardPreviewPresentation.LocalDefinition(null!, "Graveyard Keeper", definitions));
        Assert.Null(RewardPreviewPresentation.LocalDefinition(new RewardPreview { Game = "Graveyard Keeper" },
            "Graveyard Keeper", definitions));
        Assert.Null(RewardPreviewPresentation.LocalDefinition(new RewardPreview { Game = "Graveyard Keeper", Item = "Unknown" },
            "Graveyard Keeper", definitions));
    }
}
