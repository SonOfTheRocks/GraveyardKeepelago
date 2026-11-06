using System.Linq;
using System.Runtime.CompilerServices;
using GraveyardKeepelago.GameModifications.UI;
using HarmonyLib;
using UnityEngine;

namespace GraveyardKeepelago.Technologies;

[HarmonyPatch(typeof(TechTreeGUIUnlockItem), nameof(TechTreeGUIUnlockItem.Draw), typeof(TechUnlock), typeof(bool))]
public static class TechnologyNpcIconPatch
{
    private static readonly ConditionalWeakTable<TechTreeGUIUnlockItem, IconPosition> Positions = new();

    public static void RestoreAll()
    {
        if (GUIElements.me == null || GUIElements.me.tech_tree == null) return;
        foreach (var row in GUIElements.me.tech_tree.GetComponentsInChildren<TechTreeGUIUnlockItem>(true))
        {
            if (Positions.TryGetValue(row, out var position)) position.Restore();
            Positions.Remove(row);
        }
    }

    private static void Prefix(TechTreeGUIUnlockItem __instance)
    {
        // A row can be redrawn or reused for a different reward, including a non-NPC item.
        if (Positions.TryGetValue(__instance, out var position)) position.Restore();
    }

    private static void Postfix(TechTreeGUIUnlockItem __instance, TechUnlock unlock)
    {
        var runtime = TechnologyRuntime.Current;
        if (unlock == null || runtime?.IsActive != true || !runtime.IsRelationPreview(unlock) ||
            __instance.GetComponentInParent<TechTreeGUIItem>() == null || __instance.spr == null ||
            __instance.spr.sprite2D?.name.StartsWith("char_", System.StringComparison.Ordinal) != true)
            return;
        Positions.GetValue(__instance, row => new IconPosition(row.spr)).Lower();
    }

    private sealed class IconPosition
    {
        private readonly UI2DSprite _icon;
        private readonly Vector3 _original;
        private readonly Transform[] _children;
        private readonly Vector3[] _childPositions;

        public IconPosition(UI2DSprite icon)
        {
            _icon = icon;
            _original = icon.transform.localPosition;
            _children = icon.transform.Cast<Transform>().ToArray();
            _childPositions = _children.Select(child => child.localPosition).ToArray();
        }

        public void Lower()
        {
            if (_icon == null) return;
            Restore();
            // The icon's frame may be a child of the sprite. Keep that frame and other
            // child widgets in place while moving only the character artwork.
            var childWorldPositions = _children.Select(child => child == null ? Vector3.zero : child.position).ToArray();
            var position = _original;
            position.y = NpcResearchIconPlacement.PositionY(_original.y, _icon.height, true, true);
            _icon.transform.localPosition = position;
            for (var index = 0; index < _children.Length; index++)
                if (_children[index] != null) _children[index].position = childWorldPositions[index];
        }

        public void Restore()
        {
            if (_icon == null) return;
            _icon.transform.localPosition = _original;
            for (var index = 0; index < _children.Length; index++)
                if (_children[index] != null) _children[index].localPosition = _childPositions[index];
        }
    }
}
