using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

internal static class NativeSpriteFrame
{
    // Fit the visible sprite, including Unity's trimmed transparent margins, without scaling its border.
    public static void Fit(UIBasicSprite frame, float width, float height, Vector2 center)
    {
        frame.pivot = UIWidget.Pivot.Center;
        frame.drawRegion = new Vector4(0, 0, 1, 1);
        frame.type = UIBasicSprite.Type.Sliced;
        if (frame.border == Vector4.zero)
            frame.border = Vector4.one;
        if (frame is UI2DSprite sprite)
            sprite.fixedAspect = false;
        frame.width = Mathf.RoundToInt(width);
        frame.height = Mathf.RoundToInt(height);
        var drawn = frame.drawingDimensions;
        frame.width += Mathf.RoundToInt(width - (drawn.z - drawn.x));
        frame.height += Mathf.RoundToInt(height - (drawn.w - drawn.y));
        drawn = frame.drawingDimensions;
        frame.transform.localPosition = new Vector3(
            center.x - (drawn.x + drawn.z) / 2, center.y - (drawn.y + drawn.w) / 2, 0);
    }
}
