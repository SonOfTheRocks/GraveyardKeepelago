using System;

namespace GraveyardKeepelago.GameModifications.UI;

public sealed class NpcReceiptLayout
{
    private const int OuterPadding = 6, FramePadding = 4, TextGap = 8;
    public int RowWidth { get; }
    public int RowHeight { get; }
    public int FrameWidth { get; }
    public int FrameHeight { get; }
    public float FrameCenterX { get; }
    public float TextLeft { get; }
    public int TextWidth { get; }

    public NpcReceiptLayout(int rowWidth, int originalHeight, int portraitWidth, int portraitHeight)
    {
        if (portraitWidth <= 0 || portraitHeight <= 0 || originalHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(portraitWidth));
        RowWidth = rowWidth;
        FrameWidth = portraitWidth + 2 * FramePadding;
        FrameHeight = portraitHeight + 2 * FramePadding;
        RowHeight = Math.Max(originalHeight, FrameHeight + 2 * OuterPadding);
        TextWidth = rowWidth - 2 * OuterPadding - FrameWidth - TextGap;
        if (TextWidth <= 0) throw new ArgumentOutOfRangeException(nameof(rowWidth));
        FrameCenterX = -rowWidth / 2f + OuterPadding + FrameWidth / 2f;
        TextLeft = -rowWidth / 2f + OuterPadding + FrameWidth + TextGap;
    }
}
