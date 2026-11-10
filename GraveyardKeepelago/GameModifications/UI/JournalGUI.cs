using System;
using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.Journal;
using HarmonyLib;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

// Native NGUI panel with pooled visible rows; the full 2,000-entry history is never instantiated.
public sealed class JournalGUI : BaseGameGUI
{
    private JournalService _journal;
    private Func<bool> _connected, _includeOtherItems;
    private Action<string> _send;
    private Action<bool> _setIncludeOtherItems;
    private NativeUiPrototypeCatalog _prototypes;
    private NativeTextInput _input;
    private UILabel _status, _help, _latest, _measure, _others;
    private JournalLayout _layout;
    private readonly List<RowWidgets> _pool = new();
    private readonly List<UILabel> _filters = new();
    private readonly List<Row> _rows = new();
    private readonly Dictionary<string, Row> _measurements = new();
    private readonly JournalScrollPosition _scroll = new();
    private UIScrollBar _scrollbar;
    private bool _syncingScrollbar;
    private Transform _viewport;
    private JournalFilter _filter;
    private int _revision = -1;
    private int _messageCount;
    private bool _previousOtherItems;
    private float _width, _height, _viewportHeight, _line, _lastOffset = -1;
    private string _error = string.Empty;
    private static JournalGUI _current;
    private int _editFrame = -1;
    private Func<JournalCompletion> _completionSource;
    private JournalCompletion _completion;
    private JournalCompletionSuggestion[] _suggestions = Array.Empty<JournalCompletionSuggestion>();
    private string _completionText;
    private int _completionCursor, _completionStart, _completionEnd, _completionFrame = -1;
    private readonly JournalCompletionSelection _selection = new();
    private readonly JournalInputHistory _inputHistory = new();
    private JournalCompletionPopup _popup;
    private string _dismissedCompletion;
    public static bool Editing => _current != null && _current.is_shown &&
        (_current._input.Input.isSelected || _current._editFrame == Time.frameCount);
    internal static bool CanUseGameCallbacks => JournalLifecyclePolicy.UseGameCallbacks(MainGame.game_started,
        MainGame.me != null && MainGame.me.player != null,
        MainGame.me != null && MainGame.me.player != null && MainGame.me.player.components.character != null);

    private sealed class Row
    {
        public JournalEntry Entry;
        public string Text;
        public bool IsDayHeader => Entry == null;
        public float Y, Height;
    }

    private sealed class RowWidgets
    {
        public UILabel Time, Body;
        public void SetActive(bool active)
        { Time.gameObject.SetActive(active); Body.gameObject.SetActive(active); }
    }

