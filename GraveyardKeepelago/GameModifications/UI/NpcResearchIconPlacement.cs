using System;

namespace GraveyardKeepelago.GameModifications.UI;

public static class NpcResearchIconPlacement
{
    public static float PositionY(float originalY, float iconHeight, bool researchTile, bool npcPreview)
    {
        if (!researchTile || !npcPreview || !(iconHeight > 0) || float.IsInfinity(iconHeight))
            return originalY;
        // Halve the previous rounded offset, including half-pixel values for odd offsets.
        return originalY - (float)Math.Round(iconHeight / 5f) / 2f;
    }
}
