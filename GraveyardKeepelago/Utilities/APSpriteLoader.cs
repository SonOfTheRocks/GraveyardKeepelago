using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEngine;

public static class APSpriteLoader
{
    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
    private const int SpriteSize = 48;

    public static Sprite GetSprite(string fileName)
    {
        if (SpriteCache.TryGetValue(fileName, out var cached))
            return cached;

        var resourceName = $"GraveyardKeepelago.Resources.{fileName}.png";

        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            Debug.LogError($"Failed to load embedded resource: {resourceName}");
            return null;
        }

        byte[] data;
        using (var ms = new MemoryStream())
        {
            stream.CopyTo(ms);
            data = ms.ToArray();
        }

        // Load the 32x32 icon and pad it with transparent pixels to 48x48
        var sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        sourceTexture.LoadImage(data);
        sourceTexture.filterMode = FilterMode.Point;
        sourceTexture.wrapMode = TextureWrapMode.Clamp;

        var paddedTexture = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false);
        paddedTexture.filterMode = FilterMode.Point;
        paddedTexture.wrapMode = TextureWrapMode.Clamp;

        var transparentPixels = new Color32[SpriteSize * SpriteSize];
        paddedTexture.SetPixels32(transparentPixels);

        var offsetX = (SpriteSize - sourceTexture.width) / 2;
        var offsetY = (SpriteSize - sourceTexture.height) / 2;

        for (var y = 0; y < sourceTexture.height; y++)
        {
            for (var x = 0; x < sourceTexture.width; x++)
            {
                paddedTexture.SetPixel(x + offsetX, y + offsetY, sourceTexture.GetPixel(x, y));
            }
        }

        paddedTexture.Apply(false, false);

        var sprite = Sprite.Create(
            paddedTexture,
            new Rect(0, 0, SpriteSize, SpriteSize),
            new Vector2(0.5f, 0.5f),
            SpriteSize,
            0,
            SpriteMeshType.FullRect
        );

        SpriteCache[fileName] = sprite;

        return sprite;
    }
}
