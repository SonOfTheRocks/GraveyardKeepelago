# AP save startup protection

AP saves require a connection when loading, including after a fresh game start.
Without a connection, only non-AP saves are selectable. An AP record (including
a damaged record or a future format in the AP namespace) blocks selection and
loading, with a message asking the player to connect to the matching server and
slot. This prevents an AP save from starting without its AP runtime. The shared
guard rechecks the current connection when loading, in case it changed after
the save list was opened. This rule does not force an exit on a later temporary
connection loss.

While connected, a loaded save must contain exactly one valid AP progress record
whose seed, team and slot match the current session. Saves without a binding,
wrong identities, duplicate records, unsupported versions and corrupt metadata
are rejected without creating/replacing a record, restoring the world, applying
items or sending location checks. The error explains the mismatch or asks the
player to disconnect for a non-AP save. The connection remains available so a
matching save can be selected next.

The save selection list now performs this read-only validation before selection.
`SaveSlotEligibility` shares the same `SaveStartupGuard`. `SaveSlotGUI.Show`
deserializes each existing slot's file without invoking `PlatformSpecific.LoadGame`,
assigning the selected slot/save or restoring the world. Incompatible, unbound,
corrupt and unreadable saves are dimmed and `OnSlotSelect` is blocked. Their
colliders remain active for native tooltip hover; gamepad focus shows the same
reason and a disabled Select hint. Delete retains its separate native confirmation
path. Only New Game skips metadata/file inspection; disconnected lists inspect
existing saves too, to distinguish non-AP saves from AP saves.
Reopening the list reevaluates it against the current server. The original load
guard remains authoritative if data changes after the list was drawn.

`SaveProgress.Load` is strict. `SaveProgress.Create` is a separate operation that
rejects existing bindings. Only the native new-game path uses `Create`; its guard
stores the binding before invoking the game's first save callback. Existing
valid version-1 AP records remain compatible. There is no automatic migration of
unbound prototype or vanilla saves and no new shared-data version or seed format.

## Native integration

- `SaveSelectionStartupPatch` clears save-specific runtime state immediately on
  selection: item/notification/trap queues, active check service and technology
  runtime. Patch registrations remain installed and are not stacked on reload.
- `ExistingSaveStartupPatch` wraps `PlatformSpecific.LoadGame`'s
  `OnGameLoadedDelegate on_lodaded`. Native loading deserializes the file and
  invokes this callback before `SaveSlotsMenuGUI.OnLoadSlot` assigns
  `MainGame.me.save`, restores the map or starts gameplay. Rejection restores
  the previous selected slot, dismisses loading UI and returns to the menu with
  an error. Disconnected loading retains the native behavior.
- `NewSaveStartupPatch` captures the connected session at
  `GameSave.CreateNewSave`, wraps `Action on_complete`, stores the new binding,
  then allows `SaveSlotsMenuGUI.OnNewGame` to call `PlatformSpecific.SaveGame`.
  Starting a game without a connection creates no AP binding.
- `PlayerSpawnStartupPatch` initializes AP recording after the local player's
  inventory is restored, before `StartPlayingGame` calls `quests.InitQuestSystem`
  and starts flow behaviours. The existing completed-loading hook remains a
  fallback. Initialization is idempotent for the same save object.
- `InitializeAfterConnection` repeats strict validation before any AP runtime
  changes or sends. This also rejects late pause-menu connections to an unbound
  or mismatched save. Reconnects validate identity before resending saved checks.

Native method order and signatures were checked in the local decompiled
`PlatformSpecific`, `SaveSlotsMenuGUI`, `GameSave` and `PlayerComponent` sources.
The native load/first-save continuations run only after `SaveStartupGuard` succeeds.

## Explicit progress, no technology inference

The `RecoverResearchChecks` method and startup call were removed. A list of
previously researched technologies does not constitute recorded AP check history.
Research/quest/craft events record their checks as they occur, before transmission;
the saved `Checks` collection is resent after reconnect. Item receipts still use
their independent ordered ledger and ownership sets. Loading or constructing a
research service does not emit checks merely because technologies already exist.

Already transmitted checks remain in the server's progress. This guard does not
roll back or delete that progress, alter save files or reset the current server.
A contaminated server needs a separate backup restoration or fresh playthrough.

## Validation, 2026-10-28

From the client root, using the local `.dotnet-home`:

```powershell
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-save-startup/ --verbosity quiet
```

Result: **419 passed**, no failures or skips. Existing compiler/analyzer warnings
remain. The tested build is staged inside the client repository.

The same tested files were installed into the debug game and verified against
their staged hashes. The game was restarted with the existing server settings;
startup confirms the plugin and early Harmony patches load without an error.
The old in-memory save was closed without invoking the game's save action.
The server and its existing check history were not reset.

New regression cases cover creation before first save, correct identities,
wrong seeds/teams/slots, existing vanilla saves, malformed/duplicate/unsupported
records, null records, missing server identity, prevention of rebinding,
reselecting different saves, reconnect identity checks and explicit saved checks/
pending traps. Existing receipt replay, ownership and offline-check tests pass.
Their setup now calls `Create` for a new record; `Load` no longer implicitly
creates one. No previous rejection assertion was removed or weakened.

Manual Unity checks remain: with an active connection select a vanilla save and
a save from another seed, verify the error/menu and zero new checks; then load
a matching save. Start a new AP game, verify its first on-disk save already has
the binding, reload it, and check that actual tutorial/quest events are recorded.
Repeat a rejected selection followed by a valid one, a disconnected vanilla load
and a late pause-menu connection. Automated tests do not render these dialogs or
execute the native Unity scene lifecycle.

## Save-list validation, 2026-10-28

```powershell
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-badges-save-list/ --verbosity quiet
```

Result: **450 passed**, no failures or skips, including badge raster tests and
save-list eligibility tests. The latter cover matching/wrong seeds, teams and
slots; non-AP/corrupt/unreadable saves; no reads while disconnected or on the
new-game row; unchanged save records; and reevaluation for another server.
Manual UI validation still needs hover/tooltips, dimmed styling, blocked mouse/
gamepad selection, valid selection, new-game selection and disconnected lists.

## Connection-required AP loading, 2026-10-28

```powershell
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-online-save-start/ --verbosity quiet
```

Result: **455 passed**, no failures or skips. Existing compiler/analyzer
warnings remain. The former tests allowing disconnected AP selection were
updated because the user explicitly cancelled offline AP startup. Added cases
cover vanilla/AP saves offline, unreadable offline files, damaged and future AP
markers, unchanged records, and connection changes between listing and loading.
Both list validation and the actual native load callback use the same guard.
No APWorld contract or save format changed. The tested build is staged in the
output folder above; it has not been installed into the running game. Manual
Unity verification of the disconnected list and hover tooltip remains pending.
