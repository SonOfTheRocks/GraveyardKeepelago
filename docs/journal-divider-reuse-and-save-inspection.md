# Reuse the working divider; avoid full-save deserialization in the menu

## Evidence from the 30 October run

The previous ornament diagnostic reported the nominal left rail at +303 and the
nominal right rail at -303. Reconstructing this trim from Technologies paths and
standalone texture coordinates introduced wrong side assignments and guessed
pivots. Earlier pure geometry tests proved those assumptions internally consistent,
not that they matched the game's rendered widgets. They were inadequate visual
regressions.

NativeJournalPanels now builds the already working upper divider as a child group
and uses Unity Object.Instantiate to duplicate that complete group under the same
trim panel. The sole alteration is group.localPosition.y = footerY - topY. The footer
therefore retains the same sprites, pivots, signed scales, UV flips, dimensions,
relative offsets, depths and X positions as the upper divider. Removed the entire
Technologies-divider path, manual sprite replacement, rail lookup, custom facing
logic and pixel-derived stem math. The four obsolete tests of that removed stem
math were removed because their source/pixel assumptions were invalid, not because
of a failing assertion. Existing tests of the shared upper-divider positioning remain.
Actual appearance still requires an in-game check; passing .NET tests is not visual QA.

Play-click inspection measurements from the debug log:

| Save | File I/O | Full deserialize | Total |
| --- | ---: | ---: | ---: |
| 1 | 1.2 ms | 338.7 ms | 340.2 ms |
| 2 | 16.1 ms | 365.5 ms | 381.7 ms |
| 3 | 6.5 ms | 291.2 ms | 297.7 ms |
| 4 | 0.8 ms | 284.6 ms | 285.4 ms |

Total about 1305 ms; deserialization accounts for about 1280 ms. SaveSlotGUI.Show's
postfix was reconstructing all four GameSave object graphs synchronously just to
read unlocked_phrases for AP binding validation.

## Read-only phrase projection

SavePhraseReader implements a narrow read-only projection of the installed game's
SmartSerializer v1 format, based on the exported Assembly-CSharp-firstpass source.
It reads the native header and field hashes, skips typed world values recursively
without constructing them, and returns the unlocked_phrases list. Indexed strings
are decoded from the native encrypted string table only if needed. It has no Unity
calls, global serializer-cache mutation or game-object construction. Length, index,
nesting and stream boundaries are checked; corrupt inputs remain unselectable.
Unsupported versions/field types fall back to the original native deserializer.
JSON-format saves retain the native path.

This is only the menu eligibility check. ExistingSaveStartupPatch still validates
the actual fully loaded save through SaveStartupGuard before entering gameplay.
No save file, AP binding, receipt or world state is changed by inspection. No cache
or background serialization is introduced, so changed files and different connected
seeds are reevaluated as before. The existing timing log now includes the chosen mode.

New tests cover nested world data, byte arrays, Unicode, encrypted indexed strings,
empty/null phrases, actual SaveProgress identity decisions, corrupt/truncated input,
bad indices, missing/wrongly typed fields and unsupported formats.

A standalone .NET 8 probe read all four current .dat files (3.0–3.5 MB each) through
the same reader in 81.0 ms total including I/O. It returned 422, 2, 64 and 2 phrases;
saves 2 and 4 contained one AP record each. No save contents or identity values were
printed. This is not an in-game Unity/Mono timing; compare the next in-game log.
Probe source/results: out/save-phrase-probe/ and out/save-phrase-probe-results.log.

## Validation

```powershell
dotnet test GraveyardKeepelago.sln --no-restore --filter 'FullyQualifiedName~SavePhraseReader|FullyQualifiedName~Journal|FullyQualifiedName~SaveSlotEligibility|FullyQualifiedName~SaveStartup' -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-divider-reuse-save-projection/ --verbosity quiet
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-divider-reuse-save-projection/ --verbosity quiet
dotnet run --project out/save-phrase-probe/Probe.csproj -- 'C:/Users/Kai/AppData/LocalLow/Lazy Bear Games/Graveyard Keeper'
```

159 focused and all 623 tests passed, no failures or skips. Existing nullable/compiler
warnings remain. The new truncated-input test initially assumed InvalidDataException
derived from IOException; corrected the assertion to require the precise expected
exception for a missing pointer versus an invalid pointer. Production rejection was
already correct. Logs: out/divider-save-projection-focused.log and
out/divider-save-projection-full.log.

Installed with matching DLL/PDB hashes after the closed-game guard, then restarted
the debug game as PID 94544. Backup:
out/before-divider-reuse-save-projection-e1d0b6ed40484421b844c3918a3c2413/.
DLL SHA256: 8E0170A164AEF646F308AB39B5087678D476F5B3DE31416DC6475E3199ABC900.
