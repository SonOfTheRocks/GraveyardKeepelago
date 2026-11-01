using System;

namespace GraveyardKeepelago.GameModifications.UI;

public static class BadgePixels
{
    // RGBA buffers use Unity's bottom-left pixel origin. Match actual visible
    // pixels, since atlas sprite rectangles can include a full transparent canvas.
    public static byte[] Compose(byte[] skull, int width, int height, byte[] star, int starWidth, int starHeight)
    {
        var source = Bounds(skull, width, height);
        var target = Bounds(star, starWidth, starHeight);
        var result = new byte[starWidth * starHeight * 4];
        if (source.Width == 0 || target.Width == 0) return result;
        var scale = QualityBadgeGeometry.Fit(source.Width, source.Height, target.Width, target.Height, 1, 1).Scale;
        var w = Math.Max(1, (int)Math.Floor(source.Width * scale));
        var h = Math.Max(1, (int)Math.Floor(source.Height * scale));
        var left = target.X + target.Width - w;
        var bottom = target.Y + target.Height - h;
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var sx = source.X + Math.Min(source.Width - 1, (int)((x + 0.5f) * source.Width / w));
                var sy = source.Y + Math.Min(source.Height - 1, (int)((y + 0.5f) * source.Height / h));
                Array.Copy(skull, (sy * width + sx) * 4, result, ((bottom + y) * starWidth + left + x) * 4, 4);
            }
        return result;
    }

    public static (int X, int Y, int Width, int Height) Bounds(byte[] rgba, int width, int height)
    {
        if (width <= 0 || height <= 0 || rgba == null || rgba.Length != width * height * 4)
            throw new ArgumentException("Invalid badge pixel buffer.");
        var minX = width; var minY = height; var maxX = -1; var maxY = -1;
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                if (rgba[(y * width + x) * 4 + 3] != 0)
                {
                    minX = Math.Min(minX, x); minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
                }
        return maxX < 0 ? (0, 0, 0, 0) : (minX, minY, maxX - minX + 1, maxY - minY + 1);
    }
}