    public void Build(JournalService journal, Func<bool> connected, Action<string> send,
        Func<bool> includeOtherItems, Action<bool> setIncludeOtherItems, Func<JournalCompletion> completion)
    {
        _journal = journal;
        _connected = connected;
        _send = send;
        _includeOtherItems = includeOtherItems;
        _setIncludeOtherItems = setIncludeOtherItems;
        _completionSource = completion;
        _prototypes = new NativeUiPrototypeCatalog(GUIElements.me.dialog);
        _line = _prototypes.RegularText.fontSize + _prototypes.RegularText.spacingY;
        var parentScale = transform.parent.lossyScale;
        var nativeScale = _prototypes.RegularText.transform.lossyScale;
        transform.localScale = new Vector3(nativeScale.x / parentScale.x, nativeScale.y / parentScale.y, 1);
        var mapPanel = GUIElements.me.map.GetComponentInChildren<UIScrollView>(true).panel;
        var clip = mapPanel.finalClipRegion;
        transform.position = mapPanel.transform.TransformPoint(new Vector3(clip.x, clip.y, 0));
        var mapScale = mapPanel.transform.lossyScale;
        _width = clip.z * Mathf.Abs(mapScale.x / nativeScale.x);
        _height = clip.w * Mathf.Abs(mapScale.y / nativeScale.y);
        _measure = Label("00:00", transform, 0, 0, _width, _line);
        _measure.overflowMethod = UILabel.Overflow.ResizeFreely;
        var timeWidth = JournalTimestampGeometry.RequiredWidth(text => { _measure.text = text; return _measure.printedSize.x; });
        _layout = new JournalLayout(_width, _height, _line, timeWidth);
        _measure.width = Mathf.RoundToInt(_layout.BodyWidth);
        _measure.overflowMethod = UILabel.Overflow.ResizeHeight;
        _measure.gameObject.SetActive(false);
        var panel = gameObject.AddComponent<UIPanel>();
        panel.depth = mapPanel.depth + 1;
        NativeJournalPanels.Build(transform, _width, _height, _layout.FooterDividerY);

        var navigationController = gameObject.AddComponent<GamepadNavigationController>();
        navigationController.auto_select = true;
        navigationController.vertical_settings = new GamepadNavigationSettings();
        navigationController.horizontal_settings = new GamepadNavigationSettings();
        var captions = new[] { "All", "My Items", "Chat", "System" };
        for (var i = 0; i < captions.Length; i++)
        {
            var selected = (JournalFilter)i;
            _filters.Add(Button(captions[i], _layout.Filters[i], () => SetFilter(selected)));
        }
        _others = Button("Other items: off", _layout.OtherItems,
            () => { _setIncludeOtherItems(!_includeOtherItems()); _revision = -1; });

        var view = new GameObject("Journal scroll viewport");
        view.layer = gameObject.layer;
        view.transform.SetParent(transform, false);
        _viewport = view.transform;
        _viewportHeight = _layout.History.Height;
        _viewport.localPosition = new Vector3(_layout.History.CenterX, _layout.History.CenterY, 0);
        var clippedPanel = view.AddComponent<UIPanel>();
        clippedPanel.depth = panel.depth + 1;
        clippedPanel.clipping = UIDrawCall.Clipping.SoftClip;
        clippedPanel.baseClipRegion = new Vector4(0, 0, _layout.History.Width, _viewportHeight);
        clippedPanel.clipSoftness = Vector2.zero;
        var area = view.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size = new Vector2(_layout.History.Width, _viewportHeight);
        var listener = UIEventListener.Get(view);
        listener.onScroll = (_, amount) => Scroll(-amount * _line * 8);
        listener.onDrag = (_, delta) => Scroll(delta.y * UIRoot.GetPixelSizeAdjustment(gameObject));
        BuildScrollbar();

        var rowCount = Mathf.CeilToInt(_viewportHeight / (_line + _layout.MessageGap)) + 2;
        for (var i = 0; i < rowCount; i++)
        {
            var label = Label(string.Empty, _viewport, _layout.BodyLeft - _layout.History.CenterX,
                0, _layout.BodyWidth, _line);
            label.overflowMethod = UILabel.Overflow.ResizeHeight;
            var time = Label(string.Empty, _viewport, _layout.TimeRight - _layout.History.CenterX,
                0, _layout.TimeWidth, _line);
            time.rawPivot = UIWidget.Pivot.TopRight;
            time.alignment = NGUIText.Alignment.Right;
            time.supportEncoding = false;
            time.overflowMethod = UILabel.Overflow.ResizeFreely;
            time.maxLineCount = 1;
            time.color = new Color(0.70f, 0.68f, 0.73f);
            var widgets = new RowWidgets { Body = label, Time = time };
            widgets.SetActive(false);
            _pool.Add(widgets);
        }
        _latest = Button("Latest", _layout.Latest,
            () => { _scroll.Latest(); DrawRows(); });
        _status = Label(string.Empty, transform, _layout.Status.X, _layout.Status.Top,
            _layout.Status.Width, _layout.Status.Height);
        _status.supportEncoding = false;
        _status.maxLineCount = 1;
        _help = Label(string.Empty, transform, _layout.Help.X, _layout.Help.Top,
            _layout.Help.Width, _layout.Help.Height);
        _help.supportEncoding = false;
        _help.maxLineCount = 1;
        _input = new NativeTextInput(_prototypes, transform, false, 20);
        _input.Input.characterLimit = 500;
        _input.Place(_layout.Input.X, _layout.Input.Top, _layout.Input.Width, _layout.Input.Height);
        _input.Input.onSubmit.Clear();
        _input.Input.selectAllTextOnFocus = false;
        _input.Input.onSubmit.Add(new EventDelegate((EventDelegate.Callback)SubmitInput));
        UIEventListener.Get(_input.GameObject).onKey = (_, key) =>
        {
            if (key == KeyCode.Escape || key == KeyCode.Tab) _editFrame = Time.frameCount;
        };
        Button("Send", _layout.Send, Send);
        _popup = new JournalCompletionPopup(_prototypes, transform, new JournalPopupLayout(_layout, _line),
            panel.depth + 3, index => _selection.Select(index, _suggestions.Length, _popup.VisibleRows),
            AcceptCompletion, delta => _selection.Move(delta, _suggestions.Length, _popup.VisibleRows));
        var inputNav = _input.GameObject.AddComponent<UIKeyNavigation>();
        inputNav.constraint = UIKeyNavigation.Constraint.Explicit;
        var controls = GetComponentsInChildren<UIKeyNavigation>(true);
        for (var i = 0; i < controls.Length; i++)
        {
            controls[i].onTab = controls[(i + 1) % controls.Length].gameObject;
            controls[i].onUp = controls[(i + controls.Length - 1) % controls.Length].gameObject;
            controls[i].onDown = controls[(i + 1) % controls.Length].gameObject;
        }
        _current = this;
    }

