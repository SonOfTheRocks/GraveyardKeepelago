using System;
using GraveyardKeepelago.Journal;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

internal sealed class JournalCompletionPopup
{
    private readonly GameObject _root;
    private readonly UILabel[] _rows;
    private readonly UI2DSprite[] _highlights;
    private readonly UILabel _description;
    private readonly JournalPopupLayout _layout;
    private int _firstVisible;
    public bool Visible => _root.activeSelf;
    public int VisibleRows => _layout.VisibleRows;
    public bool ContainsPointer => UICamera.hoveredObject != null &&
        UICamera.hoveredObject.transform.IsChildOf(_root.transform);

    public JournalCompletionPopup(NativeUiPrototypeCatalog prototypes, Transform parent,
        JournalPopupLayout layout, int panelDepth, Action<int> select, Action accept, Action<int> move)
    {
        _layout = layout;
        _root = new GameObject("Journal command suggestions");
        _root.SetActive(false);
        _root.layer = parent.gameObject.layer;
        _root.transform.SetParent(parent, false);
        _root.transform.localPosition = new Vector3(layout.Bounds.X, layout.Bounds.Top, 0);
        _root.AddComponent<UIPanel>().depth = panelDepth;
        // A thin rim behind the inset fill cannot cover text, unlike the decorated modal frame.
        var rim = Frame(prototypes, _root.transform, "Suggestion popup simple rim", 0);
        NativeSpriteFrame.Fit(rim, layout.Bounds.Width, layout.Bounds.Height,
            new Vector2(layout.Bounds.Width / 2, -layout.Bounds.Height / 2));
        rim.color = new Color(0.9f, 0.9f, 1, 1);
        var background = Frame(prototypes, _root.transform, "Suggestion popup background", 1);
        NativeSpriteFrame.Fit(background, layout.InnerFill.Width, layout.InnerFill.Height,
            new Vector2(layout.InnerFill.CenterX, layout.InnerFill.CenterY));
        background.color = new Color(0.30f, 0.31f, 0.39f, 1);
        _rows = new UILabel[layout.VisibleRows];
        _highlights = new UI2DSprite[layout.VisibleRows];
        for (var i = 0; i < _rows.Length; i++)
        {
            var row = i;
            var label = Text(prototypes, "Suggestion " + i, layout.Bounds.Width - layout.Padding * 2, layout.RowHeight);
            label.transform.localPosition = new Vector3(layout.Padding, -layout.Padding - i * layout.RowHeight, 0);
            label.maxLineCount = 1;
            var highlight = Frame(prototypes, label.transform, "Selected suggestion", 2);
            NativeSpriteFrame.Fit(highlight, layout.Bounds.Width - layout.Padding, layout.RowHeight,
                new Vector2((layout.Bounds.Width - layout.Padding * 2) / 2, -layout.RowHeight / 2));
            highlight.color = new Color(0.95f, 0.80f, 0.48f, 1);
            var collider = label.gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(label.width, layout.RowHeight);
            collider.offset = new Vector2(label.width / 2f, -layout.RowHeight / 2);
            var listener = UIEventListener.Get(label.gameObject);
            listener.onHover = (_, hover) => { if (hover) select(_firstVisible + row); };
            listener.onClick = _ => { select(_firstVisible + row); accept(); };
            listener.onScroll = (_, amount) => move(amount > 0 ? -1 : 1);
            _rows[i] = label;
            _highlights[i] = highlight;
        }
        _description = Text(prototypes, "Suggestion description", layout.Bounds.Width - layout.Padding * 2,
            layout.Padding * 4);
        _description.transform.localPosition = new Vector3(layout.Padding,
            -layout.Padding - layout.VisibleRows * layout.RowHeight, 0);
        _description.maxLineCount = 2;
        _description.color = new Color(0.8f, 0.8f, 0.84f);
    }

    private UILabel Text(NativeUiPrototypeCatalog prototypes, string name, float width, float height)
    {
        var label = prototypes.CloneLabel(_root.transform, string.Empty, 3);
        label.name = name;
        prototypes.ApplyInputTypography(label);
        label.fontSize = prototypes.RegularText.fontSize;
        label.rawPivot = UIWidget.Pivot.TopLeft;
        label.width = Mathf.RoundToInt(width);
        label.height = Mathf.RoundToInt(height);
        label.supportEncoding = false;
        label.applyGradient = false;
        label.color = prototypes.RegularText.color;
        return label;
    }

    private static UI2DSprite Frame(NativeUiPrototypeCatalog prototypes, Transform parent, string name, int depth)
    {
        var frame = NativeUiPrototypeCatalog.Clone(prototypes.SelectionBackground.gameObject, parent, name).GetComponent<UI2DSprite>();
        frame.transform.localScale = Vector3.one;
        frame.depth = depth;
        foreach (var collider in frame.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
        return frame;
    }

    public void Show(JournalCompletionSuggestion[] suggestions, JournalCompletionSelection selection)
    {
        _firstVisible = selection.FirstVisible;
        _root.SetActive(suggestions.Length > 0);
        for (var i = 0; i < _rows.Length; i++)
        {
            var index = _firstVisible + i;
            _rows[i].gameObject.SetActive(index < suggestions.Length);
            if (index >= suggestions.Length) continue;
            _rows[i].text = suggestions[index].Value.TrimEnd();
            _highlights[i].alpha = index == selection.Index ? 1f : 0f;
        }
        if (suggestions.Length > 0)
            _description.text = (selection.Index + 1) + "/" + suggestions.Length + " · " + suggestions[selection.Index].Description;
    }

    public void Hide() => _root.SetActive(false);
}
