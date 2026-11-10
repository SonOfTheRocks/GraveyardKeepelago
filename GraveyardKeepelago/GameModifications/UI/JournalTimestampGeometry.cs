using System;

namespace GraveyardKeepelago.GameModifications.UI;

public static class JournalTimestampGeometry
{
    // Bitmap digits are proportional: measuring 00:00 does not cover every HH:mm value.
    public static float RequiredWidth(Func<string, float> measure)
    {
        var width = 0f;
        for (var hour = 0; hour < 24; hour++)
        for (var minute = 0; minute < 60; minute++)
            width = Math.Max(width, measure(hour.ToString("D2") + ":" + minute.ToString("D2")));
        return (float)Math.Ceiling(width) + 2; // Glyph outline and pixel rounding.
    }
}