    private UILabel Label(string text, Transform parent, float x, float y, float width, float height)
    {
        var label = _prototypes.CloneLabel(parent, text, 10);
        _prototypes.ApplyInputTypography(label);
        label.fontSize = _prototypes.RegularText.fontSize;
        label.applyGradient = false;
        label.color = _prototypes.RegularText.color;
        label.transform.localPosition = new Vector3(x, y, 0);
        label.width = Mathf.RoundToInt(width);
        label.height = Mathf.RoundToInt(height);
        label.maxLineCount = 0;
        label.supportEncoding = true;
        return label;
    }

    private void BuildScrollbar()
    {
        var bounds = _layout.Scrollbar;
        var root = new GameObject("Journal scrollbar") { layer = gameObject.layer };
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(bounds.CenterX, bounds.CenterY, 0);
        UI2DSprite Part(string name, int depth, Color tint)
        {
            var part = NativeUiPrototypeCatalog.Clone(_prototypes.SelectionBackground.gameObject, root.transform, name);
            var sprite = part.GetComponent<UI2DSprite>();
            sprite.transform.localScale = Vector3.one;
            sprite.depth = depth;
            sprite.color = tint;
            NativeSpriteFrame.Fit(sprite, bounds.Width, bounds.Height, Vector2.zero);
            foreach (var old in part.GetComponentsInChildren<Collider2D>(true)) old.enabled = false;
            var hit = part.GetComponent<BoxCollider2D>() ?? part.AddComponent<BoxCollider2D>();
            hit.enabled = hit.isTrigger = true;
            NGUITools.UpdateWidgetCollider(part, true);
            UIEventListener.Get(part).onScroll = (_, amount) => Scroll(-amount * _line * 8);
            return sprite;
        }
        var track = Part("Track", 8, new Color(0.35f, 0.35f, 0.4f));
        var handle = Part("Handle", 9, new Color(1, 0.88f, 0.65f));
        _scrollbar = root.AddComponent<UIScrollBar>();
        _scrollbar.backgroundWidget = track;
        _scrollbar.foregroundWidget = handle;
        _scrollbar.fillDirection = UIProgressBar.FillDirection.TopToBottom;
        _scrollbar.onChange.Add(new EventDelegate((EventDelegate.Callback)(() =>
        {
            if (_syncingScrollbar) return;
            _scroll.SetNormalized(_scrollbar.value);
            DrawRows();
        })));
    }

