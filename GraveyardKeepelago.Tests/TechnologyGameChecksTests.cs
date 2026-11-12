using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.Locations;
using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Technologies;
using KaitoKid.Utilities.Interfaces;
using Moq;

namespace GraveyardKeepelago.Tests;

public sealed class TechnologyGameChecksTests
{
    private const string Book = "Simple and Tasty - Unlock 1";
    private const string Quest = "LOCATION_TUTORIAL_REWARD_ENTER_MORGUE_WITH_CORPSE";
    private static readonly (string, int, int) Identity = ("seed", 0, 1);

    [Fact]
    public void ResearchReportsOnlyPurchasedActiveLocationsAndRunsEffectsOnce()
    {
        var active = new Dictionary<string, long>
        {
            ["Hardspares - Unlock 1"] = 10,
            ["Hardspares - Unlock 3"] = 12,
        };
        var (checks, sent) = BindOnline(active);
        var researched = new List<string>();
        var effects = 0;
        void Complete() => checks.Complete("Hardspares", researched, () => effects++);

        Assert.True(checks.TryUnlock("Hardspares", purchased: true, Complete));
        Assert.True(checks.TryUnlock("Hardspares", purchased: true, Complete));
        Assert.False(checks.TryUnlock("soul_sins_2", purchased: true, Complete));
        Assert.False(checks.TryUnlock("hardspares", purchased: true, Complete));
        Assert.Equal(["Hardspares"], researched);
        Assert.Equal(1, effects);
        Assert.Equal([10L, 12L], Assert.Single(sent));
    }

    [Fact]
    public void DirectUnlockAndPurchaseCannotClaimARecipeBookCheck()
    {
        var (checks, sent) = BindOnline(new Dictionary<string, long>
        {
            [Book] = 30,
            ["Hardspares - Unlock 1"] = 10,
        });

        Assert.True(checks.TryUnlock("2min", purchased: false, () => { }));
        Assert.True(checks.TryUnlock("2min", purchased: true, () => { }));
        Assert.True(checks.TryUnlock("Hardspares", purchased: false, () => { }));
        Assert.False(checks.TryUseRecipeBook("recipe_2min", "Hardspares", () => throw new InvalidOperationException()));
        Assert.False(checks.TryUseRecipeBook("recipe_simple_snacks", "2min", () => throw new InvalidOperationException()));
        Assert.Empty(sent);
    }

    [Fact]
    public void BookIsRecordedBeforeCompletionAndItsDialogDoesNotRepeatEffects()
    {
        var (checks, sent) = BindOnline(new Dictionary<string, long>
        {
            [Book] = 30,
            ["Simple and Tasty - Unlock 2"] = 31,
        });
        var researched = new List<string>();
        var effects = 0;
        void Complete()
        {
            Assert.Equal([30L], Assert.Single(sent));
            checks.Complete("2min", researched, () => effects++);
        }

        Assert.True(checks.TryUseRecipeBook("recipe_2min", "2min", Complete));
        Assert.True(checks.TryUnlock("2min", purchased: false, Complete));
        Assert.True(checks.TryUseRecipeBook("recipe_2min", "2min", Complete));
        Assert.Equal(1, effects);
        Assert.Equal(["2min"], researched);
    }

    [Fact]
    public void QuestUnlockRoutesOnlyTheQuestReward()
    {
        var questName = ClientContractLoader.LoadDefault().Locations[Quest].Name;
        var (checks, sent) = BindOnline(new Dictionary<string, long>
        {
            [questName] = 40,
            ["First slice - Unlock 1"] = 41,
        });
        var researched = new List<string>();

        Assert.True(checks.TryUnlock("First slice", purchased: false,
            () => checks.Complete("First slice", researched, () => Assert.Contains("First slice", researched))));
        Assert.Equal(["First slice"], researched);
        Assert.Equal([40L], Assert.Single(sent));
    }

