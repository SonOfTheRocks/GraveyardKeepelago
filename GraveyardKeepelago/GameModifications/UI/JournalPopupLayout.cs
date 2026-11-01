using System;

namespace GraveyardKeepelago.GameModifications.UI;

public sealed class JournalPopupLayout
{
    public JournalRect Bounds { get; }
    public float RowHeight { get; }
    public float Padding { get; }
    public float BorderThickness { get; }
    public JournalRect InnerFill { get; }
    public int VisibleRows { get; }
    public JournalPopupLayout(JournalLayout layout, float line)
    {
        RowHeight = line * 1.5f;
        Padding = line * 0.5f;
        BorderThickness = Math.Min(2, Padding / 2);
        var bottom = layout.Input.Top + layout.Gap;
        var available = layout.History.Top - bottom;
        VisibleRows = Math.Max(1, Math.Min(6, (int)Math.Floor((available - 3 * line) / RowHeight)));
        var height = VisibleRows * RowHeight + 3 * line;
        Bounds = new JournalRect(layout.Input.X, bottom + height, layout.Input.Width, height);
        InnerFill = new JournalRect(BorderThickness, -BorderThickness,
            Bounds.Width - BorderThickness * 2, Bounds.Height - BorderThickness * 2);
    }
}
