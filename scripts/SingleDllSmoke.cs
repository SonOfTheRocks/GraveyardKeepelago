using System;
using System.IO;
using System.Linq;
using System.Reflection;

internal static class SingleDllSmoke
{
    public static int Main(string[] args)
    {
        try
        {
            string dll = Path.GetFullPath(args[0]);
            string game = args[1];
            AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) =>
            {
                string name = new AssemblyName(eventArgs.Name).Name;
                if (IsMergedDependency(name))
                    throw new InvalidOperationException("Unmerged dependency: " + name);

                foreach (string directory in new[] { "BepInEx/core", "Graveyard Keeper_Data/Managed" })
                {
                    string candidate = Path.Combine(game, directory, name + ".dll");
                    if (File.Exists(candidate))
                        return Assembly.Load(File.ReadAllBytes(candidate));
                }
                return null;
            };

            Assembly assembly = Assembly.LoadFrom(dll);
            foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
                if (IsMergedDependency(reference.Name))
                    throw new InvalidOperationException("External dependency remains: " + reference.Name);

            Type[] types = assembly.GetTypes();
            Type plugin = assembly.GetType("GraveyardKeepelago.Plugin", throwOnError: true);
            if (!plugin.GetCustomAttributesData().Any(attribute => attribute.AttributeType.FullName == "BepInEx.BepInPlugin"))
                throw new InvalidOperationException("BepInEx plugin metadata is missing.");

            // The legacy cache must be usable without the optional empty ID tables.
            Type cacheType = assembly.GetType("GraveyardKeepelago.Archipelago.GKDataPackageCache", throwOnError: true);
            string missingCache = Path.Combine(Path.GetDirectoryName(dll), "absent-cache");
            object cache = Activator.CreateInstance(cacheType, "graveyard_keeper", new[] { missingCache });
            foreach (string method in new[] { "GetAllItems", "GetAllLocations" })
            {
                var entries = (System.Collections.IEnumerable)cacheType.GetMethod(method).Invoke(cache, null);
                if (entries.Cast<object>().Any())
                    throw new InvalidOperationException("Expected an empty ID cache from " + method);
            }

            if (Directory.Exists(missingCache))
                throw new InvalidOperationException("The local ID cache created sidecar files.");

            Console.WriteLine("PASS: plugin discovery, merged dependencies, and empty local ID cache. Types: " + types.Length);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            var typeLoad = exception as ReflectionTypeLoadException;
            if (typeLoad != null)
                foreach (Exception cause in typeLoad.LoaderExceptions)
                    Console.Error.WriteLine(cause);
            return 1;
        }
    }

    private static bool IsMergedDependency(string name)
    {
        return name == "Archipelago.MultiClient.Net" ||
            name == "KaitoKid.ArchipelagoUtilities.Net" ||
            name == "KaitoKid.Utilities.Interfaces";
    }
}
