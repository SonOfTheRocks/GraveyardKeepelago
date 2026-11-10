#if DEBUG
using System;
using System.Linq;
using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

internal static class NativeUiInspector
{
    public static void Inspect(Action<string> log)
    {
        var gui = GUIElements.me;
        if (gui == null)
        {
            log("Native UI inspection: GUIElements is not ready.");
            return;
        }
        Dump(gui.item_count?.transform, log);
        Dump(gui.dialog?.transform, log);
        Dump(gui.options?.transform, log);
        Dump(gui.main_menu?.transform.Find("Archipelago Connection Button"), log);
        foreach (var form in gui.GetComponentsInChildren<NativeFormMarker>(true))
            Dump(form.transform, log);
        foreach (var toggle in gui.GetComponentsInChildren<UIToggle>(true))
            Dump(toggle.transform, log);
    }

    private static void Dump(Transform root, Action<string> log)
    {
        if (root == null) return;
        foreach (var node in root.GetComponentsInChildren<Transform>(true))
        {
            var path = node.name;
            for (var parent = node.parent; parent != null; parent = parent.parent)
                path = parent.name + "/" + path;
            var panel = node.GetComponentInParent<UIPanel>();
            var widget = node.GetComponent<UIWidget>();
            var label = node.GetComponent<UILabel>();
            var sprite = node.GetComponent<UISprite>();
            var box = node.GetComponent<BoxCollider2D>();
            var polygon = node.GetComponent<PolygonCollider2D>();
            var sprite2D = node.GetComponent<UI2DSprite>();
            var localized = node.GetComponent<LocalizedLabel>();
            var optionsLabel = label != null && node.GetComponentInParent<OptionsMenuGUI>() != null;
            log($"Native UI {path}: active={node.gameObject.activeSelf}/{node.gameObject.activeInHierarchy}; " +
                $"components={string.Join(",", node.GetComponents<Component>().Select(c => c == null ? "missing" : c.GetType().Name))}; " +
                $"panel={panel?.name}; position={node.localPosition}; scale={node.localScale}; color={widget?.color}; " +
                $"widget={(widget == null ? "none" : $"{widget.width}x{widget.height}, depth={widget.depth}")}; " +
                $"collider={(box == null ? node.GetComponent<Collider2D>()?.GetType().Name : $"BoxCollider2D size={box.size}, offset={box.offset}, enabled={box.enabled}")}; " +
                $"font={label?.bitmapFont?.name}/{label?.trueTypeFont?.name}; token={localized?.token}; size={label?.fontSize}/{label?.finalFontSize}; " +
                $"overflow={label?.overflowMethod}; spacing={label?.spacingX}/{label?.spacingY}; effect={label?.effectStyle}; " +
                $"effectColor={label?.effectColor}; effectDistance={label?.effectDistance}; fontStyle={label?.fontStyle}; " +
                $"gradient={label?.applyGradient}/{label?.gradientTop}/{label?.gradientBottom}; worldScale={node.lossyScale}; " +
                $"nativeOptionsText={(optionsLabel && localized != null ? label.text : "")}; " +
                $"sprite={sprite?.spriteName}; atlas={sprite?.atlas?.name}; " +
                $"sprite2D={sprite2D?.sprite2D?.name}; type={sprite2D?.type}; border={sprite2D?.border}; " +
                $"drawing={sprite2D?.drawingDimensions}; polygon={(polygon == null ? "none" : string.Join(",", polygon.points.Select(p => p.ToString())))}");
        }
    }
}
#endif
