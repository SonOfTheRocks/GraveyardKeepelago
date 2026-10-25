using System;
using System.Linq;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

// Resolve only after GUIElements.Init: the inactive item-count dialog is still a valid prototype.
internal sealed class NativeUiPrototypeCatalog
{
    public UILabel Label { get; }
    public UIInput Input { get; }
    public UIToggle Toggle { get; }

    public NativeUiPrototypeCatalog(DialogGUI dialog)
    {
        Label = dialog.label_1;
        var slider = GUIElements.me.item_count?.GetComponentInChildren<SmartSlider>(true);
        Input = slider?.GetComponentInChildren<UIInput>(true);
        Toggle = GUIElements.me.GetComponentsInChildren<UIToggle>(true)
            .FirstOrDefault(t => t.GetComponent<Collider2D>() != null &&
                                 t.GetComponentInChildren<UILabel>(true) != null);
        if (Label == null || Input == null || Input.label == null ||
            Input.GetComponent<Collider2D>() == null)
            throw new InvalidOperationException(
                "Native UI prototypes missing: expected DialogGUI.label_1 and ItemCountGUI/SmartSlider/UIInput with label and Collider2D.");
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
        return label;
    }
}
