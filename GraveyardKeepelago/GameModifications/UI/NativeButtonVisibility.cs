namespace GraveyardKeepelago.GameModifications.UI;

public static class NativeButtonVisibility
{
    public static bool IsUsable(float viewportX, float viewportY, bool frameVisible, bool iconVisible, bool clickable) =>
        viewportX > 0 && viewportX < 1 && viewportY > 0 && viewportY < 1 &&
        frameVisible && iconVisible && clickable;
}
