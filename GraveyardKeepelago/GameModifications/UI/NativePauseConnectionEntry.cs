using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

internal sealed class NativePauseConnectionEntry : IDisposable
{
    private readonly InGameMenuGUI _menu;
    private readonly MenuItemGUI _entry, _continue, _options;
    private readonly GamepadNavigationItem _previousDown, _previousUp;

    public NativePauseConnectionEntry(InGameMenuGUI menu, Action clicked)
    {
        _menu = menu;
        var items = Traverse.Create(menu).Field("items").GetValue<MenuItemGUI[]>();
        var index = PauseMenuPlacement.ConnectionIndex(items.Select(item => item.on_pressed?.methodName).ToArray());
        _options = items[index];
        _continue = items.First(item => item.on_pressed?.methodName == "OnPressedContinue");
        var go = UnityEngine.Object.Instantiate(_options.gameObject, _options.transform.parent, false);
        go.name = "Archipelago Connection";
        go.transform.SetSiblingIndex(_options.transform.GetSiblingIndex());
        _entry = go.GetComponent<MenuItemGUI>();
        foreach (var localized in go.GetComponentsInChildren<LocalizedLabel>(true))
            UnityEngine.Object.DestroyImmediate(localized);
        _entry.on_pressed = new EventDelegate((EventDelegate.Callback)(() => clicked()));
        _entry.Init(menu);
        var label = go.GetComponent<UILabel>() ?? go.GetComponentInChildren<UILabel>(true);
        label.text = "Connection";
        _entry.Show();
        go.SetActive(true);

        var updated = items.ToList();
        updated.Insert(index, _entry);
        Traverse.Create(menu).Field("items").SetValue(updated.ToArray());
        menu.hideable_controls.Add(go);
        _previousDown = _continue.gamepad_item.down_item;
        _previousUp = _options.gamepad_item.up_item;
        _continue.gamepad_item.down_item = _entry.gamepad_item;
        _entry.gamepad_item.up_item = _continue.gamepad_item;
        _entry.gamepad_item.down_item = _options.gamepad_item;
        _entry.gamepad_item.left_item = _entry.gamepad_item.right_item = null;
        _options.gamepad_item.up_item = _entry.gamepad_item;
        foreach (var table in menu.GetComponentsInChildren<SimpleUITable>(true)) table.Reposition();
        foreach (var table in menu.GetComponentsInChildren<UITable>(true)) table.Reposition();
        if (menu.is_shown) menu.gamepad_controller.ReinitItems(false);
    }

    public void Dispose()
    {
        if (_menu == null || _entry == null) return;
        var items = Traverse.Create(_menu).Field("items").GetValue<MenuItemGUI[]>();
        Traverse.Create(_menu).Field("items").SetValue(items.Where(item => item != _entry).ToArray());
        _menu.hideable_controls.Remove(_entry.gameObject);
        _continue.gamepad_item.down_item = _previousDown;
        _options.gamepad_item.up_item = _previousUp;
        UnityEngine.Object.Destroy(_entry.gameObject);
    }
}
