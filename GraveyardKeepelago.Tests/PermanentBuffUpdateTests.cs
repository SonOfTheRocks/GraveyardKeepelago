using GraveyardKeepelago.GameModifications.Patches;

namespace GraveyardKeepelago.Tests;

public sealed class PermanentBuffUpdateTests
{
    [Fact]
    public void OnlyTheSavedMarkerDesignatesAPermanentBuff()
    {
        Assert.False(PermanentBuffUpdatePatch.IsPermanent(120));
        Assert.True(PermanentBuffUpdatePatch.IsPermanent(float.MaxValue));
    }
}
