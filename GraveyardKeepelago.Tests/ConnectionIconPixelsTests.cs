using GraveyardKeepelago.GameModifications.UI;

namespace GraveyardKeepelago.Tests;

public class ConnectionIconPixelsTests
{
    [Fact]
    public void DisconnectedIconIsGrayscaleAndRetainsTransparency()
    {
        byte[] source = [255, 0, 0, 128, 0, 255, 0, 255, 0, 0, 255, 0];
        var result = ConnectionIconPixels.ForConnectionState(source, false);
        for (var i = 0; i < result.Length; i += 4)
        {
            Assert.Equal(result[i], result[i + 1]);
            Assert.Equal(result[i], result[i + 2]);
            Assert.Equal(source[i + 3], result[i + 3]);
        }
        Assert.True(result[4] > result[0]);
        Assert.True(result[0] > result[8]);
        Assert.Equal(255, source[0]); // Switching appearance must not mutate the full-color resource.
        Assert.Equal(0, source[1]);
    }

    [Fact]
    public void ReconnectingRestoresTheOriginalColorsAfterDisconnectedRendering()
    {
        byte[] source = [210, 35, 90, 200, 12, 80, 230, 255];
        var disconnected = ConnectionIconPixels.ForConnectionState(source, false);
        var connected = ConnectionIconPixels.ForConnectionState(source, true);
        Assert.Equal(source, connected);
        Assert.NotEqual(disconnected, connected);
        Assert.NotSame(source, connected);
    }

    [Fact]
    public void GrayAndTransparentPixelsKeepTheirOriginalValues()
    {
        byte[] source = [0, 0, 0, 0, 255, 255, 255, 255, 127, 127, 127, 80];
        Assert.Equal(source, ConnectionIconPixels.ForConnectionState(source, false));
    }

    [Fact]
    public void IncompletePixelIsRejected()
    {
        Assert.Throws<ArgumentException>(() => ConnectionIconPixels.ForConnectionState([1, 2, 3], false));
    }
}
