using System;

namespace GraveyardKeepelago.GameModifications.UI;

public static class JournalHudBounds
{
    public static (float X, float Y) Correction(float left, float bottom, float right, float top)
    {
        const float margin = 0.02f;
        return (Shift(left, right, margin), Shift(bottom, top, margin));
    }

    private static float Shift(float min, float max, float margin) =>
        max > 1 - margin ? 1 - margin - max : Math.Max(0, margin - min);
}
