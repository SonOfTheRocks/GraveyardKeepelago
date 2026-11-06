using System;
using System.IO;
using System.Reflection;
using System.Collections;
public class Smoke {
 public static int Main(string[] args) {
  try {
   string dll = Path.GetFullPath(args[0]), game = args[1];
   AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs e) {
    string name = new AssemblyName(e.Name).Name;
    if (name.StartsWith("KaitoKid") || name == "Archipelago.MultiClient.Net") throw new Exception("Unmerged dependency: " + name);
    foreach(string folder in new string[] {Path.Combine(game,"BepInEx/core"),Path.Combine(game,"Graveyard Keeper_Data/Managed")}) {
     string candidate=Path.Combine(folder,name+".dll"); if(File.Exists(candidate)) return Assembly.Load(File.ReadAllBytes(candidate));
    }
    return null;
   };
   Assembly a=Assembly.LoadFrom(dll);
   foreach(AssemblyName dependency in a.GetReferencedAssemblies()) {
    if(dependency.Name.StartsWith("KaitoKid") || dependency.Name == "Archipelago.MultiClient.Net") throw new Exception("External dependency remains: "+dependency.Name);
   }
   // Same reflection step used by plugin discovery, without constructing Unity objects.
   Type[] types=a.GetTypes();
   object contract=a.GetType("GraveyardKeepelago.Logic.ClientContractLoader").GetMethod("LoadDefault").Invoke(null,null);
   object items=contract.GetType().GetProperty("Items").GetValue(contract,null);
   int count=(int)items.GetType().GetProperty("Count").GetValue(items,null);
   if(count!=488) throw new Exception("Unexpected item contract: "+count);
   object tech=a.GetType("GraveyardKeepelago.Technologies.TechnologyCatalog").GetMethod("LoadDefault").Invoke(null,null);
   if(tech==null) throw new Exception("Missing technologies");
   string cache=Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"absent-cache");
   object package=a.GetType("GraveyardKeepelago.Archipelago.GKDataPackageCache").GetConstructor(new Type[]{typeof(string),typeof(string[])}).Invoke(new object[]{"graveyard_keeper",new string[]{cache}});
   if(Directory.Exists(cache)) throw new Exception("Fallback created unwanted installation files");
   Console.WriteLine("PASS: plugin type discovery, merged dependencies, embedded 488-item contract, technologies and empty data-package cache. Types: "+types.Length);
   return 0;
  } catch(Exception ex) { Console.WriteLine(ex.ToString()); if(ex is ReflectionTypeLoadException) foreach(Exception inner in ((ReflectionTypeLoadException)ex).LoaderExceptions) Console.WriteLine(inner); return 1; }
 }
}


