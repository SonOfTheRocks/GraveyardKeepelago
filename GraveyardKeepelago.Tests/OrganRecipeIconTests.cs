using GraveyardKeepelago.Items.Notifications;
using GraveyardKeepelago.Logic;

namespace GraveyardKeepelago.Tests;

public class OrganRecipeIconTests
{
    [Theory]
    [InlineData("story_souls", "story")]
    [InlineData("chapter_souls", "chapter")]
    [InlineData("book_souls", "book_hard")]
    [InlineData("sack_clock_silver_souls", "sack_clock_silver")]
    [InlineData("@sack_clock_gold_souls", "sack_clock_gold")]
    [InlineData("sack_star_silver_souls", "sack_star_silver")]
    [InlineData("@sack_star_gold_souls", "sack_star_gold")]
    public void MissingOrganIconUsesTheNormalRecipeFromSharedData(string organ, string normal)
    {
        var policy = new RecipeIconFallbacks(ClientContractLoader.LoadDefault());
        Assert.Equal(normal, policy.SelectCraft(organ, id => id == normal));
        Assert.Equal(organ.TrimStart('@'), policy.SelectCraft(organ, _ => true));
        Assert.Equal(organ.TrimStart('@'), policy.SelectCraft(organ, _ => false));
    }

    [Theory]
    [InlineData("ITEM_LEARN_CREATE_QUALITY_FERTILIZER_2_ORGAN_WORKBENCH", "sack_star_gold_souls", "sack_star_gold")]
    [InlineData("ITEM_LEARN_CREATE_BOOST_FERTILIZER_2_ORGAN_WORKBENCH", "sack_clock_gold_souls", "sack_clock_gold")]
    public void HiddenOnlyOrganGrantHasAPreviewEntryWithoutChangingItsActualUnlock(string key, string organ, string normal)
    {
        var contract = ClientContractLoader.LoadDefault();
        var item = contract.Items[key];
        var original = item.Grant.CraftIDs.ToArray();
        var notification = ItemNotification.Create(item.Name, item.Grant, null!);
        Assert.Equal(new[] { ("craft", organ) }, notification.Entries);
        Assert.Equal(normal, new RecipeIconFallbacks(contract).SelectCraft(notification.Entries[0].ID, id => id == normal));
        Assert.Equal(new[] { "@" + organ }, item.Grant.CraftIDs);
        Assert.Equal(original, item.Grant.CraftIDs);
    }

    [Theory]
    [InlineData("chapter")]
    [InlineData("@hidden")]
    [InlineData("unknown_souls")]
    [InlineData("souls_builddesk::soul_workbench")]
    public void OtherRecipesAndTheWorkbenchBlueprintKeepTheirOwnPresentation(string id)
        => Assert.Equal(id, new RecipeIconFallbacks(ClientContractLoader.LoadDefault()).SelectCraft(id,
            _ => throw new Exception("Unrelated recipe must not be inspected")));

    [Fact]
    public void OtherHiddenOnlyRecipesRemainHidden()
    {
        var grant = new ClientContractGrant { Type = "craft_unlock", CraftIDs = new() { "@hidden" } };
        Assert.Empty(ItemNotification.Create("Unrelated recipe", grant, null!).Entries);
    }
}