    private void SyncScrollbar()
    {
        if (_scrollbar == null) return;
        _syncingScrollbar = true;
        try
        {
            _scrollbar.gameObject.SetActive(_scroll.Maximum > 0);
            _scrollbar.barSize = Mathf.Max(_scroll.VisibleFraction,
                Mathf.Min(1, _line * 1.5f / _viewportHeight));
            _scrollbar.Set(_scroll.Normalized, false);
        }
        finally { _syncingScrollbar = false; }
    }

    private UILabel Button(string text, JournalRect bounds, Action action)
    {
        var label = Label(text, transform, bounds.CenterX, bounds.CenterY, bounds.Width, bounds.Height);
        // NGUI UILabel.ApplyOffset centers the printed glyphs for a Center pivot.
        // Horizontal alignment alone leaves the text stuck to the top edge.
        // rawPivot avoids NGUI moving the transform to preserve the previous rectangle.
        label.rawPivot = UIWidget.Pivot.Center;
        label.alignment = NGUIText.Alignment.Center;
        label.maxLineCount = 1;
        var frameGo = NativeUiPrototypeCatalog.Clone(_prototypes.SelectionBackground.gameObject,
            label.transform, text + " background");
        var frame = frameGo.GetComponent<UI2DSprite>();
        frame.transform.localScale = Vector3.one;
        frame.depth = label.depth - 1;
        NativeSpriteFrame.Fit(frame, bounds.Width, bounds.Height, Vector2.zero);
        var collider = label.gameObject.GetComponent<BoxCollider2D>() ?? label.gameObject.AddComponent<BoxCollider2D>();
        collider.enabled = collider.isTrigger = true;
        NGUITools.UpdateWidgetCollider(label.gameObject, true);
        var button = label.gameObject.AddComponent<UIButton>();
        button.tweenTarget = frameGo;
        button.onClick.Add(new EventDelegate((EventDelegate.Callback)(() => action())));
        var navigation = label.gameObject.AddComponent<GamepadNavigationItem>();
        navigation.SetCallbacks(() => frame.alpha = 1, () => frame.alpha = 0.65f, () => action());
        frame.alpha = 0.65f;
        label.gameObject.AddComponent<UIKeyNavigation>().constraint = UIKeyNavigation.Constraint.Explicit;
        return label;
    }

    public override void Open()
    {
        base.Open();
        AlignToMap();
        _current = this;
        _completion = _completionSource();
        _completionText = null;
        _dismissedCompletion = null;
        _revision = -1;
        Rebuild(true);
        gamepad_controller.ReinitItems(true);
    }

    public override void Hide(bool play_hide_sound = true)
    {
        if (_input != null) _input.Input.isSelected = false;
        _popup?.Hide();
        _inputHistory.Reset();
        if (CanUseGameCallbacks) base.Hide(play_hide_sound);
        else
        {
            // BaseGUI.Hide touches the character and broadcasts game events even during scene teardown.
            gameObject.SetActive(false);
            BaseGUI.opened_windows.Remove(this);
            Traverse.Create(this).Field("_is_shown").SetValue(false);
            if (BaseGUI.active_gui == this)
                Traverse.Create(typeof(BaseGUI)).Field("_active_gui").SetValue(BaseGUI.opened_windows.LastOrDefault());
            if (gamepad_controller != null) gamepad_controller.Disable();
        }
        _journal?.Flush();
    }

