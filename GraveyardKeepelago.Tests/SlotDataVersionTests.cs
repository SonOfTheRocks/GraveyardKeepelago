using GraveyardKeepelago.Archipelago;
using GraveyardKeepelago.Tests.TestData;

namespace GraveyardKeepelago.Tests;

public class SlotDataVersionTests
{
    [Fact]
    public void DataVersion_ReadsArchipelagoLongValue()
    {
        var fields = DefaultFields();
        fields[SlotData.DATA_VERSION_KEY] = 7L;

        var slotData = new SlotData("Keeper", fields, new FakeLogger());

        Assert.Equal(7, slotData.DataVersion);
    }

    [Fact]
    public void DataVersion_ReadsIntegerAndStringRepresentations()
    {
        foreach (var value in new object[] { 8, "9" })
        {
            var fields = DefaultFields();
            fields[SlotData.DATA_VERSION_KEY] = value;

            var slotData = new SlotData("Keeper", fields, new FakeLogger());

            Assert.Equal(value is int ? 8 : 9, slotData.DataVersion);
        }
    }

    [Fact]
    public void MissingDataVersion_UsesIncompatibleSentinel()
    {
        var slotData = new SlotData("Keeper", DefaultFields(), new FakeLogger());

        Assert.Equal(0, slotData.DataVersion);
    }

    private static Dictionary<string, object> DefaultFields()
    {
        return new Dictionary<string, object>
        {
            [SlotData.DLC_STRANGER_SINS_KEY] = true,
            [SlotData.DLC_GAME_OF_CRONE_KEY] = true,
            [SlotData.DLC_BETTER_SAVE_SOUL_KEY] = true,
        };
    }
}
