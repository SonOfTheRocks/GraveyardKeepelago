using System.Runtime.CompilerServices;
using GraveyardKeepelago.GameModifications.UI;
using HarmonyLib;
using UnityEngine;

namespace GraveyardKeepelago.Technologies;

[HarmonyPatch(typeof(TechTreeGUIUnlockItem), nameof(TechTreeGUIUnlockItem.Draw), typeof(TechUnlock), typeof(bool))]
public static class TechnologyBuffIconPatch
{
    private static readonly ConditionalWeakTable<TechTreeGUIUnlockItem, IconSize> Sizes = new();

    private static void Prefix(TechTreeGUIUnlockItem __instance)
    {
        if (Sizes.TryGetValue(__instance, out var size)) size.Restore();
    }

    private static void Postfix(TechTreeGUIUnlockItem __instance, TechUnlock unlock)
    {
        var runtime = TechnologyRuntime.Current;
        if (unlock == null || runtime?.IsActive != true || !runtime.IsPermanentBuffPreview(unlock) ||
            __instance.GetComponentInParent<TechTreeGUIItem>() == null || __instance.spr?.sprite2D == null) return;
        Sizes.GetValue(__instance, row => new IconSize(row.spr)).Fit();
    }

    public static void RestoreAll()
    {
        if (GUIElements.me?.tech_tree == null) return;
        foreach (var row in GUIElements.me.tech_tree.GetComponentsInChildren<TechTreeGUIUnlockItem>(true))
        {
            if (Sizes.TryGetValue(row, out var size)) size.Restore();
            Sizes.Remove(row);
        }
    }

    private sealed class IconSize
    {
        private readonly UI2DSprite _icon;
        private readonly int _width, _height;
        private readonly Vector3 _position;
        public IconSize(UI2DSprite icon)
        { _icon = icon; _width = icon.width; _height = icon.height; _position = icon.transform.localPosition; }

        public void Fit()
        {
            Restore();
            var sprite = _icon.sprite2D;
            var pivot = _icon.pivotOffset;
            var fit = BuffResearchIconGeometry.Fit(_width, _height, sprite.rect.width, sprite.rect.height,
                _icon.pixelSize, pivot.x, pivot.y);
            _icon.width = Mathf.RoundToInt(fit.Width);
            _icon.height = Mathf.RoundToInt(fit.Height);
            _icon.transform.localPosition = _position + new Vector3(fit.X, fit.Y, 0);
        }

        public void Restore()
        {
            if (_icon == null) return;
            _icon.width = _width;
            _icon.height = _height;
            _icon.transform.localPosition = _position;
        }
    }
}