    [Fact]
    public void OfflineActionsReloadAndReplayOnlyForTheOriginalSlot()
    {
        var active = new Dictionary<string, long> { ["Hardspares - Unlock 2"] = 11, [Book] = 30 };
        var phrases = new List<string> { "vanilla_phrase" };
        var original = SaveBoundLocationChecks.Bind(phrases, Identity, active, newSave: true);
        var checks = Create(original, active.Keys);
        var sent = new List<long[]>();
        original.Connect(Identity, sent.Add);
        original.Disconnect();
        checks.TryUnlock("Hardspares", purchased: true, () => { });
        checks.TryUseRecipeBook("recipe_2min", "2min", () => { });
        Assert.Empty(sent);

        var saved = new List<string>(phrases);
        Assert.Throws<InvalidDataException>(() =>
            SaveBoundLocationChecks.Bind(saved, ("seed", 0, 2), active, newSave: false));
        var otherSave = SaveBoundLocationChecks.Bind(new List<string>(), ("seed", 0, 2), active, newSave: true);
        Assert.True(otherSave.Connect(("seed", 0, 2), sent.Add));
        Assert.Empty(sent);

        var reloaded = SaveBoundLocationChecks.Bind(saved, Identity, active, newSave: false);
        Assert.True(reloaded.Connect(Identity, sent.Add));
        var rebound = Create(reloaded, active.Keys);
        rebound.TryUnlock("Hardspares", purchased: true, () => { });
        rebound.TryUseRecipeBook("recipe_2min", "2min", () => { });
        Assert.Equal([11L, 30L], Assert.Single(sent));
        Assert.Contains("vanilla_phrase", saved);
    }

    [Fact]
    public void TechnologyLeavesControlledGrantsForItemReceiptAndKeepsNativeUnlocks()
    {
        var catalog = TechnologyCatalog.LoadDefault();
        var contract = ClientContractLoader.LoadDefault();
        var service = new LocationTriggerService(Mock.Of<ILocationChecker>(), new LocationTriggerRegistry(contract, [], catalog));
        var dlcs = new DLCManager(Mock.Of<ILogger>(), []);
        var checks = new TechnologyGameChecks(service, catalog,
            contract.Items.Values.Where(item => dlcs.IsContentEnabled(item.DLC)), ["tech_alchemy_begins", "unlock_totems"]);

        Assert.Equal(["souls_builddesk:p:corpse_bed_place"],
            checks.GetCrafts(["ex:mf_preparation_1:flesh", "@ex:mf_preparation_2:flesh", "@morgue_builddesk:p:corpse_bed_place", "@souls_builddesk:p:corpse_bed_place"]));
        Assert.Equal(["unmanaged_work"], checks.GetWorks(["@t_iron_ore_1", "@unmanaged_work"]));
        Assert.Equal(["unmanaged_perk"], checks.GetPerks(["@p_t_beeswax", "@unmanaged_perk"]));
        Assert.True(checks.ShouldSuppressScriptCraft("tech_alchemy_begins", "@keeper_room_builddesk:p:keeper_room_picture_1"));
        Assert.False(checks.ShouldSuppressScriptCraft("unrelated_flow", "keeper_room_builddesk:p:keeper_room_picture_1"));
        Assert.False(checks.ShouldSuppressScriptCraft("unlock_totems", "souls_builddesk::soul_totem_souls"));

        var withSouls = Create(Mock.Of<ILocationChecker>(), []);
        Assert.True(withSouls.ShouldSuppressScriptCraft("unlock_totems", "souls_builddesk::soul_totem_souls"));
    }

    private static (TechnologyGameChecks Checks, List<long[]> Sent) BindOnline(IReadOnlyDictionary<string, long> active)
    {
        var recorder = SaveBoundLocationChecks.Bind(new List<string>(), Identity, active, newSave: true);
        var sent = new List<long[]>();
        recorder.Connect(Identity, sent.Add);
        return (Create(recorder, active.Keys), sent);
    }

    private static TechnologyGameChecks Create(ILocationChecker checker, IEnumerable<string> active)
    {
        var contract = ClientContractLoader.LoadDefault();
        var catalog = TechnologyCatalog.LoadDefault();
        var service = new LocationTriggerService(checker, new LocationTriggerRegistry(contract, active, catalog));
        return new TechnologyGameChecks(service, catalog, contract.Items.Values, ["tech_alchemy_begins", "unlock_totems"]);
    }
}