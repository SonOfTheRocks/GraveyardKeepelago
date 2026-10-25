using System;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

internal sealed class NativeToggle
{
    public GameObject GameObject { get; }
    private readonly UIToggle _toggle;
    private readonly UILabel _label;
    private readonly Collider2D _collider;
    private readonly NativeSwitchVisual _switch;
    private readonly PolygonColliderAutoSize _autoSize;
    private bool _editable = true;
    public event Action<bool> Changed;

    public NativeToggle(NativeUiPrototypeCatalog prototypes, Transform parent, int depth)
    {
        if (prototypes.Toggle != null)
        {
            GameObject = NativeUiPrototypeCatalog.Clone(prototypes.Toggle.gameObject, parent, "Native toggle");
            _toggle = GameObject.GetComponent<UIToggle>();
            _label = GameObject.GetComponentInChildren<UILabel>(true);
        }
        else
        {
            // No switch is exposed by this game build. Use the proven native hit area and font.
            GameObject = NativeUiPrototypeCatalog.Clone(prototypes.Input.gameObject, parent, "Native toggle");
            var input = GameObject.GetComponent<UIInput>();
            input.enabled = false;
            input.onChange.Clear();
            input.onSubmit.Clear();
            _label = input.label;
            UnityEngine.Object.DestroyImmediate(input);
            foreach (var background in GameObject.GetComponentsInChildren<UI2DSprite>(true))
                UnityEngine.Object.DestroyImmediate(background.gameObject);
            _toggle = GameObject.AddComponent<UIToggle>();
            _switch = GameObject.AddComponent<NativeSwitchVisual>();
            _switch.Initialize(depth + 5);
            _label.alignment = NGUIText.Alignment.Left;
        }
        _collider = GameObject.GetComponent<Collider2D>();
        _autoSize = GameObject.GetComponent<PolygonColliderAutoSize>();
        _toggle.group = 0;
        _toggle.validator = null;
        _toggle.activeAnimation = null;
        _toggle.animator = null;
        _toggle.tween = null;
        _toggle.instantTween = true;
        _toggle.onChange.Clear();
        _toggle.onChange.Add(new EventDelegate((EventDelegate.Callback)(() =>
        {
            Refresh();
            Changed?.Invoke(Value);
        })));
        _label.overflowMethod = UILabel.Overflow.ClampContent;
        foreach (var widget in GameObject.GetComponentsInChildren<UIWidget>(true))
            widget.depth = depth++;
    }

    public bool Value
    {
        get => _toggle.value;
        set { _toggle.Set(value, false); Refresh(); }
    }

    private void Refresh()
    {
        _label.text = Value ? "ON" : "OFF";
        _switch?.SetState(Value, _editable);
    }

    public void SetEditable(bool editable)
    {
        _editable = editable;
        _toggle.enabled = editable;
        _collider.enabled = editable;
        _label.alpha = editable ? 1f : 0.55f;
        Refresh();
    }

    public void Place(float x, float y, float width, float height)
    {
        var scale = GameObject.transform.localScale;
        var bounds = new NativeControlGeometry(width, height, _label.fontSize,
            Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        var stateLeft = _switch == null ? 0 : bounds.StateLeft;
        GameObject.transform.localPosition = new Vector3(x + stateLeft * scale.x, y - height / 2f, 0);
        _label.pivot = UIWidget.Pivot.Left;
        _label.width = Mathf.RoundToInt(Mathf.Min(bounds.Width - stateLeft, _label.fontSize * 3));
        _label.height = Mathf.RoundToInt(bounds.Height);
        _switch?.Resize(bounds);
        if (_autoSize != null)
        {
            _autoSize.padding = _autoSize.offset = 0;
            _autoSize.shift = new Vector2(-stateLeft / 2, 0);
            _autoSize.expand = new Vector2(stateLeft / 2, 0);
        }
        NGUITools.UpdateWidgetCollider(GameObject, true);
        Refresh();
    }
}
