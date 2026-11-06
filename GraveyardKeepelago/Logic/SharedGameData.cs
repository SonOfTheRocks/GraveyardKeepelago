using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace GraveyardKeepelago.Logic;

// Runtime data always belongs to this DLL; adjacent files cannot override its contract.
public static class SharedGameData
{
    private const string Prefix = "GraveyardKeepelago.Data/";
    private static readonly IReadOnlyDictionary<string, string> Resources =
        typeof(SharedGameData).Assembly.GetManifestResourceNames()
            .Where(name => name.Replace((char)92, '/').StartsWith(Prefix, StringComparison.Ordinal))
            .ToDictionary(name => name.Replace((char)92, '/').Substring(Prefix.Length), name => name, StringComparer.Ordinal);
    public static IEnumerable<string> Files => Resources.Keys.OrderBy(name => name, StringComparer.Ordinal);
    public static string ReadText(string path)
    {
        if (!Resources.TryGetValue(path.Replace((char)92, '/'), out var resource))
            throw new FileNotFoundException("Required embedded game data is missing", path);
        using var stream = typeof(SharedGameData).Assembly.GetManifestResourceStream(resource);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    public static T Read<T>(string path) => JsonConvert.DeserializeObject<T>(ReadText(path)) ??
        throw new InvalidDataException("Embedded game data is empty: " + path);
}



