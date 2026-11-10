using System;
using System.Collections.Generic;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

internal sealed class NativeFormLayout
{
    private readonly UILabel _anchor;
    private readonly List<Action<float, float, float, float>> _rows = new();
    private float _captionWidth;
    private readonly float _nativeWidth;
    public int ReservedLineCount => NativeFormGeometry.StatusLineCount(_rows.Count);
    public NativeFormLayout(UILabel anchor)
    {
        _anchor = anchor;
        _nativeWidth = anchor.width;
    }

    public void Add(UILabel caption, Action<float, float, float, float> placeControl)
    {
        // Measure the full caption before clamping it to the shared column.
        caption.overflowMethod = UILabel.Overflow.ResizeFreely;
        _captionWidth = Mathf.Max(_captionWidth, Mathf.Ceil(caption.printedSize.x));
        caption.overflowMethod = UILabel.Overflow.ClampContent;
        _rows.Add((x, y, width, line) =>
        {
            caption.transform.localPosition = new Vector3(x, y, 0);
            caption.height = Mathf.CeilToInt(line);
            // Additional dialog width belongs to the controls, not the caption column.
            var controlOffset = NativeControlGeometry.InlineControlOffset(_nativeWidth, _captionWidth, line / 2);
            caption.width = Mathf.RoundToInt(controlOffset - line / 2);
            caption.alignment = NGUIText.Alignment.Right;
            placeControl(x + controlOffset, y + line / 4, width - controlOffset, line * 1.5f);
        });
    }

    public void Reposition()
    {
        // The owner mirrors label_1's native transform, outside the UITable's children.
        // Its geometry follows the native dialog, including UI scale and anchor updates.
        var topLeft = _anchor.localCorners[1];
        var line = Mathf.Max(1, _anchor.fontSize + _anchor.spacingY);
        for (var i = 0; i < _rows.Count; i++)
            _rows[i](topLeft.x, topLeft.y - NativeFormGeometry.RowTopOffset(i, line), _anchor.width, line);
    }
}
