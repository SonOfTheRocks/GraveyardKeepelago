using GraveyardKeepelago.Items;
using GraveyardKeepelago.Items.Traps;
using GraveyardKeepelago.Locations;
using GraveyardKeepelago.Locations.Patches;
using GraveyardKeepelago.Logic;
using KaitoKid.ArchipelagoUtilities.Net.Client;
using KaitoKid.Utilities.Interfaces;
using Moq;

namespace GraveyardKeepelago.Tests;

public sealed class CraftGameChecksTests
{
    private const string Flesh = "Tutorial - Get First Flesh";
    private const string Bishop = "Tutorial - Talk to Bishop (2)";
    private const string Donkey = "Donkey - Meet the Donkey";
    private const string Smiler = "Smiler - Ask him about human healing";
    private const string FirstTable = "ex:mf_preparation_1:flesh";
    private const string SecondTable = "ex:mf_preparation_2:flesh";
    private const string BishopPicture = "keeper_room_builddesk:p:keeper_room_picture_4";
    private const string DonkeyPicture = "keeper_room_builddesk:p:keeper_room_picture_7";
    private const string HumanHealing = "souls_builddesk::soul_healing_for_human";
    private static readonly (string Seed, int Team, int Slot) Identity = ("seed", 0, 1);

    [Theory]
    [InlineData(FirstTable, SecondTable)]
    [InlineData(SecondTable, FirstTable)]
    public void EitherCompletedExtractionRecordsTheSameLocationOnce(string first, string second)
    {
        var (checks, sent) = BindOnline(new Dictionary<string, long> { [Flesh] = 10 });

        Assert.True(UnlockCraftCheckPatch.ShouldRunOriginal(checks, first));
        checks.HandleCraftCompletion("craft_emitter");
        Assert.Empty(sent);

        checks.HandleCraftCompletion(first);
        checks.HandleCraftCompletion(second);
        Assert.Equal([10L], Assert.Single(sent));
    }

    [Theory]
    [InlineData(Bishop, BishopPicture)]
    [InlineData(Donkey, DonkeyPicture)]
    [InlineData(Smiler, HumanHealing)]
    public void ActiveQuestBlueprintReplacesTheVanillaUnlock(string location, string craftID)
    {
        var (checks, sent) = BindOnline(new Dictionary<string, long> { [location] = 20 });

        checks.HandleCraftCompletion(craftID);
        Assert.Empty(sent);
        Assert.False(UnlockCraftCheckPatch.ShouldRunOriginal(checks, craftID));
        Assert.False(UnlockCraftCheckPatch.ShouldRunOriginal(checks, craftID));
        Assert.Equal([20L], Assert.Single(sent));
    }

    [Fact]
    public void InactiveLocationsAndUnversionedSavesKeepVanillaUnlocks()
    {
        var (checks, sent) = BindOnline(new Dictionary<string, long> { [Bishop] = 20 });

        checks.HandleCraftCompletion(FirstTable);
        Assert.True(UnlockCraftCheckPatch.ShouldRunOriginal(checks, DonkeyPicture));
        Assert.True(UnlockCraftCheckPatch.ShouldRunOriginal(checks, HumanHealing));
        Assert.True(UnlockCraftCheckPatch.ShouldRunOriginal(null, BishopPicture));
        Assert.Empty(sent);
    }

    [Fact]
    public void ReceivedBlueprintCanUnlockWithoutClaimingItsQuestAndClearsTheScopeOnFailure()
    {
        var (checks, sent) = BindOnline(new Dictionary<string, long> { [Bishop] = 20 });
        const string itemName = "Blueprint: Bishop's Choice";
        var item = new Mock<IAPItem>();
        item.Setup(grant => grant.Apply()).Callback(() =>
        {
            Assert.True(UnlockCraftCheckPatch.ShouldRunOriginal(checks, BishopPicture));
            throw new InvalidOperationException("Simulated grant failure");
        });
        var registry = new GKItemRegistry();
        registry.AddRecipe(itemName, item.Object);
        var processor = new ItemProcessor(Mock.Of<ILogger>(), registry, Mock.Of<ITrapManager>());

        Assert.Throws<InvalidOperationException>(() =>
            processor.ProcessItem(new ReceivedItem("dummy", itemName, "dummy", 0L, 0L, 0L, 0)));
        item.Verify(grant => grant.Apply(), Times.Once);
        Assert.Empty(sent);

        Assert.False(UnlockCraftCheckPatch.ShouldRunOriginal(checks, BishopPicture));
        Assert.Equal([20L], Assert.Single(sent));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OfflineCraftChecksAndSmilersAlternativeTriggerSurviveReloadAndReplay(bool dialogFirst)
    {
        var active = new Dictionary<string, long> { [Flesh] = 10, [Smiler] = 30 };
        var phrases = new List<string>();
        var original = SaveBoundLocationChecks.Bind(phrases, Identity, active, newSave: true);
        var checks = Create(original, active.Keys);
        var sent = new List<long[]>();
        Assert.True(original.Connect(Identity, sent.Add));
        original.Disconnect();

        checks.HandleCraftCompletion(FirstTable);
        if (dialogFirst)
            Assert.Equal(FlowRewardSuppression.Quest, checks.HandleFlowChoice("npc_smiler", 3, 5));
        Assert.False(UnlockCraftCheckPatch.ShouldRunOriginal(checks, HumanHealing));
        if (!dialogFirst)
            Assert.Equal(FlowRewardSuppression.Quest, checks.HandleFlowChoice("npc_smiler", 3, 5));
        Assert.Empty(sent);

        var saved = new List<string>(phrases);
        Assert.Throws<InvalidDataException>(() =>
            SaveBoundLocationChecks.Bind(saved, ("seed", 0, 2), active, newSave: false));
        var reloaded = SaveBoundLocationChecks.Bind(saved, Identity, active, newSave: false);
        Assert.True(reloaded.Connect(Identity, sent.Add));

        var rebound = Create(reloaded, active.Keys);
        rebound.HandleCraftCompletion(SecondTable);
        rebound.HandleFlowChoice("npc_smiler", 3, 5);
        Assert.False(UnlockCraftCheckPatch.ShouldRunOriginal(rebound, HumanHealing));
        Assert.Equal([10L, 30L], Assert.Single(sent));
    }

    private static (LocationTriggerService Checks, List<long[]> Sent) BindOnline(IReadOnlyDictionary<string, long> active)
    {
        var recorder = SaveBoundLocationChecks.Bind(new List<string>(), Identity, active, newSave: true);
        var sent = new List<long[]>();
        Assert.True(recorder.Connect(Identity, sent.Add));
        return (Create(recorder, active.Keys), sent);
    }

    private static LocationTriggerService Create(ILocationChecker checker, IEnumerable<string> active)
        => new(checker, new LocationTriggerRegistry(ClientContractLoader.LoadDefault(), active));
}
