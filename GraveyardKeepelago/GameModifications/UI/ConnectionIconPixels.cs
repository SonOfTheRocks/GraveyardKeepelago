using System;

namespace GraveyardKeepelago.GameModifications.UI;

public static class ConnectionIconPixels
{
    public static byte[] ForConnectionState(byte[] rgba, bool connected)
    {
        if (rgba == null || rgba.Length % 4 != 0)
            throw new ArgumentException("Expected RGBA pixels.", nameof(rgba));
        var result = (byte[])rgba.Clone();
        if (connected)
            return result;
        for (var i = 0; i < result.Length; i += 4)
        {
            var gray = (byte)((77 * rgba[i] + 150 * rgba[i + 1] + 29 * rgba[i + 2]) >> 8);
            result[i] = result[i + 1] = result[i + 2] = gray;
        }
        return result;
    }
}
