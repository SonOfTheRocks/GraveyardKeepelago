using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

[HarmonyPatch(typeof(TechTreeGUIUnlockItem), nameof(TechTreeGUIUnlockItem.Draw), typeof(TechUnlock.TechUnlockData), typeof(TechUnlock))]
public static class TrapQualityBadgePatch
{
    private static readonly ConditionalWeakTable<TechTreeGUIUnlockItem, BadgeSlot> Slots = new();

    private static void Prefix(TechTreeGUIUnlockItem __instance)
    {
        if (Slots.TryGetValue(__instance, out var slot)) slot.Restore();
    }

    private static void Postfix(TechTreeGUIUnlockItem __instance, TechUnlock.TechUnlockData __0)
    {
        var badge = __instance.item_quality;
        // The data already identifies this as a skull in the quality slot, including receipts during setup.
        if (badge == null || !QualityBadgeGeometry.IsTrapBadge(__0?.quality_icon?.name)) return;
        Slots.GetValue(__instance, _ => new BadgeSlot(badge)).Fit();
    }

    public static void RestoreAll()
    {
        if (GUIElements.me == null) return;
        foreach (var row in GUIElements.me.GetComponentsInChildren<TechTreeGUIUnlockItem>(true))
        {
            if (Slots.TryGetValue(row, out var slot)) slot.Restore();
            Slots.Remove(row);
        }
    }

    private sealed class BadgeSlot
    {
        private readonly UI2DSprite _badge;
        private readonly int _width, _height;
        private readonly Vector3 _position;
        private readonly Vector3 _scale;
        private readonly bool _fixedAspect;
        private readonly Vector4 _drawRegion;
        private bool _logged;
        public BadgeSlot(UI2DSprite badge)
        {
            _badge = badge; _width = badge.width; _height = badge.height;
            _position = badge.transform.localPosition; _scale = badge.transform.localScale;
            _fixedAspect = badge.fixedAspect; _drawRegion = badge.drawRegion;
        }
        public void Fit()
        {
            Restore();
            var skull = _badge.sprite2D;
            var star = EasySpritesCollection.GetSprite("item_star_3");
            if (star == null) return;
            _badge.fixedAspect = false;
            _badge.drawRegion = new Vector4(0, 0, 1, 1);
            _badge.sprite2D = star;
            var reference = _badge.drawingDimensions;
            var target = QualityBadgeGeometry.StarReference(reference.x, reference.y, reference.z, reference.w,
                star.textureRect.width, star.textureRect.height, _badge.pixelSize);
            _badge.sprite2D = skull;
            var visible = _badge.drawingDimensions;
            var fit = QualityBadgeGeometry.FitRendered(visible.x, visible.y, visible.z, visible.w,
                target.Left, target.Bottom, target.Right, target.Top);
            _badge.transform.localScale = _scale * fit.Scale;
            _badge.transform.localPosition = _position + new Vector3(fit.X * _scale.x, fit.Y * _scale.y, 0);
            if (!_logged)
            {
                Debug.Log($"[AP trap badge] star={reference}, target={target}, skull={visible}, scale={fit.Scale}, slot={_width}x{_height}, nativeScale={_scale}");
                _logged = true;
            }
        }
        public void Restore()
        {
            if (_badge == null) return;
            _badge.width = _width;
            _badge.height = _height;
            _badge.fixedAspect = _fixedAspect;
            _badge.drawRegion = _drawRegion;
            _badge.transform.localPosition = _position;
            _badge.transform.localScale = _scale;
        }
    }
}
