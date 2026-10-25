using System;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

internal sealed class NativeModalTitle
{
    private readonly UILabel _label;
    private readonly UI2DSprite _band;
    private readonly UI2DSprite _frame;
    private readonly Vector2 _offsetFromFrameTop;
    private readonly float _nativeBandWidth, _bandHeight;
    private readonly Vector3 _bandOffset;

    public NativeModalTitle(NativeUiPrototypeCatalog prototypes, Transform parent, string title, int depth)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("A modal title is required.", nameof(title));
        _frame = prototypes.DialogFrame;
        var original = prototypes.OptionsTitle;
        var sourceFrame = prototypes.OptionsFrame;
        var sourceBounds = sourceFrame.drawingDimensions;
        var originalPosition = sourceFrame.transform.InverseTransformPoint(original.transform.position);
        _offsetFromFrameTop = new Vector2(
            originalPosition.x - (sourceBounds.x + sourceBounds.z) / 2,
            originalPosition.y - sourceBounds.w);
        var go = NativeUiPrototypeCatalog.Clone(original.gameObject, parent, "Native modal title");
        _label = go.GetComponent<UILabel>();
        prototypes.ApplyMenuTextPalette(_label);
        _band = go.GetComponentInChildren<UI2DSprite>(true);
        var bandBounds = _band.drawingDimensions;
        _nativeBandWidth = bandBounds.z - bandBounds.x;
        _bandHeight = bandBounds.w - bandBounds.y;
        _bandOffset = _band.transform.localPosition + new Vector3(
            (bandBounds.x + bandBounds.z) / 2, (bandBounds.y + bandBounds.w) / 2, 0);
        _band.depth = depth;
        _label.depth = depth + 1;
        _label.pivot = UIWidget.Pivot.Center;
        _label.alignment = NGUIText.Alignment.Center;
        _label.supportEncoding = false;
        _label.maxLineCount = 1;
        var nativeHeight = _label.height;
        // Measure at the native font size; no transform or ShrinkContent scaling.
        _label.overflowMethod = UILabel.Overflow.ResizeFreely;
        _label.text = title;
        _label.ProcessText();
        _label.width = Mathf.CeilToInt(_label.printedSize.x);
        _label.height = nativeHeight;
        _label.overflowMethod = UILabel.Overflow.ClampContent;
    }

    public void Reposition()
    {
        var frameBounds = _frame.drawingDimensions;
        var localTop = new Vector3((frameBounds.x + frameBounds.z) / 2 + _offsetFromFrameTop.x,
            frameBounds.w + _offsetFromFrameTop.y, 0);
        var parent = _label.transform.parent;
        var position = parent.InverseTransformPoint(_frame.transform.TransformPoint(localTop));
        _label.transform.localPosition = new Vector3(Mathf.Round(position.x), Mathf.Round(position.y), position.z);
        var worldWidth = _frame.transform.TransformVector(new Vector3(frameBounds.z - frameBounds.x, 0, 0));
        var titleWidth = Mathf.Abs(_label.transform.InverseTransformVector(worldWidth).x);
        var geometry = new NativeTitleGeometry(_label.width, _nativeBandWidth, _label.fontSize, titleWidth);
        NativeSpriteFrame.Fit(_band, geometry.BandWidth, _bandHeight, _bandOffset);
    }
}
