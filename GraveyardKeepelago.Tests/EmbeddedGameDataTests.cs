using GraveyardKeepelago.Logic;
using GraveyardKeepelago.Technologies;
using Newtonsoft.Json.Linq;

namespace GraveyardKeepelago.Tests;

public class EmbeddedGameDataTests
{
    [Fact]
    public void EmbeddedDataMatchesAllSharedSourceFilesAndRemainsSelfContained()
    {
        var fixtureRoot = Path.Combine(AppContext.BaseDirectory, "Data");
        var files = Directory.GetFiles(fixtureRoot, "*.json", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(fixtureRoot, path).Replace((char)92, '/')).Order().ToArray();
        Assert.Equal(files, SharedGameData.Files.Order().ToArray());
        foreach (var path in files)
            Assert.True(JToken.DeepEquals(JToken.Parse(File.ReadAllText(Path.Combine(fixtureRoot, path))),
                SharedGameData.Read<JToken>(path)), path);
        var embedded = ClientContractLoader.LoadDefault();
        var disk = ClientContractLoader.Load(fixtureRoot);
        Assert.Equal(disk.DataVersion, embedded.DataVersion);
        Assert.Equal(disk.Items.Keys.Order(), embedded.Items.Keys.Order());
        Assert.Equal(disk.Locations.Keys.Order(), embedded.Locations.Keys.Order());
        Assert.NotEmpty(TechnologyCatalog.LoadDefault().Definitions);
    }

    [Fact]
    public void MissingEmbeddedResourceFailsClearlyInsteadOfReadingAnUnrelatedDiskFile()
    {
        var exception = Assert.Throws<FileNotFoundException>(() => SharedGameData.Read<JToken>("missing.json"));
        Assert.Equal("missing.json", exception.FileName);
    }
    [Fact]
    public void DataPackageFallbackDoesNotRequireOrCreateInstallationFiles()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var cache = new GraveyardKeepelago.Archipelago.GKDataPackageCache("graveyard_keeper", path);
        Assert.Empty(cache.GetAllItems());
        Assert.Empty(cache.GetAllLocations());
        Assert.False(Directory.Exists(path));
    }}