    public override void Update()
    {
        if (!is_shown_and_top) return;
        AlignToMap();
        if (_input.Input.isSelected)
        {
            _editFrame = Time.frameCount;
            RefreshCompletion();
            if (for_gamepad && LazyInput.GetKeyDown(GameKey.Back))
            {
                _input.Input.isSelected = false;
                LazyInput.ClearKeyDown(GameKey.Back);
            }
            else if (for_gamepad && LazyInput.GetKeyDown(GameKey.Select)) SubmitInput();
            foreach (var key in new[] { GameKey.GameGUI, GameKey.PrevTab, GameKey.NextTab, GameKey.Select })
                LazyInput.ClearKeyDown(key);
        }
        else
        {
            base.Update();
            var direction = LazyInput.GetDirection();
            if (Mathf.Abs(direction.y) > 0.1f) Scroll(-direction.y * _line * 18 * Time.unscaledDeltaTime);
            if (Input.GetKeyDown(KeyCode.PageUp)) Scroll(-_viewportHeight * 0.8f);
            if (Input.GetKeyDown(KeyCode.PageDown)) Scroll(_viewportHeight * 0.8f);
            if (Input.GetKeyDown(KeyCode.End)) { _scroll.Latest(); DrawRows(); }
        }
        if (_revision != _journal.History.Revision || _previousOtherItems != _includeOtherItems()) Rebuild(false);
        if (!Mathf.Approximately(_lastOffset, _scroll.Offset)) DrawRows();
        _input.SetEditable(_connected());
        var unread = _journal.History.Unread(_includeOtherItems());
        _latest.text = unread > 0 ? "Latest (" + unread + ")" : "Latest";
        _status.text = (_connected() ? "Connected" : "Disconnected — reconnect with F8") +
            " · " + _messageCount + " messages";
        _help.text = _error.Length > 0 ? _error : "Arrows: scroll/filter · " +
            (for_gamepad ? GameKeyTip.GetIcon(GameKey.Option1) + " Chat · " + GameKeyTip.GetIcon(GameKey.Option2) + " Latest" : "Enter: send · End: latest");
        if (_input.Input.isSelected && !for_gamepad && _error.Length == 0)
            _help.text = "Up/Down: input history · Enter: send";
        if ((_input.Input.isSelected || _popup.ContainsPointer) && _suggestions.Length > 0 &&
            _dismissedCompletion != _input.Value)
        {
            _popup.Show(_suggestions, _selection);
            if (_error.Length == 0) _help.text = "Up/Down: suggestions · Tab/Enter: insert · Esc: dismiss";
        }
        else _popup.Hide();
    }

    private void RefreshCompletion()
    {
        var input = _input.Input;
        if (_completionText == input.value && _completionCursor == input.cursorPosition &&
            _completionStart == input.selectionStart && _completionEnd == input.selectionEnd) return;
        _completionText = input.value;
        _completionCursor = input.cursorPosition;
        _completionStart = input.selectionStart;
        _completionEnd = input.selectionEnd;
        _suggestions = _completion?.Suggest(input.value, input.cursorPosition, input.selectionStart, input.selectionEnd)
            ?? Array.Empty<JournalCompletionSuggestion>();
        _selection.Reset();
    }

    internal static bool HandleCompletionKey(GameObject owner, KeyCode key)
    {
        var view = _current;
        if (view == null || !view.is_shown || owner != view._input.GameObject) return false;
        if (key != KeyCode.Tab && key != KeyCode.Escape && key != KeyCode.UpArrow && key != KeyCode.DownArrow) return false;
        if (view._completionFrame == Time.frameCount) return true;
        if (!view._input.Input.isSelected) return false;
        view.RefreshCompletion();
        if (view._suggestions.Length == 0 || view._dismissedCompletion == view._input.Value)
        {
            if (key != KeyCode.UpArrow && key != KeyCode.DownArrow) return false;
            var recalled = view._inputHistory.Move(key == KeyCode.UpArrow ? -1 : 1, view._input.Value);
            if (recalled != view._input.Value)
            {
                view._input.Value = recalled;
                view._input.Input.cursorPosition = recalled.Length;
            }
            view._dismissedCompletion = recalled;
            view._popup.Hide();
            view._completionFrame = view._editFrame = Time.frameCount;
            return true;
        }
        if (key == KeyCode.Tab) view.AcceptCompletion();
        else if (key == KeyCode.Escape) { view._dismissedCompletion = view._input.Value; view._popup.Hide(); }
        else view._selection.Move(key == KeyCode.UpArrow ? -1 : 1, view._suggestions.Length, view._popup.VisibleRows);
        view._completionFrame = view._editFrame = Time.frameCount;
        return true;
    }

