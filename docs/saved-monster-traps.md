# Saved monster trap execution

The versioned item receiver accepts `Bats Trap` (`bats`) and `Green Slime Trap`
(`green_slimes`) into `SaveProgress.Data.PendingTraps` alongside the confirmed
receipt. `PendingTrapRunner` consumes this same ordered list; it has no second
in-memory queue. Existing item names, IDs, shared v9 data and receipt format are
unchanged. The unversioned receiver keeps its existing behavior. Versioned login
remains blocked until the remaining runtime support is complete.

`Plugin.Update` runs pending traps on Unity's game thread, after received items.
Execution requires the currently bound save and matching connected seed/team/slot.
Disconnection, an active asynchronous grant, player death or nonpositive health,
loading, pause, main menu, GUI initialization, open modal windows, dialogue,
cinematics and scripted loss of player control delay activation. The runner starts
with a five-second delay, waits two seconds after an unsafe context and spaces
activations at least ten seconds apart. Binding a loaded save restarts these
timers; reconnecting cannot replenish a consumed queue entry.

Each trap creates four monsters of its exact type (`bat_test` or `slime_test`).
Preparation resolves the native definition and four distinct unblocked positions
one tile (96 world units) around the player. If preparation fails, the entry stays
pending and retries after ten seconds; repeated identical errors are suppressed.
Unsupported IDs fail explicitly, without a random fallback or silent deletion.

Before invoking native spawning, consumption is stored in the same progress
record as the receipts. If storage fails, the queue entry is restored and spawning
does not start. After spawning starts, an exception or null result is uncertain:
the trap stays consumed, already created monsters remain and the error is logged.
There is no retry or refund, including a failure in the first native call. The
next trap may run after its cooldown. This avoids duplicating a partial group.

`SaveProgress.Store` updates the native save's phrase record in memory. It does
not force a disk save or make native world effects a disk transaction. Reloading
an older disk snapshot restores its queue and world together through the game's
normal save lifecycle.

Monsters use `WorldMap.SpawnWGO` as standalone native world objects. The client
adds no temporary lifetime, dismissal, combat protection, respawn mechanism or
serialization filter. Native combat/death, drops, world transitions and saving
govern their behavior, including after a trap activates and the player later
enters a dialogue. This deliberately differs from the temporary-monster subsystem
in reference PR #2.

Native evidence: `WorldMap.SpawnWGO` creates and registers each object;
`SerializableGameMap.SaveSceneToMe` includes non-player objects below `world_root`
and `RestoreScene` restores them; `WorldGameObject` handles native death and drops.
No custom `MobSpawner` or shared balance definitions are inserted.

## Validation boundary

Unit tests exercise the shared grant/factory -> receipt -> pending queue path,
history replay, reload, availability callback and context changes, timing,
storage failure, preparation retry, exact monster mappings and uncertain/partial
spawn failures. Position tests cover distance, blocked directions, uniqueness
and insufficient room. These tests do not execute native Unity objects.

Before enabling versioned logins, manually verify both monster types, collision
placement, menus/dialogue/cinematics/sleep, death, slot/save switching,
reload/reconnect with pending entries, and native persistence/death/drops of
surviving monsters. Verify a save taken after activation does not replay the
trap, while loading an earlier snapshot restores its earlier state. Native
spawn faults and any reentrant saving during a native call also require an
in-game check. This change has not been deployed into the game installation.

## Local verification, 2026-11-24

Commands ran from the client repository, with `DOTNET_CLI_HOME` set to its
existing `.dotnet-home` directory. Build copies were redirected into `out`.

```powershell
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/saved-monster-traps/ --verbosity quiet
dotnet build GraveyardKeepelago.sln -c Release --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/saved-monster-traps/release/ --verbosity quiet
dotnet test GraveyardKeepelago.sln -c Release --no-build --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/saved-monster-traps/release/ --verbosity quiet
```

- Debug suite: 185 passed, 2 failed, 0 skipped (187 total). Both failures are
  existing `GKPerk.Apply` Debug assertions about hidden work IDs, exercised by
  `ProcessItem_Perk_RoutesCorrectly` for Axeman and Master gatherer. They also
  fail in isolation. The affected code and fixtures match `origin/main` and were
  not changed. All 17 added trap tests pass in Debug.
- Release build: success, 0 errors, 79 existing compiler/analyzer warnings.
- Release suite: 187 passed, 0 failed, 0 skipped. Debug assertions are omitted
  by the existing Release configuration; this does not resolve the old failures.
- `git diff --cached --check`: clean. No deployment, push or PR creation.
