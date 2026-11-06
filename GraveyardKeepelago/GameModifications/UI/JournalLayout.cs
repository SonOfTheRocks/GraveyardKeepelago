using System;

namespace GraveyardKeepelago.GameModifications.UI;

// Shared, non-overlapping bounds in native font units. Y denotes the top edge.
public readonly struct JournalRect
{
    public float X { get; }
    public float Top { get; }
    public float Width { get; }
    public float Height { get; }
    public float Right => X + Width;
    public float Bottom => Top - Height;
    public float CenterX => X + Width / 2;
    public float CenterY => Top - Height / 2;

    public JournalRect(float x, float top, float width, float height)
    { X = x; Top = top; Width = width; Height = height; }
}

public sealed class JournalLayout
{
    public float Padding { get; }
    public float Gap { get; }
    public float ControlHeight { get; }
    public JournalRect[] Filters { get; }
    public JournalRect OtherItems { get; }
    public JournalRect History { get; }
    public JournalRect Scrollbar { get; }
    public JournalRect Status { get; }
    public JournalRect Latest { get; }
    public JournalRect Input { get; }
    public JournalRect Send { get; }
    public JournalRect Help { get; }
    public float FooterDividerY { get; }
    public float TimeWidth { get; }
    public float TimeRight => History.X + TimeWidth;
    public float BodyLeft => TimeRight + Gap;
    public float BodyWidth => History.Right - BodyLeft;
    public float MessageGap { get; }

    public JournalLayout(float width, float height, float line, float timeWidth)
    {
        foreach (var value in new[] { width, height, line, timeWidth })
            if (!(value > 0) || float.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(width));
        if (width < 35 * line || height < 15 * line || timeWidth > 6 * line)
            throw new ArgumentOutOfRangeException(nameof(width), "Journal content area is too small.");
        Padding = line * 0.75f;
        Gap = line * 0.5f;
        ControlHeight = line * 1.75f;
        MessageGap = line * 0.35f;
        TimeWidth = timeWidth;
        var left = -width / 2 + Padding;
        var right = width / 2 - Padding;
        var contentWidth = right - left;
        var top = height / 2 - Padding;
        var otherWidth = line * 10.5f;
        var filterWidth = (contentWidth - otherWidth - Gap * 4) / 4;
        Filters = new JournalRect[4];
        for (var i = 0; i < Filters.Length; i++)
            Filters[i] = new JournalRect(left + i * (filterWidth + Gap), top, filterWidth, ControlHeight);
        OtherItems = new JournalRect(right - otherWidth, top, otherWidth, ControlHeight);

        Help = new JournalRect(left, -height / 2 + Padding + line, contentWidth, line);
        var inputTop = Help.Top + Gap + ControlHeight;
        var actionWidth = line * 8;
        Send = new JournalRect(right - actionWidth, inputTop, actionWidth, ControlHeight);
        Input = new JournalRect(left, inputTop, Send.X - Gap - left, ControlHeight);
        Latest = new JournalRect(Send.X, Input.Top + Gap + ControlHeight, actionWidth, ControlHeight);
        Status = new JournalRect(left, Latest.CenterY + line / 2, Input.Width, line);
        FooterDividerY = Latest.Top + Gap;
        var historyTop = Filters[0].Bottom - Gap;
        var historyHeight = historyTop - FooterDividerY - Gap;
        var scrollbarWidth = line * 0.65f;
        Scrollbar = new JournalRect(right - scrollbarWidth, historyTop, scrollbarWidth, historyHeight);
        History = new JournalRect(left, historyTop, contentWidth - scrollbarWidth - Gap, historyHeight);
    }
}
