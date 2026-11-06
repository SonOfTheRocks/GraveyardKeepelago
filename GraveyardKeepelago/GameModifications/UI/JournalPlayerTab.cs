using System;
using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.Journal;
using HarmonyLib;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

// Uses GameGUI's real tab dictionary: mouse clicks and NextTab share the same ordering.
public sealed class JournalPlayerTab : IDisposable
{
    public const GameGUI.TabType TabType = (GameGUI.TabType)100;
    private readonly GameGUI _menu;
    private readonly GameTabItemGUI _tab;
    private readonly JournalGUI _view;
    private readonly UILabel _label;
    public bool IsReading => _view != null && _view.is_shown;

    public JournalPlayerTab(JournalService journal, Func<bool> connected, Action<string> send,
        Func<bool> includeOtherItems, Action<bool> setIncludeOtherItems, Func<JournalCompletion> completion)
    {
        _menu = GUIElements.me.game_gui;
        var tabs = Traverse.Create(_menu).Field("_tabs").GetValue<Dictionary<GameGUI.TabType, GameTabItemGUI>>();
        var views = Traverse.Create(_menu).Field("TABS").GetValue<Dictionary<GameGUI.TabType, BaseGameGUI>>();
        if (tabs == null || views == null || !tabs.ContainsKey(GameGUI.TabType.Map) ||
            tabs.ContainsKey(TabType) || views.ContainsKey(TabType))
            throw new InvalidOperationException("Native player-menu tabs are unavailable or already include the AP journal.");
        var map = tabs[GameGUI.TabType.Map];
        var root = new GameObject("Archipelago Journal");
        root.SetActive(false);
        root.layer = GUIElements.me.map.gameObject.layer;
        root.transform.SetParent(GUIElements.me.map.transform.parent, false);
        try
        {
            _view = root.AddComponent<JournalGUI>();
            _view.Build(journal, connected, send, includeOtherItems, setIncludeOtherItems, completion);
            _view.Init();
            var go = UnityEngine.Object.Instantiate(map.gameObject, map.transform.parent, false);
            _tab = go.GetComponent<GameTabItemGUI>();
            go.GetComponentInChildren<UIButton>(true).defaultColor =
                Traverse.Create(map).Field("_default_color").GetValue<Color>();
            _tab.Init(GameGUI.TabType.Map, _view); // Initialize with an existing localization token.
            Traverse.Create(_tab).Field("_tab_type").SetValue(TabType);
            go.name = "Archipelago";
            _tab.go_active.SetActive(false);
            foreach (var localized in go.GetComponentsInChildren<LocalizedLabel>(true))
                UnityEngine.Object.DestroyImmediate(localized);
            _label = go.GetComponentInChildren<UILabel>(true);
            _label.text = "Archipelago";
            _label.overflowMethod = UILabel.Overflow.ResizeFreely;
            var button = go.GetComponentInChildren<UIButton>(true);
            button.enabled = true;
            button.onClick.Clear();
            button.onClick.Add(new EventDelegate((EventDelegate.Callback)_tab.OnPressed));
            // The new caption is longer than Map. Keep the native padding and expand its frame.
            foreach (var sprite in go.GetComponentsInChildren<UI2DSprite>(true))
                sprite.width = Mathf.Max(sprite.width, Mathf.CeilToInt(_label.printedSize.x) + _label.fontSize * 2);
            go.transform.SetSiblingIndex(map.transform.GetSiblingIndex() + 1);
            var ordered = JournalTabOrder.InsertAfter(tabs.Keys, GameGUI.TabType.Map, TabType);
            var updated = ordered.ToDictionary(key => key, key => key.Equals(TabType) ? _tab : tabs[key]);
            Traverse.Create(_menu).Field("_tabs").SetValue(updated);
            views.Add(TabType, _view);
            Reposition();
        }
        catch
        {
            Traverse.Create(_menu).Field("_tabs").SetValue(tabs);
            views.Remove(TabType);
            if (_tab != null) UnityEngine.Object.Destroy(_tab.gameObject);
            UnityEngine.Object.Destroy(root);
            throw;
        }
    }

    public void UpdateIndicator(int unread)
    {
        var text = unread > 0 ? "Archipelago *" : "Archipelago";
        if (_label.text == text) return;
        _label.text = text;
        Reposition();
    }

    private void Reposition() => Traverse.Create(_menu).Field("_tabs_grid").GetValue<UITable>()?.Reposition();
    public void Open() => _menu.OpenOrSelectTab(TabType);

    public void Dispose()
    {
        if (_menu == null) return;
        if (Traverse.Create(_menu).Field("_current_tab_type").GetValue<GameGUI.TabType>() == TabType)
        {
            if (_menu.is_shown && JournalGUI.CanUseGameCallbacks) _menu.SelectTab(GameGUI.TabType.Map);
            else Traverse.Create(_menu).Field("_current_tab_type").SetValue(GameGUI.TabType.Map);
        }
        Traverse.Create(_menu).Field("_tabs").GetValue<Dictionary<GameGUI.TabType, GameTabItemGUI>>()?.Remove(TabType);
        Traverse.Create(_menu).Field("TABS").GetValue<Dictionary<GameGUI.TabType, BaseGameGUI>>()?.Remove(TabType);
        if (_view != null)
        {
            if (_view.is_shown) _view.Hide(false);
            UnityEngine.Object.Destroy(_view.gameObject);
        }
        if (_tab != null) { _tab.gameObject.SetActive(false); UnityEngine.Object.Destroy(_tab.gameObject); }
        Reposition();
    }
}

[HarmonyPatch(typeof(GameGUI), nameof(GameGUI.Update))]
internal static class JournalTypingPatch
{
    private static bool Prefix()
    {
        if (!JournalGUI.Editing) return true;
        // Tab, arrows and Enter belong to the text field while editing, not gameplay/menu shortcuts.
        foreach (var key in new[] { GameKey.GameGUI, GameKey.PrevTab, GameKey.NextTab, GameKey.Back })
            LazyInput.ClearKeyDown(key);
        return false;
    }
}
