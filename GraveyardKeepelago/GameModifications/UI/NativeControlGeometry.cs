using System;

namespace GraveyardKeepelago.GameModifications.UI;

// Dimensions in the cloned control's coordinates; caller supplies dimensions in form coordinates.
public sealed class NativeControlGeometry
{
    public float Width { get; }
    public float Height { get; }
    public float Inset { get; }
    public float TextWidth => Width - 2 * Inset;
    public float FrameLeft => -Inset;
    public float FrameRight => Width - Inset;
    public float LeftArrowCenter(float arrowWidth) => (-Width + arrowWidth) / 2;
    public float RightArrowCenter(float arrowWidth) => (Width - arrowWidth) / 2;
    public float SelectorTextWidth(float arrowWidth) => Width - 2 * (arrowWidth + Inset);
    // Stretch the selector horizontally while preserving its native vertical proportions.
    public float SelectorBackgroundHeight(float prototypeHeight) => Math.Min(Height, prototypeHeight);
    public static float SelectorTextOriginY(float nativeTextY, float nativeFrameCenterY) =>
        nativeTextY - nativeFrameCenterY;
    public static float InlineControlOffset(float rowWidth, float captionWidth = 0, float gap = 0) =>
        Math.Max((float)Math.Round(rowWidth * 0.4f), (float)Math.Ceiling(captionWidth + gap));

    public NativeControlGeometry(float width, float height, float fontSize, float scaleX, float scaleY)
    {
        if (!(width > 0) || !(height > 0) || !(fontSize > 0) || !(scaleX > 0) || !(scaleY > 0))
            throw new ArgumentOutOfRangeException(nameof(width), "Native control dimensions and scales must be positive.");
        Width = width / scaleX;
        Height = height / scaleY;
        Inset = Math.Min((float)Math.Round(Height / 5), (float)Math.Floor(Width / 8));
    }
}
