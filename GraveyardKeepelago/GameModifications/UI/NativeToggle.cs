using System;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

internal sealed class NativeToggle
{
    public GameObject GameObject { get; }
    private readonly NativeToggleModel _model = new();
    private readonly UILabel _label;
    private readonly UI2DSprite _background, _left, _right;
    private readonly UIButton _leftButton, _rightButton;
    private readonly BoxCollider2D _focusCollider;
    private readonly float _backgroundOpacity;
    private readonly float _backgroundHeight;
    private readonly float _textOriginY;
    private readonly float _arrowSize;
    public event Action<bool> Changed;

    public NativeToggle(NativeUiPrototypeCatalog prototypes, Transform parent, int depth)
    {
        GameObject = NativeUiPrototypeCatalog.Clone(prototypes.BooleanSelector, parent, "Native boolean selector");
        GameObject.AddComponent<NativeBooleanSelectorMarker>();
        _label = GameObject.transform.Find("label")?.GetComponent<UILabel>();
        _background = GameObject.transform.Find("back frame")?.GetComponent<UI2DSprite>();
        _left = GameObject.transform.Find("dec")?.GetComponent<UI2DSprite>();
        _right = GameObject.transform.Find("inc")?.GetComponent<UI2DSprite>();
        if (_label == null || _background == null || _left == null || _right == null)
            throw new InvalidOperationException("Native options selector must contain label, back frame, dec and inc.");
        _backgroundOpacity = _background.alpha;
        _backgroundHeight = _background.drawingDimensions.w - _background.drawingDimensions.y;
        var drawnBackground = _background.drawingDimensions;
        var backgroundCenterY = _background.transform.localPosition.y +
            (drawnBackground.y + drawnBackground.w) / 2;
        _textOriginY = NativeControlGeometry.SelectorTextOriginY(
            _label.transform.localPosition.y, backgroundCenterY);
        var sourceLeft = prototypes.BooleanSelector.transform.Find("dec").GetComponent<UIButton>();
        var sourceRight = prototypes.BooleanSelector.transform.Find("inc").GetComponent<UIButton>();
        _arrowSize = _left.drawingDimensions.z - _left.drawingDimensions.x;
        CenterArrowText(_left);
        CenterArrowText(_right);
        _leftButton = AddButton(_left, sourceLeft);
        _rightButton = AddButton(_right, sourceRight);
        // Keep the native pivot and baseline: centering the widget changes the bitmap text's baseline.
        _label.overflowMethod = UILabel.Overflow.ClampContent;
        _background.depth = depth;
        _label.depth = depth + 1;
        GameObject.GetComponent<UIWidget>().depth = depth + 1;
        foreach (var arrow in new[] { _left, _right })
        {
            arrow.depth = depth + 2;
            foreach (var label in arrow.GetComponentsInChildren<UILabel>(true))
                label.depth = depth + 3;
        }
        _focusCollider = GameObject.AddComponent<BoxCollider2D>();
        _focusCollider.isTrigger = true;
        var listener = UIEventListener.Get(GameObject);
        listener.onClick = _ => _model.Toggle();
        listener.onKey = (_, key) =>
        {
            if (key == KeyCode.LeftArrow || key == KeyCode.RightArrow)
                _model.Toggle();
            else if (key == KeyCode.Escape)
                UICamera.selectedObject = null;
        };
        _model.Changed += value => { Refresh(); Changed?.Invoke(value); };
        Refresh();
    }

    private UIButton AddButton(UI2DSprite arrow, UIButton prototype)
    {
        if (prototype == null)
            throw new InvalidOperationException("Native selector arrow has no UIButton prototype.");
        var button = arrow.gameObject.AddComponent<UIButton>();
        button.tweenTarget = arrow.gameObject;
        button.defaultColor = prototype.defaultColor;
        button.hover = prototype.hover;
        button.pressed = prototype.pressed;
        button.disabledColor = prototype.disabledColor;
        button.duration = prototype.duration;
        button.hoverSprite2D = prototype.hoverSprite2D;
        button.pressedSprite2D = prototype.pressedSprite2D;
        button.disabledSprite2D = prototype.disabledSprite2D;
        button.onClick.Add(new EventDelegate((EventDelegate.Callback)(() =>
        {
            _model.Toggle();
            UICamera.selectedObject = GameObject;
        })));
        return button;
    }

    private static void CenterArrowText(UI2DSprite arrow)
    {
        var drawn = arrow.drawingDimensions;
        var center = new Vector3((drawn.x + drawn.z) / 2, (drawn.y + drawn.w) / 2, 0);
        foreach (var label in arrow.GetComponentsInChildren<UILabel>(true))
            label.transform.localPosition -= center;
    }

    public bool Value
    {
        get => _model.Value;
        set { _model.Value = value; Refresh(); }
    }

    private void Refresh() => _label.text = _model.DisplayText;

    public void SetEditable(bool editable)
    {
        _model.Editable = editable;
        _focusCollider.enabled = editable;
        _leftButton.isEnabled = _rightButton.isEnabled = editable;
        _label.alpha = editable ? 1f : 0.55f;
        _background.alpha = _backgroundOpacity * (editable ? 1f : 0.55f);
    }

    public void Place(float x, float y, float width, float height)
    {
        var scale = GameObject.transform.localScale;
        var bounds = new NativeControlGeometry(width, height, _label.fontSize,
            Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        GameObject.transform.localPosition = new Vector3(x + width / 2, y - height / 2, 0);
        var root = GameObject.GetComponent<UIWidget>();
        root.pivot = UIWidget.Pivot.Center;
        root.width = Mathf.RoundToInt(bounds.Width);
        root.height = Mathf.RoundToInt(bounds.Height);
        NativeSpriteFrame.Fit(_background, bounds.Width,
            bounds.SelectorBackgroundHeight(_backgroundHeight), Vector2.zero);
        NativeSpriteFrame.Fit(_left, _arrowSize, _arrowSize,
            new Vector2(bounds.LeftArrowCenter(_arrowSize), 0));
        NativeSpriteFrame.Fit(_right, _arrowSize, _arrowSize,
            new Vector2(bounds.RightArrowCenter(_arrowSize), 0));
        _label.width = Mathf.RoundToInt(bounds.SelectorTextWidth(_arrowSize));
        var textCorners = _label.localCorners;
        _label.transform.localPosition = new Vector3(
            -(textCorners[0].x + textCorners[2].x) / 2, _textOriginY, 0);
        _focusCollider.size = new Vector2(bounds.Width, bounds.Height);
        _focusCollider.offset = Vector2.zero;
    }
}

internal sealed class NativeBooleanSelectorMarker : MonoBehaviour { }
