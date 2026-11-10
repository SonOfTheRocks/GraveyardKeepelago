using GraveyardKeepelago.Logic;

namespace GraveyardKeepelago.Tests;

public class LocationHandlerTests
{
    [Theory]
    [InlineData("Persistence")]
    [InlineData("Industriousness")]
    [InlineData("Eloquence")]
    public void GameOfCroneEndingTechsMapToOneSharedLocation(string techId)
    {
        Assert.Equal(
            LocationHandler.GameOfCroneEndingLocation,
            LocationHandler.GetSharedRewardLocationForTech(techId));
    }

    [Theory]
    [InlineData("Faith")]
    [InlineData("Advanced gravestones")]
    [InlineData("")]
    public void OtherTechsDoNotMapToTheGameOfCroneEndingLocation(string techId)
    {
        Assert.Null(LocationHandler.GetSharedRewardLocationForTech(techId));
    }
}
