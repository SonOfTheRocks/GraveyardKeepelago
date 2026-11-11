using System.Text;
using GraveyardKeepelago.Locations;
using Newtonsoft.Json.Linq;

namespace GraveyardKeepelago.Tests;

public class SaveBoundLocationChecksTests
{
    private static readonly (string Seed, int Team, int Slot) Identity = ("Seed", 0, 1);
    private static readonly Dictionary<string, long> Active = new() { ["Research"] = 17, ["Book"] = 31 };

    [Fact]
    public void StoresActiveChecksBeforeSendingAndIgnoresRepeatedCalls()
    {
        var phrases = new List<string> { "vanilla_phrase" };
        var checks = SaveBoundLocationChecks.Bind(phrases, Identity, Active, newSave: true);
        var packets = new List<long[]>();
        Assert.True(checks.Connect(Identity, ids =>
        {
            Assert.Equal(new[] { "Research" }, ReadRecord(phrases)["Checks"]!.Values<string>());
            packets.Add(ids);
        }));

        checks.AddCheckedLocations(new[] { "Inactive", "Research", "Research" });
        checks.AddCheckedLocation("Research");

        Assert.Equal(new long[] { 17 }, Assert.Single(packets));
        Assert.Equal("vanilla_phrase", phrases[0]);
        Assert.Equal(2, phrases.Count);
    }

    [Fact]
    public void ReplaysOfflineChecksOnReconnectAndReload()
    {
        var phrases = new List<string>();
        var checks = SaveBoundLocationChecks.Bind(phrases, Identity, Active, newSave: true);
        var packets = new List<long[]>();
        Assert.True(checks.Connect(Identity, packets.Add));
        checks.Disconnect();
        checks.AddCheckedLocation("Book");
        Assert.Empty(packets);

        Assert.True(checks.Connect(Identity, packets.Add));
        Assert.Equal(new long[] { 31 }, Assert.Single(packets));

        // A distinct phrase list represents the native save being deserialized.
        var reloaded = SaveBoundLocationChecks.Bind(phrases.ToList(), Identity, Active, newSave: false);
        packets.Clear();
        Assert.True(reloaded.Connect(Identity, packets.Add));
        reloaded.AddCheckedLocation("Book");
        Assert.Equal(new long[] { 31 }, Assert.Single(packets));
    }

    [Fact]
    public void FailedTransmissionLeavesTheCheckAvailableForReplay()
    {
        var phrases = new List<string>();
        var checks = SaveBoundLocationChecks.Bind(phrases, Identity, Active, newSave: true);
        Assert.True(checks.Connect(Identity, _ => throw new IOException("Connection interrupted")));
        Assert.Throws<IOException>(() => checks.AddCheckedLocation("Research"));

        var packets = new List<long[]>();
        var reloaded = SaveBoundLocationChecks.Bind(phrases.ToList(), Identity, Active, newSave: false);
        Assert.True(reloaded.Connect(Identity, packets.Add));
        Assert.Equal(new long[] { 17 }, Assert.Single(packets));
    }

    [Theory]
    [InlineData("Other seed", 0, 1)]
    [InlineData("Seed", 1, 1)]
    [InlineData("Seed", 0, 2)]
    public void DifferentIdentityCannotLoadOrReceiveChecks(string seed, int team, int slot)
    {
        var phrases = new List<string>();
        var checks = SaveBoundLocationChecks.Bind(phrases, Identity, Active, newSave: true);
        var packets = new List<long[]>();
        Assert.True(checks.Connect(Identity, packets.Add));
        checks.AddCheckedLocation("Book");
        packets.Clear();

        Assert.False(checks.Connect((seed, team, slot), packets.Add));
        checks.AddCheckedLocation("Research");
        Assert.Empty(packets);

        var saved = Assert.Single(phrases);
        Assert.Throws<InvalidDataException>(() =>
            SaveBoundLocationChecks.Bind(phrases, (seed, team, slot), Active, newSave: false));
        Assert.Equal(saved, Assert.Single(phrases));
    }

    [Fact]
    public void ANewSaveForTheSameSlotDoesNotInheritAnotherSavesChecks()
    {
        var firstSave = SaveBoundLocationChecks.Bind(new List<string>(), Identity, Active, newSave: true);
        firstSave.AddCheckedLocation("Book");
        var secondSave = SaveBoundLocationChecks.Bind(new List<string>(), Identity, Active, newSave: true);
        var packets = new List<long[]>();

        Assert.True(secondSave.Connect(Identity, packets.Add));
        Assert.Empty(packets);
    }

    [Fact]
    public void LoadingPreservesExistingProgressAndFiltersReplayToActiveLocations()
    {
        var record = JObject.Parse("""
            {"Version":1,"Seed":"Seed","Team":0,"Slot":1,"Checks":["Research","Inactive"],"Crafts":["flesh"]}
            """);
        var phrases = new List<string> { Encode(record), "vanilla_phrase" };
        var checks = SaveBoundLocationChecks.Bind(phrases, Identity, Active, newSave: false);
        var packets = new List<long[]>();

        Assert.True(checks.Connect(Identity, packets.Add));
        Assert.Equal(new long[] { 17 }, Assert.Single(packets));
        checks.AddCheckedLocation("Book");
        Assert.Equal(new long[] { 31 }, packets[1]);
        Assert.Equal("flesh", ReadRecord(phrases)["Crafts"]![0]!.Value<string>());
        Assert.Equal("vanilla_phrase", phrases[1]);
    }

    [Theory]
    [InlineData("graveyard_keepelago:progress:v1:invalid")]
    [InlineData("graveyard_keepelago:progress:v2:future")]
    public void DamagedOrFutureRecordsAreNeverOverwritten(string record)
    {
        var phrases = new List<string> { record };
        Assert.Throws<InvalidDataException>(() =>
            SaveBoundLocationChecks.Bind(phrases, Identity, Active, newSave: true));
        Assert.Equal(record, Assert.Single(phrases));
    }

    [Fact]
    public void ExistingUnboundSaveCannotAdoptAConnection()
    {
        var phrases = new List<string> { "vanilla_phrase" };
        Assert.Throws<InvalidDataException>(() =>
            SaveBoundLocationChecks.Bind(phrases, Identity, Active, newSave: false));
        Assert.Equal("vanilla_phrase", Assert.Single(phrases));
    }

    private static string Encode(JObject record) =>
        SaveBoundLocationChecks.RecordPrefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(record.ToString()));

    private static JObject ReadRecord(IEnumerable<string> phrases)
    {
        var record = Assert.Single(phrases, phrase =>
            phrase.StartsWith(SaveBoundLocationChecks.RecordPrefix, StringComparison.Ordinal));
        var json = Convert.FromBase64String(record[SaveBoundLocationChecks.RecordPrefix.Length..]);
        return JObject.Parse(Encoding.UTF8.GetString(json));
    }
}
