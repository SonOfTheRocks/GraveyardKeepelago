namespace GraveyardKeepelago.GameModifications.UI;

// Shared by native row placement and the dialog body that reserves their space.
public static class NativeFormGeometry
{
    public static float ContentWidth(float nativeWidth, float fontSize) => nativeWidth + 4 * fontSize;
    public static float WindowWidth(float nativeWindowWidth, float nativeContentWidth, float contentWidth) =>
        nativeWindowWidth + contentWidth - nativeContentWidth;
    public static float RowTopOffset(int index, float lineHeight) => (1 + index * 2) * lineHeight;
    public static int StatusLineCount(int rowCount) => rowCount * 2 + 2;
}
