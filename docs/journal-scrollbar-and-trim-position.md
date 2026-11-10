# Journal scrollbar and rendered ornament placement

The 29 October screenshot shows the correctly facing middle ornaments detached
from the side rails. Their previous placement transferred transform origins from
the Technologies panel while also mirroring the artwork around its native pivot.
The journal now measures the cloned ornament's drawing rectangle after mirroring,
aligns its visible outer edge with the journal's actual divider endpoint, and centers
it vertically on the divider. No reference to another panel's width or rail position
remains. Native ornament artwork is reused unchanged.

The history has a separate scrollbar column and gap, reducing message wrapping width
accordingly. A fresh NGUI UIScrollBar uses existing native selection backgrounds as
track and handle. Clicking the track, dragging the handle, mouse-wheel, dragging the
history, keyboard/controller scrolling and Latest all share JournalScrollPosition.
The handle reflects the visible fraction, with a minimum usable height, and hides
when there is no overflow. Synchronization suppresses recursive change callbacks.
No generated media was added.

Regression tests cover dragging, clamping, short/empty history, new-message anchoring,
Latest, non-overlapping layout, and visible-edge placement with different pivots.
The layout assertion for the previous shared history/control right edge now checks
the scrollbar's right edge instead: text explicitly stops before that column.

Validation (DOTNET_CLI_HOME set to the repository's .dotnet-home):

```powershell
dotnet test GraveyardKeepelago.sln --no-restore --filter 'FullyQualifiedName~Journal' -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal-scrollbar/ --verbosity quiet
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal-scrollbar/ --verbosity quiet
```

124 focused and all 612 tests passed; no failures or skips. Existing nullable/compiler
warnings remain. Logs: out/journal-scrollbar-focused.log and out/journal-scrollbar-full.log.
DLL SHA256: C804A5F819A5BDB01790BB814E0D466C2BDEDEB554ADF7A9F3B1670C03F7A9DB.
Visible alignment and scrollbar interactions require an in-game check.

Installed after confirming the debug game was closed; DLL/PDB hashes matched.
Backup: out/before-journal-scrollbar-fdd5cdce14c045acafdd3e3c85560eef/.
Restarted debug game as PID 86452; the fresh log confirms GraveyardKeepelago loaded
and the chainloader completed without a plugin startup exception. Native world
loading still reports missing FlowCanvas assets priest_test and golgothan_body.

## Follow-up: side-rail offset

The next screenshot exposed the missing native offset between the map clipping
rectangle and the window rail. The previous outer-edge test assumed these were
the same boundary; that assumption was incorrect. The footer now aligns to the
already positioned header ornaments' rendered outer edges, preserving their native
frame offset and avoiding another fixed pixel correction. Mirroring and vertical
centering are unchanged. Regression cases explicitly distinguish the inset content
boundary from the native corner extent and cover both sides and different pivots.

Validation:

```powershell
dotnet test GraveyardKeepelago.sln --no-restore --filter 'FullyQualifiedName~Journal' -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal-frame-offset/ --verbosity quiet
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal-frame-offset/ --verbosity quiet
```

124 focused and all 612 tests passed, with no failures or skips. Logs:
out/journal-frame-offset-focused.log and out/journal-frame-offset-full.log.

Rechecked the user's AP UI diagnostics warning against the live log and decompiled
source: InventoryPanelGUI.Open creates the native spacing widget separator without
setting its layer. UIWidget.CheckLayer logs the mismatch and assigns the panel layer.
NativeUiLayerDiagnostics deliberately adds Environment.StackTrace to that warning;
this is not a thrown exception and the path is outside the journal. No warning was
suppressed and no unrelated native inventory behavior changed.

The game was closed at the deployment guard. Installed the frame-offset build with
verified DLL/PDB hashes and restarted PID 65324. Backup:
out/before-journal-frame-offset-152aa9ef9423442b99f2f03c843f0de6/.
DLL SHA256: D8747253D691054EBC4FFA6CE04B120679AB7107F6AF8CE17D37FDE3A57168DF.

## Explicit facing and console-only warning filtering

The next screenshot showed outward-facing triangles again. The previous tests
only checked negation of a source scale and rectangle translation, which could not
establish actual artwork orientation. Footer ornaments now explicitly use the
existing native char_wndw_dec_V_1 sprite, positive transform scales, identity local
rotation and an explicit horizontal UV flip on the right only. In the exported
36x36 native canvas the upright is centered at column 4 (pixel center 4.5); the tip
reaches column 20. The upright is anchored directly to the native Technologies
vertical rail's center, independently of transparent margins and map clip width.
No generated graphic is used. Regression cases cover upright alignment and inward
tip direction on both sides at two display scales. The old scale-negation assertion
was removed because its premise did not establish rendered facing.

The existing ConsoleLogListener-only filter now recognizes the exact observed
Inventory separator layer diagnostic, including its appended stacktrace, and the
exact NGUI layer-repair warning. DiskLogListener is unchanged. Tests verify full log
event delivery, console suppression, and visibility of unknown widget paths, changed
layers, different sources and error severities.

Validation:

```powershell
dotnet test GraveyardKeepelago.sln --no-restore --filter 'FullyQualifiedName~Journal|FullyQualifiedName~ConsoleNoise' -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal-facing-console/ --verbosity quiet
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal-facing-console/ --verbosity quiet
```

187 focused and all 613 tests passed without failures or skips. Existing nullable
warnings remain. Logs: out/journal-facing-console-focused.log and
out/journal-facing-console-full.log. Visible rail attachment needs in-game checking.

Installed after the closed-game guard, with matching DLL/PDB hashes, and restarted
PID 54892. Backup: out/before-journal-facing-console-290c8c84abde472296a644d11372467e/.
DLL SHA256: 6C45469FB28CB32DD9A26819E6792A3C4E65EF51B09BCB9C4559090EE410B970.

## Runtime facing and Play-click timing (30 October)

The next screenshot still showed outward-facing triangles. The installed hash
matched the previous build; the standalone exported texture's facing did not
predict the actual in-game rendering. Reversed the explicit UV mirror selection:
left mirrored, right unmirrored. The upright alignment remains separate from that
choice. Updated the regression test's input to the observed left-facing runtime
sprite, rather than asserting a right-facing source premise. Added one log-only
diagnostic per side with sprite name, packing status, flip, scale, rail and position
to make the runtime state reviewable. This does not establish why the standalone
export and runtime appearance differ. No generated artwork or texture was added.

Investigated the hitch immediately after MainMenuGUI.OnPressedPlay. Native code
opens SaveSlotsMenuGUI, which draws one row per save. SaveSlotListShowPatch then
synchronously reads and fully deserializes each .dat file through GameSave.FromBinary
or FromJSON, solely to inspect unlocked_phrases for AP identity. The current log
shows four saves. This is a plausible main-thread stall, but no duration was recorded
in that build. Journal construction occurs after the game starts and is not on this
Play-to-save-list path. Added disk-log-only timing for I/O and deserialization per
save inspection, without save contents or connection credentials. No behavior,
selection guard or threading changes were made. Inspect [AP startup timing] after
the next Play click before choosing a cache/prewarm strategy; the actual-load identity
guard must remain authoritative even with a future menu cache.

Validation:

```powershell
dotnet test GraveyardKeepelago.sln --no-restore --filter 'FullyQualifiedName~Journal|FullyQualifiedName~SaveSlotEligibility|FullyQualifiedName~SaveStartup|FullyQualifiedName~ConsoleNoise' -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-runtime-facing-timing/ --verbosity quiet
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-runtime-facing-timing/ --verbosity quiet
```

215 focused and all 613 tests passed, without failures or skips. Existing compiler
warnings remain. Logs: out/runtime-facing-timing-focused.log and
out/runtime-facing-timing-full.log. Visual facing and real-world stall duration
still require a run of this build; no performance improvement is claimed yet.

After the user's shutdown confirmation, installed this tested build with matching
DLL/PDB hashes and restarted the debug game as PID 25240. Backup:
out/before-runtime-facing-timing-6c5e2b3fb1a8425f884a4f1e18c69391/.
DLL SHA256: 9067FCA43A99A96AC282A9E35D8C665D00D03558FD29264782AE101599D7568B.
