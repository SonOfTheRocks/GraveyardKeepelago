using System;

namespace GraveyardKeepelago.GameModifications.UI;

public sealed class NativeTitleGeometry
{
    public float BandWidth { get; }
    public float TextWidth { get; }

    public NativeTitleGeometry(float textWidth, float nativeBandWidth, float padding, float frameWidth)
    {
        if (!(textWidth > 0) || !(nativeBandWidth > 0) || padding < 0 || !(frameWidth >= textWidth))
            throw new ArgumentOutOfRangeException(nameof(textWidth), "Native title must fit its frame without shrinking the bitmap font.");
        TextWidth = textWidth;
        BandWidth = Math.Min(frameWidth, Math.Max(nativeBandWidth, textWidth + 2 * padding));
    }
}