    private void AcceptCompletion()
    {
        if (_suggestions.Length == 0) return;
        _input.Value = _suggestions[_selection.Index].Value;
        _input.Input.isSelected = true;
        _input.Input.cursorPosition = _input.Value.Length;
        _dismissedCompletion = _completion.HasParameterSuggestions(_input.Value) ? null : _input.Value;
        RefreshCompletion();
        if (_dismissedCompletion == null) _popup.Show(_suggestions, _selection);
        else _popup.Hide();
        _completionFrame = _editFrame = Time.frameCount;
    }

    private void SubmitInput()
    {
        if (_completionFrame == Time.frameCount) return;
        RefreshCompletion();
        if (_suggestions.Length > 0 && _dismissedCompletion != _input.Value) AcceptCompletion();
        else Send();
    }

    private void Rebuild(bool latest)
    {
        var follow = latest || _scroll.AtBottom;
        var visible = _journal.History.Visible(_filter, _includeOtherItems());
        float? previousFirstY = null;
        if (!latest && _rows.Count > 0 && visible.Length > 0)
        {
            var surviving = _rows.Find(row => row.Entry?.ID == visible[0].ID);
            if (surviving != null) previousFirstY = surviving.Y;
        }
        var alive = new HashSet<string>(_journal.History.Entries.Select(entry => entry.ID));
        foreach (var key in _measurements.Keys.Where(key => !alive.Contains(key)).ToArray()) _measurements.Remove(key);
        _rows.Clear();
        _messageCount = visible.Length;
        var y = 0f;
        foreach (var day in JournalDayGroups.Build(visible, TimeZoneInfo.Local))
        {
            var header = new Row { Text = "— " + day.Caption + " —", Y = y, Height = _line * 1.8f };
            _rows.Add(header);
            y += header.Height;
            foreach (var entry in day.Entries)
            {
                if (!_measurements.TryGetValue(entry.ID, out var row))
                {
                    _measure.text = entry.FormatBody(lightBackground: false);
                    row = new Row { Entry = entry, Text = _measure.text,
                        Height = Mathf.Max(_line, _measure.printedSize.y) + _layout.MessageGap };
                    _measurements.Add(entry.ID, row);
                }
                row.Y = y;
                y += row.Height;
                _rows.Add(row);
            }
        }
        var newFirstY = _rows.FirstOrDefault(row => !row.IsDayHeader)?.Y ?? 0;
        var removed = previousFirstY.HasValue ? Mathf.Max(0, previousFirstY.Value - newFirstY) : 0;
        _scroll.Resize(y, _viewportHeight, follow, removed);
        _others.text = "Other items: " + (_includeOtherItems() ? "on" : "off");
        for (var i = 0; i < _filters.Count; i++)
            _filters[i].color = i == (int)_filter ? new Color(1f, 0.84f, 0.48f) : _prototypes.RegularText.color;
        DrawRows();
        _previousOtherItems = _includeOtherItems();
        _revision = _journal.History.Revision;
    }

