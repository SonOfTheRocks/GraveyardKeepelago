using System;
using System.Linq;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

internal static class NativeJournalPanels
{
    public static void Build(Transform parent, float width, float height, float footerY)
    {
        // Character-tab scene objects: retain the native texture, slicing, tint and corner artwork.
        var inventory = GUIElements.me.inventory.transform;
        var back = Source(inventory, "dark back");
        var line = Source(inventory, "top --------");
        var right = Source(inventory, "triangle separator");
        var left = Source(inventory, "triangle separator (1)");
        var background = Copy(back, parent, "Journal native background", 0);
        background.rawPivot = UIWidget.Pivot.Center;
        background.transform.localScale = Vector3.one;
        background.width = Mathf.RoundToInt(width);
        background.height = Mathf.RoundToInt(height);
        background.transform.localPosition = Vector3.zero;
        // The exported technology background is a reusable stone texture without labels or panel-specific artwork.
        if (background is UI2DSprite sprite)
        {
            var stone = GUIElements.me.tech_tree.GetComponentsInChildren<UI2DSprite>(true)
                .Select(widget => widget.sprite2D).FirstOrDefault(asset => asset != null && asset.name == "char_wndw_techtree_bg_1")
                ?? EasySpritesCollection.GetSprite("char_wndw_techtree_bg_1", not_found_is_valid: true);
            if (stone != null)
            {
                sprite.sprite2D = stone;
                sprite.type = UIBasicSprite.Type.Simple;
                sprite.fixedAspect = false;
                sprite.color = new Color(0.48f, 0.48f, 0.52f, 1);
            }
        }
        // Use the native content/header boundary, even where it lies above the map's clip region.
        var topY = parent.InverseTransformPoint(line.transform.position).y;
        // Keep the entire ornament above the clipped history panel, not just its lower half.
        var trim = new GameObject("Journal unclipped trim");
        trim.layer = parent.gameObject.layer;
        trim.transform.SetParent(parent, false);
        trim.AddComponent<UIPanel>().depth = parent.GetComponent<UIPanel>().depth + 2;
        var header = new GameObject("Journal top divider");
        header.layer = parent.gameObject.layer;
        header.transform.SetParent(trim.transform, false);
        Divider(header.transform, line, left, right, width, topY, "Journal top edge");
        MiddleDivider(trim.transform, line, width, footerY);
    }

    private static UIWidget Source(Transform root, string path) =>
        root.Find(path)?.GetComponent<UIWidget>() ??
        throw new InvalidOperationException("Native Character panel prototype missing: " + path);

    private static UIWidget Copy(UIWidget source, Transform parent, string name, int depth,
        bool preserveNativeTransform = false)
    {
        var copy = NativeUiPrototypeCatalog.Clone(source.gameObject, parent, name, preserveNativeTransform).GetComponent<UIWidget>();
        foreach (var collider in copy.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
        copy.depth = depth;
        return copy;
    }

    private static void MiddleDivider(Transform parent, UIWidget lineSource, float width, float y)
    {
        var line = Copy(lineSource, parent, "Journal footer edge", 1);
        line.rawPivot = UIWidget.Pivot.Center;
        line.width = Mathf.RoundToInt(width / Mathf.Abs(line.transform.localScale.x));
        line.transform.localPosition = new Vector3(0, y, 0);
        var technology = GUIElements.me.tech_tree.transform;
        const string path = "back/inner frame (above scroll)/";
        // Native object names are not a reliable left/right mapping. Keep each actual
        // world transform through Unity's reparenting, including inherited reflections.
        foreach (var name in new[] { "triangle separator", "triangle separator (1)" })
        {
            var original = Source(technology, path + name);
            var corner = Copy(original, parent, "Journal native middle ornament", 2, true);
            var position = corner.transform.localPosition;
            // Preserve native X, rotation, signed scale, sprite, UV flip and pivot.
            // Translate vertically by the drawn center so the full middle ornament meets the line.
            var drawn = corner.drawingDimensions;
            var center = corner.transform.TransformPoint(new Vector3(
                (drawn.x + drawn.z) / 2, (drawn.y + drawn.w) / 2, 0));
            position.y += y - parent.InverseTransformPoint(center).y;
            corner.transform.localPosition = position;
        }
    }

    private static void Divider(Transform parent, UIWidget source, UIWidget left, UIWidget right,
        float width, float y, string name)
    {
        var line = Copy(source, parent, name, 1);
        var scale = line.transform.localScale;
        line.rawPivot = UIWidget.Pivot.Center;
        line.width = Mathf.RoundToInt(width / Mathf.Abs(scale.x));
        line.transform.localPosition = new Vector3(0, y, 0);
        var sourceBottomLeft = parent.InverseTransformPoint(source.worldCorners[0]);
        var sourceTopRight = parent.InverseTransformPoint(source.worldCorners[2]);
        var sourceCenterY = (sourceBottomLeft.y + sourceTopRight.y) / 2;
        foreach (var cornerSource in new[] { left, right })
        {
            var corner = Copy(cornerSource, parent, name + " corner", 2);
            corner.drawRegion = new Vector4(0, 0, 1, 1);
            // Instantiate's lossy-scale conversion loses the mirrored left corner's sign.
            var cornerScale = corner.transform.localScale;
            cornerScale.x = Mathf.Abs(cornerScale.x) * Mathf.Sign(cornerSource.transform.localScale.x);
            corner.transform.localScale = cornerScale;
            var sourcePosition = parent.InverseTransformPoint(cornerSource.transform.position);
            var side = Mathf.Sign(cornerSource.transform.localPosition.x);
            var sourceEdge = side < 0 ? Mathf.Min(sourceBottomLeft.x, sourceTopRight.x) :
                Mathf.Max(sourceBottomLeft.x, sourceTopRight.x);
            // Preserve the native inset and pivot offset; the corner origin is not the line's endpoint.
            var x = JournalPanelGeometry.CornerPosition(sourcePosition.x, sourceEdge, side * width / 2);
            var position = new Vector3(x, JournalPanelGeometry.CornerPosition(sourcePosition.y, sourceCenterY, y), 0);
            corner.transform.localPosition = position;
        }
    }

}
