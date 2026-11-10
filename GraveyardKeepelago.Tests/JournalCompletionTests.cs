using GraveyardKeepelago.Journal;
using GraveyardKeepelago.GameModifications.UI;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Tests.TestData;

namespace GraveyardKeepelago.Tests;

public class JournalCompletionTests
{
    private readonly JournalCompletion _completion = new(
        new[] { "Stone", "Stone cutter", "Furnace", "stone", "", "Advanced furnace" },
        new[] { "Church: First sermon", "Garden: Harvest carrots" });

    private JournalCompletionSuggestion[] Suggest(string value) => _completion.Suggest(value, value.Length, value.Length, value.Length);

    [Theory]
    [InlineData("!hi", "!hint ")]
    [InlineData("!HIN", "!hint ")]
    [InlineData("!hont", "!hint ")]
    [InlineData("!hint sto", "!hint Stone")]
    [InlineData("!hint FURN", "!hint Furnace")]
    [InlineData("!hint Furnce", "!hint Furnace")]
    [InlineData("!getItem sto", "!getitem Stone")]
    [InlineData("!hint_location sermon", "!hint_location Church: First sermon")]
    [InlineData("!missing carrots", "!missing Garden: Harvest carrots")]
    [InlineData("!checked Church", "!checked Church: First sermon")]
    public void CompletesCommandsAndTheirOwnNameArguments(string text, string expected)
        => Assert.Contains(Suggest(text), suggestion => suggestion.Value == expected);

    [Fact]
    public void MatchesAreDistinctAndPrefixMatchesComeBeforeSubstringMatches()
    {
        Assert.Equal(new[] { "!hint Stone", "!hint Stone cutter" }, Suggest("!hint sto").Select(suggestion => suggestion.Value));
        Assert.Equal("!hint Furnace", Suggest("!hint furn")[0].Value);
    }

    [Theory]
    [InlineData("normal chat")]
    [InlineData("/help")]
    [InlineData("!alias my name")]
    [InlineData("!countdown 10")]
    [InlineData("!admin password")]
    [InlineData("!unknown stuff")]
    [InlineData("!hint Church")]
    [InlineData("!hint_location Furnace")]
    public void FreeTextUnknownCommandsAndWrongArgumentNamespacesStayUntouched(string text)
        => Assert.Empty(Suggest(text));

    [Fact]
    public void EditingInTheMiddleOrReplacingASelectionDoesNotOfferAFullLineReplacement()
    {
        Assert.Empty(_completion.Suggest("!hint sto", 5, 5, 5));
        Assert.Empty(_completion.Suggest("!hint sto", 9, 6, 9));
    }

    [Fact]
    public void CommandHelpRemainsAvailableWithoutConnectionOrNameData()
    {
        var completion = new JournalCompletion(null, null);
        Assert.NotEmpty(completion.Suggest("!", 1, 1, 1));
        Assert.Empty(completion.Suggest("!hint x", 7, 7, 7));
        Assert.Contains(Suggest("!release"), suggestion => suggestion.Description.Contains("server rules"));
    }

    [Theory]
    [InlineData("!hint ")]
    [InlineData("!getitem ")]
    [InlineData("!getItem ")]
    public void ItemArgumentsUseThePopulatedRuntimeRegistryWithoutLegacyDataPackageCache(string command)
    {
        var contract = ClientContractLoader.LoadDefault();
        var registry = new GKItemRegistry();
        new GKItemFactory(new FakePlayerActions(), contract).BuildAll(registry);
        var completion = new JournalCompletion(registry.Names, Array.Empty<string>());
        Assert.True(completion.HasParameterSuggestions(command));
        var suggestions = completion.Suggest(command, command.Length, command.Length, command.Length);
        Assert.Equal(contract.Items.Count, suggestions.Length);
        Assert.Equal(contract.Items.Values.Select(item => item.Name).OrderBy(name => name, StringComparer.OrdinalIgnoreCase),
            suggestions.Select(suggestion => suggestion.Description));
        var itemName = contract.Items.Values.First().Name;
        var input = command + itemName;
        Assert.Contains(completion.Suggest(input, input.Length, input.Length, input.Length),
            suggestion => suggestion.Value == command.ToLowerInvariant() + itemName);
    }

    [Theory]
    [InlineData("!hint ")]
    [InlineData("!getitem ")]
    public void CategoryAndPartialItemNameMatchAcrossInterveningWords(string command)
    {
        var contract = ClientContractLoader.LoadDefault();
        var completion = new JournalCompletion(contract.Items.Values.Select(item => item.Name), null);
        string[] Matches(string query)
        {
            var text = command + query;
            return completion.Suggest(text, text.Length, text.Length, text.Length)
                .Select(suggestion => suggestion.Description).ToArray();
        }
        var bowls = Matches("bow");
        Assert.Equal(4, bowls.Length);
        Assert.Equal(bowls, Matches("Create: bow"));
        Assert.Equal(bowls, Matches("create:   BOW"));
    }

    [Theory]
    [InlineData("!hint_location Church: sermon", "!hint_location Church: First sermon")]
    [InlineData("!missing Garden: carrots", "!missing Garden: Harvest carrots")]
    [InlineData("!checked Garden: carrots", "!checked Garden: Harvest carrots")]
    public void LocationQueriesMatchWordsWithGaps(string text, string expected)
        => Assert.Contains(Suggest(text), suggestion => suggestion.Value == expected);

    [Theory]
    [InlineData("!hint_location Garden: sermon")]
    [InlineData("!hint_location sermon Church:")]
    [InlineData("!hint_location Church: First First")]
    public void EveryTermMustOccurInOrderWithoutReusingOneOccurrence(string text)
        => Assert.Empty(Suggest(text));

    [Fact]
    public void ContiguousMatchesRankAheadOfGappedMatchesAndInsertedNamesStayCanonical()
    {
        var completion = new JournalCompletion(new[] { "Create: Ceramic bowls", "Create: bowl", "Other Create: bowl" }, null);
        const string text = "!hint Create: bow";
        Assert.Equal(new[] { "!hint Create: bowl", "!hint Other Create: bowl", "!hint Create: Ceramic bowls" },
            completion.Suggest(text, text.Length, text.Length, text.Length).Select(suggestion => suggestion.Value));
    }

    [Theory]
    [InlineData(296, 300, 400, 396)]
    [InlineData(-296, -300, -400, -396)]
    [InlineData(592, 600, 800, 792)]
    [InlineData(195, 194, -50, -49)]
    public void DecorativeCornersKeepTheirNativeInsetAndVerticalPivotOffset(float origin, float edge, float target, float expected)
        => Assert.Equal(expected, JournalPanelGeometry.CornerPosition(origin, edge, target));
}
