using System;

namespace GraveyardKeepelago.GameModifications.UI;

public static class BuffResearchIconGeometry
{
    // Match native buff rendering's pixel size, capped by the technology icon's existing box.
    public static (float Width, float Height, float X, float Y) Fit(float boxWidth, float boxHeight,
        float spriteWidth, float spriteHeight, float pixelSize, float pivotX, float pivotY)
    {
        if (!Positive(boxWidth) || !Positive(boxHeight) || !Positive(spriteWidth) || !Positive(spriteHeight) || !Positive(pixelSize))
            return (boxWidth, boxHeight, 0, 0);
        var width = spriteWidth * pixelSize;
        var height = spriteHeight * pixelSize;
        var scale = Math.Min(1, Math.Min(boxWidth / width, boxHeight / height));
        width = Math.Max(1, (float)Math.Floor(width * scale));
        height = Math.Max(1, (float)Math.Floor(height * scale));
        return (width, height, (0.5f - pivotX) * (boxWidth - width), (0.5f - pivotY) * (boxHeight - height));
    }

    private static bool Positive(float value) => value > 0 && !float.IsInfinity(value) && !float.IsNaN(value);
}
