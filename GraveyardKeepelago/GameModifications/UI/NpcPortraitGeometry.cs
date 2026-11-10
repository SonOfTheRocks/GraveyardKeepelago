using System;

namespace GraveyardKeepelago.GameModifications.UI;

public static class NpcPortraitGeometry
{
    public static float FitScale(float availableWidth, float availableHeight, float portraitWidth, float portraitHeight)
    {
        if (!(availableWidth > 0) || !(availableHeight > 0) || !(portraitWidth > 0) || !(portraitHeight > 0))
            throw new ArgumentOutOfRangeException(nameof(availableWidth));
        return Math.Min(availableWidth / portraitWidth, availableHeight / portraitHeight);
    }
}
