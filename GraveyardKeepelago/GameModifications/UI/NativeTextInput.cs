using System;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

internal sealed class NativeTextInput
{
    public UIInput Input { get; }
    public GameObject GameObject => Input.gameObject;
    private readonly Collider2D _collider;
    private readonly UI2DSprite[] _backgrounds;
    private readonly PolygonColliderAutoSize _autoSize;

    public NativeTextInput(NativeUiPrototypeCatalog prototypes, Transform parent, bool password, int depth)
    {
        var go = NativeUiPrototypeCatalog.Clone(prototypes.Input.gameObject, parent, "Native text input");
        Input = go.GetComponent<UIInput>();
        _collider = go.GetComponent<Collider2D>();
        _autoSize = go.GetComponent<PolygonColliderAutoSize>();
        _backgrounds = go.GetComponentsInChildren<UI2DSprite>(true);
        foreach (var background in _backgrounds)
        {
            // Native counter backgrounds are children of the input label.
            background.transform.SetParent(go.transform, false);
            background.transform.localScale = Vector3.one;
        }
        if (Input.label == null || !Input.label.transform.IsChildOf(go.transform))
            throw new InvalidOperationException("Native input label must belong to the cloned input subtree.");
        Input.onChange.Clear();
        Input.onSubmit.Clear();
        Input.onSubmit.Add(new EventDelegate((EventDelegate.Callback)(() =>
        {
            LazyInput.ClearKeyDown(GameKey.Select);
            Input.isSelected = false;
        })));
        Input.onValidate = null;
        Input.validation = UIInput.Validation.None;
        Input.keyboardType = UIInput.KeyboardType.Default;
        Input.inputType = password ? UIInput.InputType.Password : UIInput.InputType.Standard;
        Input.onReturnKey = UIInput.OnReturnKey.Submit;
        Input.characterLimit = 0;
        Input.savedAs = string.Empty; // ConnectionSettings owns persistence, especially the password.
        Input.enabled = true;
        Input.label.supportEncoding = false;
        Input.label.maxLineCount = 1;
        Input.label.overflowMethod = UILabel.Overflow.ClampContent;
        Input.label.pivot = UIWidget.Pivot.Left;
        Input.label.alignment = NGUIText.Alignment.Left;
        foreach (var background in _backgrounds)
            background.depth = depth;
        Input.label.depth = depth + 1;
        Input.defaultText = string.Empty;
    }

    public string Value { get => Input.value; set => Input.Set(value ?? string.Empty, false); }

    public void SetEditable(bool editable)
    {
        if (!editable)
            Input.isSelected = false;
        Input.enabled = editable;
        _collider.enabled = editable;
        Input.label.alpha = editable ? 1f : 0.55f;
        foreach (var background in _backgrounds)
            background.alpha = editable ? 1f : 0.55f;
    }

    public void Place(float x, float y, float width, float height)
    {
        // Preserve the prototype's natural font size, expressed in the form's native coordinates.
        var scale = GameObject.transform.localScale;
        var bounds = new NativeControlGeometry(width, height, Input.label.fontSize,
            Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        GameObject.transform.localPosition = new Vector3(x + bounds.Inset * scale.x, y - height / 2f, 0);
        Input.label.width = Mathf.RoundToInt(bounds.TextWidth);
        Input.label.height = Mathf.RoundToInt(bounds.Height);
        foreach (var background in _backgrounds)
        {
            NativeSpriteFrame.Fit(background, bounds.Width, bounds.Height,
                new Vector2(bounds.Width / 2 - bounds.Inset, 0));
        }
        if (_autoSize != null)
        {
            _autoSize.padding = _autoSize.offset = 0;
            _autoSize.shift = Vector2.zero;
            _autoSize.expand = new Vector2(bounds.Inset, 0);
        }
        NGUITools.UpdateWidgetCollider(GameObject, true);
    }
}
