# Development documentation

[Back to the README](../README.md)

Player-facing instructions live in the [player guide](player-guide.md) and
[troubleshooting guide](troubleshooting.md). The root README is the short entry point
for prospective players; keep implementation history and test procedures here in `docs`.

## Implementation and validation

- [Technology research and unlocks](technology-implementation.md)
- [Save binding and startup protection](save-startup-protection.md)
- [Explicit connection lifecycle](manual-connection.md)
- [Journal implementation and verification](archipelago-journal.md)
- [Command completion matching](journal-completion-word-gaps.md)
- [AP item colors](ap-item-colors.md)
- [Monster traps and seed options](traps.md)
- [Temporary trap monster safety](temporary-trap-monster-safety.md)
- [Tutorial access](tutorial-access.md)
- [Sleeping at full energy](full-energy-sleep.md)

These notes include historical observations and validation results; version numbers in
older entries describe those changes, not necessarily the current compatibility contract.
The current shared-data version comes from `External/GraveyardKeeperData/manifest.json`.

## Building and testing

See [the contributor guide](../AGENTS.md) for architecture, shared-contract requirements,
media policy and test expectations. The SDK is pinned in `global.json`. Initialize the
`External/GraveyardKeeperData` submodule and provide the local game assemblies referenced
by `GraveyardKeepelago/GraveyardKeepelago.csproj`.

```powershell
dotnet build GraveyardKeepelago.sln -p:PluginsPath=out/local-build/
dotnet test GraveyardKeepelago.sln -p:PluginsPath=out/local-build/
```

Builds copy plugin files after compilation. Override `PluginsPath` to use an isolated
output directory; the default targets the local debug game's plugin folder. Do not
replace plugin files while that game is running.

Automated tests cover decisions and lifecycle behavior. Native UI rendering, input,
controller interaction and resolution/UI-scale changes also need in-game verification;
the relevant implementation notes describe the checks. Record results there rather
than adding test logs or visual approval history to the README.

## Single-DLL release

Run `./scripts/package-client.ps1` (`-NoRestore` after restoring dependencies) to build
a standalone `GraveyardKeepelago.dll` under `out/releases/`. The script uses a fresh
staging directory, embeds shared JSON data and merges the three client libraries with
ILRepack. Game assemblies, BepInEx and Harmony are not bundled. The version and hash
identify the release directory; players only copy its DLL.

Normal builds/tests retain separate library references for development. Production
`LoadDefault` methods always read embedded resources and ignore adjacent JSON files.
Explicit disk loading remains available for fixtures. The former empty local ID tables are no longer needed; server Data Packages remain authoritative.

When upgrading an older development installation, remove its old client dependency
DLLs and static data from that plugin folder so only the standalone DLL remains. Keep
BepInEx's core files and player configuration/journals. Verify the standalone artifact
in isolation as well as running the normal test suite; merging changes assembly identities.


Standalone verification (Windows/.NET Framework, using installed game/BepInEx assemblies):

```powershell
./scripts/verify-single-dll.ps1 -PluginPath <release-DLL> -GameDirectory <game-folder>
```

This copies only the standalone client into an isolated test directory. It rejects any
attempt to load the old three client DLLs, verifies reflection-based type discovery,
loads the embedded contract and technologies, and constructs the empty ID fallback
without any data files. Actual Unity UI interaction still needs an in-game check.
