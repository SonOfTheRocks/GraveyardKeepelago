using UnityEngine;

namespace GraveyardKeepelago.GameModifications.UI;

public static class TrapBadgeSprite
{
    public const string NativeSpriteName = "i_skull";
    // Use the original game sprite directly: no atlas readback or generated texture.
    public static Sprite Get() => EasySpritesCollection.GetSprite(NativeSpriteName);
}
