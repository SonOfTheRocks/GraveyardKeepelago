# Building and testing

Use the .NET SDK specified in `global.json` and a local Graveyard Keeper installation.
Run the commands from the repository root in PowerShell.

```powershell
$game = (Resolve-Path 'C:\Games\Graveyard Keeper').Path
dotnet build GraveyardKeepelago.sln "-p:GamePath=$game"
dotnet test GraveyardKeepelago.sln "-p:GamePath=$game"
```

Without a `GamePath` override the project looks for a sibling
`Graveyard Keeper Debug` directory. Build and test write to the projects'
output directories. Set `PluginsPath` explicitly if you want the loose development
files copied to a separate directory.

## Single-DLL package

```powershell
./scripts/package-client.ps1 -GameDirectory $game
./scripts/verify-single-dll.ps1 -PluginPath <path-reported-by-package-client> -GameDirectory $game
```

Packaging builds in a fresh staging directory under `out/` and puts the merged
`GraveyardKeepelago.dll` under `out/releases/`. It bundles the Archipelago client
library and both KaitoKid libraries. BepInEx, Harmony, and the game assemblies come
from the game installation. If testing the packaged DLL in BepInEx, use a plugin
directory free of the loose development DLLs.

The Windows smoke test copies only the packaged client to an isolated directory.
It checks type discovery, BepInEx metadata, merged dependencies, and initialization
of the local ID cache without sidecar tables. It does not start Unity or connect to
a multiworld. The package is for testing the existing client runtime. Use a client
with shared-data v9 runtime support for the current APWorld.
