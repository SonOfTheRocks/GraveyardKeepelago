# Native middle ornaments and item argument completion (30 October)

The user confirmed that the upper ornaments have a thicker upper transition and
therefore are the wrong artwork for a middle divider. The footer now copies the
actual Technologies middle ornaments from back/inner frame (above scroll), keeping
their own sprites, dimensions, pivots, UV flips and complete world transforms.

NativeUiPrototypeCatalog.Clone has an opt-in preserveNativeTransform argument.
It passes worldPositionStays=true to Unity's Instantiate and skips the catalog's
usual lossy-scale normalization in that mode. This preserves inherited reflections
and native X positions rather than interpreting the misleading L/R object names
or rebuilding signed transforms. Ordinary form cloning retains its previous default.
After cloning, only Y changes to center the drawn middle ornament on the footer line.
The upper divider stays as before. No manual UV mirroring, replacement texture or
stem/pixel offsets are used. Native appearance still needs an in-game check.

Both !hint and !getitem already had item-parameter matching, including case-insensitive
commands and immediate suggestions after a space. However, Plugin passed the legacy
DataPackageCache's item entries; the shipped graveyard_keeper_item_table.json contains
an empty items object. The journal now uses GKItemRegistry.Names, populated from the
same client contract and factory used to apply received items. No extra manifest load
is performed per menu open and no names are inferred from previously received items.
The 488 registered names are available even with the legacy cache empty. This changes
neither canonical item names/IDs nor server command permissions.

Regression tests build the real registry from the shared contract, then verify all
488 parameter suggestions, immediate activation after !hint/!getitem/!getItem plus a
space, and matching of full item names. Existing matching/namespace/mid-line-editing
tests remain. The source change is covered beyond the synthetic-name unit tests
that previously failed to detect the empty production data source.

Validation (DOTNET_CLI_HOME=.dotnet-home):

```powershell
dotnet test GraveyardKeepelago.sln --no-restore --filter 'FullyQualifiedName~Journal|FullyQualifiedName~ClientContract|FullyQualifiedName~ItemParserRouting' -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-native-middle-item-completion/ --verbosity quiet
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-native-middle-item-completion/ --verbosity quiet
```

138 focused and all 627 tests passed without failures or skips. Existing nullable
compiler warnings remain. Logs: out/native-middle-item-completion-focused.log and
out/native-middle-item-completion-full.log.

The previous phrase-reader revision is now confirmed in-game: the four inspection
times are 23.3, 16.6, 16.2 and 16.3 ms, totaling 72.4 ms, versus 1305 ms previously.
All four use phrase projection without fallback. This matches the user's report
that the new reader works well.

Installed after confirming the debug game was closed, verified DLL/PDB hashes and
restarted PID 23064. Backup:
out/before-native-middle-item-completion-b307b0aac2924c08ae8a35df03035df6/.
DLL SHA256: 91385B4877EFF941F2793084427B3D6FB21B537E04E9DA452E2EF2AEC5CBDEB6.
