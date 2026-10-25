using System;
using System.Collections.Generic;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

internal sealed class NativeFormLayout
{
    private readonly UILabel _anchor;
    private readonly List<Action<float, float, float, float>> _rows = new();
    public NativeFormLayout(UILabel anchor) => _anchor = anchor;

    public void Add(UILabel caption, Action<float, float, float, float> placeControl)
    {
        _rows.Add((x, y, width, line) =>
        {
            caption.transform.localPosition = new Vector3(x, y, 0);
            caption.width = Mathf.RoundToInt(width);
            caption.height = Mathf.CeilToInt(line);
            placeControl(x, y - line, width, line * 1.5f);
        });
    }

    public void Reposition()
    {
        // The owner mirrors label_1's native transform, outside the UITable's children.
        // Its geometry follows the native dialog, including UI scale and anchor updates.
        var topLeft = _anchor.localCorners[1];
        var line = Mathf.Max(1, _anchor.fontSize + _anchor.spacingY);
        for (var i = 0; i < _rows.Count; i++)
            _rows[i](topLeft.x, topLeft.y - (2 + i * 3) * line, _anchor.width, line);
    }
}
