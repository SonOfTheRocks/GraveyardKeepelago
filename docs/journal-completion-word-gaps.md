# Item and location completion across word gaps

The user reported that !hint bow finds the four bowl recipes but !hint Create: bow
finds none. Matching previously required the entire query to be a contiguous prefix
or substring of the candidate (or a near-complete name with a single typo).
Create: Ceramic bowls and Create: A bowl of ... contain intervening words, so the
longer query could not match even though both pieces occur in the names.

Argument matching now also accepts whitespace-separated terms found in order,
case-insensitively, with intervening characters/words. Each term must match after
the previous one; one occurrence cannot satisfy repeated terms. Contiguous prefix
and substring matches rank first; gapped matches join the existing fallback tier.
This applies to !hint, !getitem, !hint_location, !missing and !checked. Command-name
matching and canonical inserted names remain unchanged. Unknown commands, free text,
mid-line editing and selected input retain their existing behavior.

New tests use the real shared item contract to verify all four bowl recipes for
both !hint and !getitem, category prefixes, case and extra whitespace. They also
cover location gaps, missing/out-of-order/repeated terms and match ranking. The six
positive regression cases failed before the production change, as expected.

Validation (DOTNET_CLI_HOME=.dotnet-home):

```powershell
dotnet test GraveyardKeepelago.sln --no-restore --filter 'FullyQualifiedName~JournalCompletion' -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-completion-word-gaps/ --verbosity quiet
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-completion-word-gaps/ --verbosity quiet
```

37 focused and all 636 tests passed without failures or skips. Existing compiler
warnings remain. Logs: out/completion-word-gaps-before.log (expected regression
failures), out/completion-word-gaps-focused.log and out/completion-word-gaps-full.log.

Installed with matching DLL/PDB hashes after confirming the debug game was closed;
restarted as PID 97376. Backup:
out/before-completion-word-gaps-935ba764461f4b23bf98e86f7fcbd1b1/.
DLL SHA256: 6AA6FFA8FA50F3847E44B0CCB19FAA30253D8B49F713FB2412EEA853073707E0.
