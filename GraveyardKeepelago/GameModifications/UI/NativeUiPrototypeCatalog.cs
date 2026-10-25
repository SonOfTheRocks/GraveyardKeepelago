using System;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

// Resolve only after GUIElements.Init: the inactive item-count dialog is still a valid prototype.
internal sealed class NativeUiPrototypeCatalog
{
    public UILabel Label { get; }
    public UILabel RegularText { get; }
    public UIInput Input { get; }
    public GameObject BooleanSelector { get; }
    public UILabel OptionsTitle { get; }
    public UI2DSprite OptionsFrame { get; }
    public UI2DSprite DialogFrame { get; }
    public UI2DSprite SelectionBackground { get; }

    public NativeUiPrototypeCatalog(DialogGUI dialog)
    {
        // Runtime-inspected prefab paths. Clone the whole title label, including its native band.
        var options = GUIElements.me.options;
        // Runtime-inspected regular small_font; the ItemCount input uses small_font_bold.
        RegularText = dialog.label_1;
        // Field captions use menu typography, not the dimmer regular-weight dialog body text.
        Label = options?.transform.Find("content table/master/label")?.GetComponent<UILabel>();
        SelectionBackground = options?.transform.Find("content table/language/options switcher/back frame")?.GetComponent<UI2DSprite>();
        BooleanSelector = options?.transform.Find("content table/language/options switcher")?.gameObject;
        OptionsTitle = options?.transform.Find("header and back/header label")?.GetComponent<UILabel>();
        OptionsFrame = options?.transform.Find("header and back/back")?.GetComponent<UI2DSprite>();
        DialogFrame = dialog.transform.Find("window/header and back/back")?.GetComponent<UI2DSprite>();
        var slider = GUIElements.me.item_count?.GetComponentInChildren<SmartSlider>(true);
        Input = slider?.GetComponentInChildren<UIInput>(true);
        if (Label == null || SelectionBackground == null || BooleanSelector == null || Input == null || Input.label == null ||
            Input.GetComponent<Collider2D>() == null)
            throw new InvalidOperationException(
                "Native UI prototypes missing: expected Options menu caption and selection background, and ItemCountGUI/SmartSlider/UIInput with label and Collider2D.");
        if (RegularText == null || RegularText.bitmapFont == null || RegularText.bitmapFont == Label.bitmapFont ||
            RegularText.fontStyle != FontStyle.Normal)
            throw new InvalidOperationException("Native regular text prototype must provide a normal bitmap font distinct from bold captions.");
        if (OptionsTitle == null || OptionsFrame == null || DialogFrame == null ||
            OptionsTitle.GetComponentInChildren<UI2DSprite>(true) == null)
            throw new InvalidOperationException("Native UI title prototypes missing: expected Options menu/header and back/header label with band and both native window frames.");
    }

    public static GameObject Clone(GameObject source, Transform parent, string name)
    {
        if (parent.gameObject.activeInHierarchy)
            throw new InvalidOperationException("Native prototypes must be cloned into an inactive form.");
        var clone = UnityEngine.Object.Instantiate(source, parent, false);
        clone.name = name;
        var sourceScale = source.transform.lossyScale;
        var parentScale = parent.lossyScale;
        clone.transform.localScale = new Vector3(sourceScale.x / parentScale.x,
            sourceScale.y / parentScale.y, sourceScale.z / parentScale.z);
        foreach (var transform in clone.GetComponentsInChildren<Transform>(true))
            transform.gameObject.layer = parent.gameObject.layer;
        // Never retain localization, animation, or serialized event receivers from the source form.
        foreach (var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour is UIWidget || behaviour is UIInput || behaviour is UIToggle ||
                behaviour is PolygonColliderAutoSize)
                continue;
            behaviour.enabled = false;
            UnityEngine.Object.DestroyImmediate(behaviour);
        }
        foreach (var widget in clone.GetComponentsInChildren<UIWidget>(true))
            widget.SetAnchor((Transform)null);
        clone.SetActive(true);
        return clone;
    }

    public UILabel CloneLabel(Transform parent, string text, int depth)
    {
        var go = Clone(Label.gameObject, parent, text);
        foreach (var collider in go.GetComponentsInChildren<Collider2D>(true))
            collider.enabled = false;
        var label = go.GetComponent<UILabel>();
        label.transform.localScale = Vector3.one;
        label.transform.localRotation = Quaternion.identity;
        label.overflowMethod = UILabel.Overflow.ClampContent;
        label.pivot = UIWidget.Pivot.TopLeft;
        label.alignment = NGUIText.Alignment.Left;
        label.depth = depth;
        label.text = text;
        ApplyMenuTextPalette(label);
        return label;
    }

    public void ApplyMenuTextPalette(UILabel label)
    {
        label.color = Label.color;
        label.applyGradient = Label.applyGradient;
        label.gradientTop = Label.gradientTop;
        label.gradientBottom = Label.gradientBottom;
        label.effectColor = Label.effectColor;
    }

    public void ApplyInputTypography(UILabel label)
    {
        label.bitmapFont = RegularText.bitmapFont;
        label.fontStyle = FontStyle.Normal;
        // A bitmap font's weight is part of the asset, not just UILabel.fontStyle.
        if (label.bitmapFont != RegularText.bitmapFont || label.bitmapFont == Label.bitmapFont ||
            label.fontStyle != FontStyle.Normal)
            throw new InvalidOperationException("Native input must use the regular font while captions remain bold.");
    }

    public void VerifyCaptionAppearance(UILabel caption)
    {
        if (caption.bitmapFont != Label.bitmapFont || caption.trueTypeFont != Label.trueTypeFont ||
            caption.fontSize != Label.fontSize || caption.fontStyle != Label.fontStyle ||
            caption.color != Label.color || caption.effectStyle != Label.effectStyle ||
            caption.effectColor != Label.effectColor || caption.applyGradient != Label.applyGradient ||
            caption.gradientTop != Label.gradientTop || caption.gradientBottom != Label.gradientBottom ||
            caption.effectDistance != Label.effectDistance ||
            caption.spacingX != Label.spacingX || caption.spacingY != Label.spacingY ||
            caption.overflowMethod != UILabel.Overflow.ClampContent)
            throw new InvalidOperationException($"Native caption '{caption.text}' no longer matches the Options menu's typography.");
    }
}
