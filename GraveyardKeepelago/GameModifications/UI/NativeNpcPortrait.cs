using System;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

public sealed class NativeNpcPortrait : IDisposable
{
    private readonly UI2DSprite _icon;
    private readonly bool _iconWasEnabled;
    private readonly WidgetState _root, _background, _frame, _labelState;
    private readonly UILabel _label;
    private readonly UILabel.Overflow _overflow;
    private GameObject _host;
    private bool _disposed;

    public NativeNpcPortrait(TechTreeGUIUnlockItem row, Sprite npcSprite)
    {
        _icon = row.spr;
        _iconWasEnabled = _icon.enabled;
        _root = new WidgetState(row.GetComponent<UIWidget>());
        _background = new WidgetState(row.background);
        // Runtime-inspected native icon frame; it is separate from the NPC stage.
        _frame = new WidgetState(_icon.transform.Find("bg")?.GetComponent<UIWidget>());
        _label = row.label_name;
        _labelState = new WidgetState(_label);
        _overflow = _label.overflowMethod;
        var prototype = GUIElements.me.npcs_list.prefab.char_spr;
        var source = prototype.transform.parent.GetComponent<UI2DSprite>();
        if (source == null || source.sprite2D?.name != "char_wndw_j_frame_stand" ||
            _frame.Widget == null || row.background == null)
            throw new InvalidOperationException("Native NPC stage or receipt frame prototype is missing.");

        try
        {
            var layout = new NpcReceiptLayout(row.background.width, row.background.height, source.width, source.height);
            var outer = row.background.drawingDimensions;
            var center = row.transform.InverseTransformPoint(row.background.transform.TransformPoint(
                new Vector3((outer.x + outer.z) / 2, (outer.y + outer.w) / 2, 0)));
            var portraitCenter = row.transform.TransformPoint(center + new Vector3(layout.FrameCenterX, 0, 0));

            _host = new GameObject("AP relation portrait");
            _host.SetActive(false);
            _host.layer = _icon.gameObject.layer;
            _host.transform.SetParent(_icon.transform, false);
            var stage = NativeUiPrototypeCatalog.Clone(source.gameObject, _host.transform, "NPC stage with spotlight");
            stage.transform.localPosition = Vector3.zero;
            stage.transform.localRotation = Quaternion.identity;
            stage.transform.localScale = Vector3.one;
            _host.transform.position = portraitCenter;

            if (_root.Widget != null)
            {
                _root.Widget.width = layout.RowWidth;
                _root.Widget.height = layout.RowHeight;
            }
            row.background.height = layout.RowHeight;
            _frame.Widget.width = layout.FrameWidth;
            _frame.Widget.height = layout.FrameHeight;
            _frame.Widget.transform.position = portraitCenter;

            _label.width = layout.TextWidth;
            _label.overflowMethod = UILabel.Overflow.ResizeHeight;
            _label.ProcessText();
            _label.transform.position = row.transform.TransformPoint(center + new Vector3(layout.TextLeft, 0, 0));

            var portrait = stage.transform.Find(prototype.name)?.GetComponent<UI2DSprite>();
            if (portrait == null) throw new InvalidOperationException("Native NPC character sprite is missing.");
            portrait.sprite2D = npcSprite;
            foreach (var widget in stage.GetComponentsInChildren<UIWidget>(true))
                widget.depth = _icon.depth + widget.depth - source.depth;
            foreach (var collider in stage.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
            foreach (var collider in stage.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            _icon.enabled = false;
            _host.SetActive(true);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_icon != null) _icon.enabled = _iconWasEnabled;
        if (_label != null) _label.overflowMethod = _overflow;
        _root.Restore();
        _background.Restore();
        _frame.Restore();
        _labelState.Restore();
        if (_host == null) return;
        _host.SetActive(false);
        UnityEngine.Object.Destroy(_host);
        _host = null;
    }

    private sealed class WidgetState
    {
        public UIWidget Widget { get; }
        private readonly int _width, _height;
        private readonly Vector3 _position;
        public WidgetState(UIWidget widget)
        {
            Widget = widget;
            if (widget == null) return;
            _width = widget.width;
            _height = widget.height;
            _position = widget.transform.localPosition;
        }
        public void Restore()
        {
            if (Widget == null) return;
            Widget.width = _width;
            Widget.height = _height;
            Widget.transform.localPosition = _position;
        }
    }
}
