# Building and testing

Use the .NET SDK specified in `global.json` and a local Graveyard Keeper installation.
Run the commands from the repository root in PowerShell.

```powershell
$game = (Resolve-Path 'C:\Games\Graveyard Keeper').Path
dotnet build GraveyardKeepelago.sln "-p:GamePath=$game"
dotnet test GraveyardKeepelago.sln "-p:GamePath=$game"
```

An explicit `GamePath` takes precedence. To set a local default for Rider,
command-line builds and packaging, create the Git-ignored `GamePath.props.user`
in the repository root. For a debug copy next to the repository, use:

```xml
<Project>
    <PropertyGroup>
        <GamePath>$(MSBuildThisFileDirectory)..\Graveyard Keeper Debug</GamePath>
    </PropertyGroup>
</Project>
```

Without an override, the build checks the default Steam library under
`Program Files (x86)\Steam\steamapps\common` and then the usual GOG Galaxy
library under `Program Files (x86)\GOG Galaxy\Games`. Use an override for a
custom library or a standalone GOG installation. The game directory must
contain both managed game assemblies used by the project.

Build and test write to the projects' output directories. Set `PluginsPath`
explicitly to copy the loose development files to a separate directory.

## Single-DLL package

```powershell
./scripts/package-client.ps1 -GameDirectory $game
./scripts/verify-single-dll.ps1 -PluginPath <path-reported-by-package-client> -GameDirectory $game
```

If you omit `-GameDirectory` from the packaging command, it uses the same
`GamePath` resolution as a normal build.

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
