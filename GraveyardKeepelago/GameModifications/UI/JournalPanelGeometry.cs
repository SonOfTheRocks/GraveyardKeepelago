namespace GraveyardKeepelago.GameModifications.UI;

public static class JournalPanelGeometry
{
    public static float CornerPosition(float nativeOrigin, float nativeEdge, float targetEdge)
        => targetEdge + nativeOrigin - nativeEdge;

}