    private void DrawRows()
    {
        var index = 0;
        var read = new List<JournalEntry>();
        foreach (var row in _rows)
        {
            if (row.Y + row.Height < _scroll.Offset || row.Y > _scroll.Offset + _viewportHeight) continue;
            if (index == _pool.Count) break;
            var widgets = _pool[index++];
            widgets.SetActive(true);
            widgets.Body.text = row.Text;
            widgets.Time.gameObject.SetActive(!row.IsDayHeader);
            if (!row.IsDayHeader) widgets.Time.text = row.Entry.Timestamp.ToLocalTime().ToString("HH:mm");
            widgets.Body.alignment = row.IsDayHeader ? NGUIText.Alignment.Center : NGUIText.Alignment.Left;
            widgets.Body.width = Mathf.RoundToInt(row.IsDayHeader ? _layout.History.Width : _layout.BodyWidth);
            widgets.Body.color = row.IsDayHeader ? new Color(0.84f, 0.79f, 0.65f) : _prototypes.RegularText.color;
            var position = widgets.Body.transform.localPosition;
            position.x = (row.IsDayHeader ? _layout.History.X : _layout.BodyLeft) - _layout.History.CenterX;
            position.y = _viewportHeight / 2 - row.Y + _scroll.Offset - (row.IsDayHeader ? _line * 0.3f : 0);
            widgets.Body.transform.localPosition = position;
            widgets.Time.transform.localPosition = new Vector3(widgets.Time.transform.localPosition.x, position.y, position.z);
            // Reading a filtered view must not silently mark hidden categories as read.
            if (!row.IsDayHeader && (row.Y >= _scroll.Offset && row.Y + row.Height <= _scroll.Offset + _viewportHeight ||
                row.Height > _viewportHeight && row.Y <= _scroll.Offset && row.Y + row.Height >= _scroll.Offset + _viewportHeight))
                read.Add(row.Entry);
        }
        while (index < _pool.Count) _pool[index++].SetActive(false);
        if (_scroll.AtBottom) read = _rows.Where(row => !row.IsDayHeader).Select(row => row.Entry).ToList();
        _journal.History.MarkRead(read);
        _lastOffset = _scroll.Offset;
        SyncScrollbar();
    }

    private void SetFilter(JournalFilter filter) { _filter = filter; Rebuild(true); }
    private void Scroll(float amount) { _scroll.Move(amount); DrawRows(); }
    private void Send()
    {
        _editFrame = Time.frameCount;
        if (JournalChat.TrySend(_input.Value, _connected(), _send, out _error))
        { _inputHistory.Record(_input.Value); _input.Value = string.Empty; _input.Input.isSelected = false; }
        LazyInput.ClearKeyDown(GameKey.Select);
    }

    private void AlignToMap()
    {
        // Resolution/UI-scale changes resize the same content area; preserve scroll and input state.
        var panel = GUIElements.me.map.GetComponentInChildren<UIScrollView>(true).panel;
        var clip = panel.finalClipRegion;
        var nativeScale = _prototypes.RegularText.transform.lossyScale;
        var available = panel.transform.lossyScale;
        var factor = Mathf.Min(clip.z * Mathf.Abs(available.x / nativeScale.x) / _width,
            clip.w * Mathf.Abs(available.y / nativeScale.y) / _height);
        var parentScale = transform.parent.lossyScale;
        transform.localScale = new Vector3(nativeScale.x / parentScale.x, nativeScale.y / parentScale.y, 1) * Mathf.Max(0.1f, factor);
        transform.position = panel.transform.TransformPoint(new Vector3(clip.x, clip.y, 0));
    }

    protected override bool OnPressedBack() { GUIElements.me.game_gui.Hide(); return true; }
    protected override bool OnPressedPrevTab() { GUIElements.me.game_gui.NextTab(-1); return true; }
    protected override bool OnPressedNextTab() { GUIElements.me.game_gui.NextTab(1); return true; }
    protected override bool OnPressedLeft() { SetFilter((JournalFilter)(((int)_filter + 3) % 4)); return true; }
    protected override bool OnPressedRight() { SetFilter((JournalFilter)(((int)_filter + 1) % 4)); return true; }
    protected override bool OnPressedUp() => true;
    protected override bool OnPressedDown() => true;
    protected override bool OnPressedOption1()
    { if (_connected()) { _input.Input.isSelected = true; _editFrame = Time.frameCount; } return true; }
    protected override bool OnPressedOption2() { _scroll.Latest(); DrawRows(); return true; }

    private void OnDestroy()
    {
        if (ReferenceEquals(_current, this)) _current = null;
    }
}
