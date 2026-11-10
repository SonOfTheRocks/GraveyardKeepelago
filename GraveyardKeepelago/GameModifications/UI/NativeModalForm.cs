using System;
using System.Collections.Generic;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

// Owns native controls only; callers own the field meanings and dialog actions.
internal sealed class NativeModalForm : IDisposable
{
    private readonly DialogGUI _dialog;
    private readonly GameObject _root;
    private readonly NativeUiPrototypeCatalog _prototypes;
    private readonly NativeFormLayout _layout;
    private readonly UILabel.Overflow _originalOverflow;
    private readonly int _originalWidth;
    private readonly UIWidget _window;
    private readonly int _originalWindowWidth;
    private readonly Color _originalTextColor, _originalOutlineColor;
    private readonly bool _originalGradient;
    private int _depth;
    private NativeModalTitle _title;
    private readonly List<UIKeyNavigation> _navigation = new();
    private readonly List<UILabel> _captions = new();
    public int ReservedLineCount => _layout.ReservedLineCount;

    public NativeModalForm(DialogGUI dialog)
    {
        _dialog = dialog;
        _originalOverflow = dialog.label_1.overflowMethod;
        _originalWidth = dialog.label_1.width;
        _window = dialog.transform.Find("window")?.GetComponent<UIWidget>();
        if (_window == null)
            throw new InvalidOperationException("Native dialog window widget is missing.");
        _originalWindowWidth = _window.width;
        _originalTextColor = dialog.label_1.color;
        _originalOutlineColor = dialog.label_1.effectColor;
        _originalGradient = dialog.label_1.applyGradient;
        _prototypes = new NativeUiPrototypeCatalog(dialog);
        foreach (var widget in dialog.GetComponentsInChildren<UIWidget>(true))
            _depth = Mathf.Max(_depth, widget.depth + 1);
        var panel = dialog.label_1.GetComponentInParent<UIPanel>();
        if (panel == null)
            throw new InvalidOperationException("Native dialog has no UI panel.");
        _root = new GameObject("Archipelago Native Modal Form");
        _root.SetActive(false);
        _root.AddComponent<NativeFormMarker>();
        _root.layer = dialog.label_1.gameObject.layer;
        _root.transform.SetParent(panel.transform.IsChildOf(dialog.transform)
            ? panel.transform : dialog.transform, false);
        AlignWithLabel();
        _layout = new NativeFormLayout(dialog.label_1);
    }

    public NativeTextInput AddTextInput(string caption, bool password = false)
    {
        var label = _prototypes.CloneLabel(_root.transform, caption, _depth++);
        _captions.Add(label);
        var input = new NativeTextInput(_prototypes, _root.transform, password, _depth);
        _depth += 10;
        _layout.Add(label, input.Place);
        AddNavigation(input.GameObject);
        return input;
    }

    public void SetTitle(string title)
    {
        if (_title != null)
            throw new InvalidOperationException("This native form already has a title.");
        _title = new NativeModalTitle(_prototypes, _root.transform, title, _depth);
        _depth += 2;
    }

    public NativeToggle AddToggle(string caption)
    {
        var label = _prototypes.CloneLabel(_root.transform, caption, _depth++);
        _captions.Add(label);
        var toggle = new NativeToggle(_prototypes, _root.transform, _depth);
        _depth += 10;
        _layout.Add(label, toggle.Place);
        AddNavigation(toggle.GameObject);
        return toggle;
    }

    public void Show()
    {
        for (var i = 0; i < _navigation.Count; i++)
        {
            var nav = _navigation[i];
            nav.onUp = _navigation[(i + _navigation.Count - 1) % _navigation.Count].gameObject;
            nav.onTab = nav.onDown = _navigation[(i + 1) % _navigation.Count].gameObject;
        }
        Reposition();
        _root.SetActive(true);
        foreach (var caption in _captions)
            _prototypes.VerifyCaptionAppearance(caption);
        var expectedPanel = _dialog.label_1.GetComponentInParent<UIPanel>();
        foreach (var widget in _root.GetComponentsInChildren<UIWidget>(true))
            if (widget.GetComponentInParent<UIPanel>() != expectedPanel)
                throw new InvalidOperationException("Native form widget belongs to a different UI panel.");
    }

    private void AddNavigation(GameObject go)
    {
        var nav = go.AddComponent<UIKeyNavigation>();
        nav.constraint = UIKeyNavigation.Constraint.Explicit;
        nav.startsSelected = false;
        _navigation.Add(nav);
    }

    public void Prepare()
    {
        _dialog.label_1.overflowMethod = UILabel.Overflow.ResizeHeight;
        _dialog.label_1.width = Mathf.RoundToInt(NativeFormGeometry.ContentWidth(
            _originalWidth, _dialog.label_1.fontSize));
        // The frame anchors to this widget, independently of the content label's width.
        _window.width = Mathf.RoundToInt(NativeFormGeometry.WindowWidth(
            _originalWindowWidth, _originalWidth, _dialog.label_1.width));
        _prototypes.ApplyMenuTextPalette(_dialog.label_1);
    }

    public void Reposition()
    {
        AlignWithLabel();
        _layout.Reposition();
        _title?.Reposition();
    }

    private void AlignWithLabel()
    {
        var label = _dialog.label_1.transform;
        var parentScale = _root.transform.parent.lossyScale;
        var labelScale = label.lossyScale;
        _root.transform.position = label.position;
        _root.transform.rotation = label.rotation;
        _root.transform.localScale = new Vector3(labelScale.x / parentScale.x,
            labelScale.y / parentScale.y, labelScale.z / parentScale.z);
    }

    public void Hide()
    {
        if (_root == null)
            return;
        var selected = UIInput.selection;
        if (selected != null && selected.transform.IsChildOf(_root.transform))
            selected.isSelected = false;
        var focused = UICamera.selectedObject;
        if (focused != null && focused.transform.IsChildOf(_root.transform))
            UICamera.selectedObject = null;
        _root.SetActive(false);
        if (_dialog != null)
        {
            _dialog.label_1.overflowMethod = _originalOverflow;
            _dialog.label_1.width = _originalWidth;
            _window.width = _originalWindowWidth;
            _window.BroadcastMessage("UpdateAnchors", SendMessageOptions.DontRequireReceiver);
            _dialog.label_1.color = _originalTextColor;
            _dialog.label_1.effectColor = _originalOutlineColor;
            _dialog.label_1.applyGradient = _originalGradient;
        }
    }

    public void Dispose()
    {
        Hide();
        if (_root != null)
            UnityEngine.Object.Destroy(_root);
    }
}
