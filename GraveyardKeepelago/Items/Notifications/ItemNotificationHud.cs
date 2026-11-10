using System;
using System.Collections.Generic;
using GraveyardKeepelago.GameModifications.UI;
using UnityEngine;

namespace GraveyardKeepelago.Items.Notifications;

// Native bitmap labels only: no focus, colliders, modal GUI or generated artwork.
public sealed class ItemNotificationHud : IDisposable
{
    private readonly GameObject _root;
    private readonly Vector3 _nativeScale;
    private readonly float[] _oldPositions = new float[ItemNotificationQueue.Capacity];
    private readonly UILabel[] _labels = new UILabel[ItemNotificationQueue.Capacity];
    private readonly ItemNotificationQueue.VisibleReceipt[] _receipts =
        new ItemNotificationQueue.VisibleReceipt[ItemNotificationQueue.Capacity];
    private readonly float[] _positions = new float[ItemNotificationQueue.Capacity];

    public ItemNotificationHud()
    {
        var hud = GUIElements.me.hud;
        _root = new GameObject("AP received items");
        _root.SetActive(false);
        _root.layer = hud.gameObject.layer;
        _root.transform.SetParent(hud.transform, false);
        var source = hud.day_label;
        var scale = source.transform.lossyScale;
        var parent = hud.transform.lossyScale;
        _nativeScale = _root.transform.localScale = new Vector3(scale.x / parent.x, scale.y / parent.y, 1);
        try
        {
            for (var i = 0; i < _labels.Length; i++)
            {
                var go = NativeUiPrototypeCatalog.Clone(source.gameObject, _root.transform, "AP receipt " + i);
                foreach (var collider in go.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
                var label = _labels[i] = go.GetComponent<UILabel>();
                label.transform.localScale = Vector3.one;
                label.transform.localPosition = Vector3.zero;
                label.rawPivot = UIWidget.Pivot.BottomRight;
                label.alignment = NGUIText.Alignment.Right;
                label.overflowMethod = UILabel.Overflow.ResizeHeight;
                label.maxLineCount = 0;
                label.supportEncoding = true;
                label.color = Color.white;
                // Rich AP colors must not be multiplied by the cloned HUD label's gradient.
                label.applyGradient = false;
                label.effectStyle = UILabel.Effect.Outline;
                label.effectColor = new Color(0, 0, 0, 0.9f);
                go.SetActive(false);
            }
        }
        catch { UnityEngine.Object.Destroy(_root); throw; }
    }

    public void Hide() { if (_root != null) _root.SetActive(false); }

    public void Draw(IReadOnlyList<ItemNotificationQueue.VisibleReceipt> visible, float elapsed)
    {
        _root.transform.localScale = _nativeScale;
        Array.Copy(_positions, _oldPositions, _positions.Length);
        var camera = GUIElements.me.GetComponentInChildren<Camera>();
        var depth = Vector3.Dot(GUIElements.me.hud.transform.position - camera.transform.position,
            camera.transform.forward);
        _root.transform.position = camera.ViewportToWorldPoint(new Vector3(0.98f, 0.18f, depth));
        // Convert viewport width to the native font's coordinate system. Wrap, never truncate.
        var left = _root.transform.InverseTransformPoint(camera.ViewportToWorldPoint(new Vector3(0.68f, 0.18f, depth)));
        var width = Mathf.Max(80, Mathf.RoundToInt(-left.x));
        _root.SetActive(visible.Count > 0);
        var target = 0f;
        for (var i = 0; i < _labels.Length; i++)
        {
            var label = _labels[i];
            var index = visible.Count - 1 - i; // newest at the bottom
            label.gameObject.SetActive(index >= 0);
            if (index < 0) { _receipts[i] = null; continue; }
            var receipt = visible[index];
            label.width = width;
            if (_receipts[i] != receipt) label.text = receipt.Notification.HudText;
            // Follow each receipt across pooled rows when a newer one arrives.
            var previous = Array.IndexOf(_receipts, receipt);
            var oldPosition = previous >= 0 ? _oldPositions[previous] : target;
            _positions[i] = Mathf.Lerp(oldPosition, target, 1 - Mathf.Exp(-12 * Mathf.Max(0, elapsed)));
            label.transform.localPosition = new Vector3(0, _positions[i], 0);
            label.alpha = receipt.Opacity;
            target += Mathf.Max(label.fontSize * 2, label.printedSize.y) + label.fontSize * 0.65f;
        }
        if (visible.Count > 0)
        {
            var bottom = 1f;
            var top = 0f;
            for (var i = 0; i < visible.Count; i++)
            {
                var corners = _labels[i].worldCorners;
                bottom = Mathf.Min(bottom, camera.WorldToViewportPoint(corners[0]).y);
                top = Mathf.Max(top, camera.WorldToViewportPoint(corners[2]).y);
            }
            if (top - bottom > 0.74f) _root.transform.localScale *= 0.74f / (top - bottom);
        }
        // Assign after positioning so moving a row cannot overwrite another row's identity.
        for (var i = 0; i < _receipts.Length; i++)
            _receipts[i] = i < visible.Count ? visible[visible.Count - 1 - i] : null;
    }

    public void Dispose()
    {
        if (_root != null) { _root.SetActive(false); UnityEngine.Object.Destroy(_root); }
    }
}




