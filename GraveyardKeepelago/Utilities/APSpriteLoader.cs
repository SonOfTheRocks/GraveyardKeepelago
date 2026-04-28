using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEngine;

public static class APSpriteLoader
{
    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();


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

        // size doesn't matter, LoadImage replaces width and height automatically
        var tex = new Texture2D(1, 1);
        tex.LoadImage(data);

        var sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
        
        SpriteCache[fileName] = sprite;

        return sprite;
    }
}