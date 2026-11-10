using System;

namespace GraveyardKeepelago.GameModifications.UI;

public static class QualityBadgeGeometry
{
    public static bool IsTrapBadge(string spriteName) => spriteName == "i_skull" || spriteName == "i_skull(Clone)";
    public static bool IsTrapBadge(bool apActive, string spriteName) => apActive && IsTrapBadge(spriteName);

    public static (float Left, float Bottom, float Right, float Top) StarReference(float left, float bottom, float right,
        float top, float nativeWidth, float nativeHeight, float pixelSize)
    {
        if (!Positive(nativeWidth) || !Positive(nativeHeight) || !Positive(pixelSize)) return (left, bottom, right, top);
        // Some quality widgets stretch the star's canvas. Never enlarge the skull to that stretched size.
        var width = Math.Min(right - left, nativeWidth * pixelSize);
        var height = Math.Min(top - bottom, nativeHeight * pixelSize);
        return (right - width, top - height, right, top);
    }

    // Compare the rendered, trimmed rectangles. Packed sprites can share a large canvas.
    public static (float Scale, float X, float Y) FitRendered(float left, float bottom, float right, float top,
        float starLeft, float starBottom, float starRight, float starTop)
    {
        if (!Positive(right - left) || !Positive(top - bottom) || !Positive(starRight - starLeft) || !Positive(starTop - starBottom))
            return (1, 0, 0);
        var scale = Math.Min(1, Math.Min((starRight - starLeft) / (right - left), (starTop - starBottom) / (top - bottom)));
        return (scale, starRight - right * scale, starTop - top * scale);
    }

    // Fit the existing widget inside the native star's pixel-size box, keeping
    // its top-right corner fixed for any NGUI pivot. Scale uniformly so a skull
    // retains its shape. Inputs always come from the unmodified widget state.
    public static (float Scale, float X, float Y) Fit(float width, float height,
        float starWidth, float starHeight, float pivotX, float pivotY)
    {
        if (!Positive(width) || !Positive(height) || !Positive(starWidth) || !Positive(starHeight))
            return (1, 0, 0);
        var scale = Math.Min(1f, Math.Min(starWidth / width, starHeight / height));
        return (scale, (1 - pivotX) * width * (1 - scale), (1 - pivotY) * height * (1 - scale));
    }

    private static bool Positive(float value) => value > 0 && !float.IsInfinity(value) && !float.IsNaN(value);

    public static (float Width, float Height, float X, float Y) NativeSkullBounds(float boxWidth, float boxHeight,
        float spriteWidth, float spriteHeight, float pixelSize, float pivotX, float pivotY)
    {
        if (!Positive(boxWidth) || !Positive(boxHeight) || !Positive(spriteWidth) || !Positive(spriteHeight) || !Positive(pixelSize))
            return (boxWidth, boxHeight, 0, 0);
        var width = spriteWidth * pixelSize;
        var height = spriteHeight * pixelSize;
        var scale = Math.Min(1, Math.Min(boxWidth / width, boxHeight / height));
        width = Math.Max(1, (float)Math.Floor(width * scale));
        height = Math.Max(1, (float)Math.Floor(height * scale));
        return (width, height, (1 - pivotX) * (boxWidth - width), (1 - pivotY) * (boxHeight - height));
    }
}
