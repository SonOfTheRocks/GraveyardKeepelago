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
    public float SwitchHeight { get; }
    public float SwitchWidth => SwitchHeight * 2;
    public float KnobSize => SwitchHeight * 0.75f;
    public float KnobOffX => SwitchHeight / 2;
    public float KnobOnX => SwitchWidth - SwitchHeight / 2;
    public float StateLeft => SwitchWidth + Inset * 2;

    public NativeControlGeometry(float width, float height, float fontSize, float scaleX, float scaleY)
    {
        if (!(width > 0) || !(height > 0) || !(fontSize > 0) || !(scaleX > 0) || !(scaleY > 0))
            throw new ArgumentOutOfRangeException(nameof(width), "Native control dimensions and scales must be positive.");
        Width = width / scaleX;
        Height = height / scaleY;
        Inset = Math.Min((float)Math.Round(Height / 5), (float)Math.Floor(Width / 8));
        SwitchHeight = Math.Min(fontSize, Height);
    }
}
