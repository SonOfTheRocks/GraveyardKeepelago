# Temporary trap monster safety

The reported failure was a slime trap activating during normal gameplay, followed
by the player entering a conversation. The existing TrapManager ready gate prevents
new spawns during conversations but cannot protect against already active enemies.
CombatComponent.WasHitBy applies damage without testing dialogue/cinematic state.
SerializableGameMap.SaveSceneToMe includes inactive world objects, so merely pausing
or disabling enemies also does not prevent them returning after a reload.

## Implementation

TemporaryTrapMonsters is a pure session-owned controller. Its native adapter registers
each successful WorldMap.SpawnWGO result using the native unique ID (assigned by
WorldMap.OnAddNewWGO before SpawnWGO returns). Every partially completed trap group
therefore retains ownership of its successful spawns. No persistent marker is added.
The old queue/receipt consumption ordering is unchanged.

TrapMonsterRuntime captures player/save/world/WorldZone identities and live dialogue,
modal/cinematic/loading and player-control state. Active enemies suspend while that
context is protected; the controller gives two seconds of unscaled-time grace from
the actual return of control. Another protected interval restarts the grace. No
animation-category check is used, so ordinary attack/craft animations alone are not
treated as protected conversations.

The native adapter preserves and restores dont_update and Rigidbody2D.simulated,
clears physical momentum, pauses running BehaviourTreeOwners and resumes only the
owners it paused itself. Owners/bodies initialized after the spawn frame are also
handled. Prefixes on the three native world-object update phases and on WasHitBy
reevaluate the context for tracked enemies; the combat guard blocks hits both to
and from them while suspended. Unknown/ordinary world objects and enemies are
unaffected. Native adapter failures block tracked enemy updates/hits and log an
error rather than escaping into native dialogue/combat or stopping AP receipts.

OnPlayerDied, player TeleportToGDPoint and WorldZone.OnPlayerExit dismiss active
groups immediately. The regular update also detects replacement/removal of the
player, save, world or area. ClearSaveRuntime, reset and plugin disposal release the
tracker. Dismissal uses native DestroyMe without DoZeroHPActivity or combat rewards,
clears inventory before native removal and immediately detaches/deactivates the
object to avoid same-frame snapshots seeing Unity's deferred destruction. The
managed is_dead flag makes the native spawn helper's delayed ground-redraw callback
exit safely if dismissal occurs within its initial 0.5 seconds; no death flow is run.
Removal failures are isolated per monster, logged, and keep the failed ID owned
until cleanup; other group members are still processed.

SaveSceneToMe's postfix filters exactly the current tracker IDs from the private
_wgos list. Paused enemies are excluded too; saving itself does not end gameplay
with those enemies. The normal serializer then writes a world without active trap
monsters. Pending trap IDs still live in SaveProgress, while already activated
traps remain consumed. No schema, APWorld contract, item ID/name or slot-data change
was needed. Old unmarked monsters already serialized by earlier builds cannot be
identified reliably and are deliberately not deleted as if they were all AP mobs.

## Validation

```powershell
dotnet test GraveyardKeepelago.sln --no-restore --filter 'FullyQualifiedName~TemporaryTrapMonsters|FullyQualifiedName~TrapQueue' -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-temporary-trap-monsters/ --verbosity quiet
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-temporary-trap-monsters/ --verbosity quiet
```

32 focused and all 656 tests passed, no failures or skips. Existing compiler/analyzer
and nullable warnings remain; no new test warning remains. Logs:
out/temporary-trap-focused.log and out/temporary-trap-full.log.

Coverage includes already active enemies in every protected context, grace timing,
repeated protection, bidirectional hit blocking and unaffected ordinary enemies,
death/area/player/save/world/unload cleanup, explicit idempotent dismissal, duplicate
registration, destroyed enemies, removal failure isolation, immediate unsafe/dead
spawns, snapshot filtering and activated-vs-pending trap persistence across reload.
Native Unity/NodeCanvas behavior still needs in-game verification.

Manual checks with newly received traps:

1. Spawn slimes/bats, enter a conversation while they are nearby, and remain there.
   Verify no movement/damage and normal dialogue completion, then two seconds grace.
2. Repeat with a cinematic, answer selection, menu and ordinary player attacks.
   Protected mobs cannot be farmed during grace; normal combat resumes afterward.
3. Leave the area, teleport, die, or return to menu: active trap mobs disappear
   without drops or trap replay. Ordinary enemies remain unaffected.
4. Save with surviving trap mobs, reload and confirm they are absent. Separately
   save with queued traps and verify those still activate once during safe gameplay.
5. Check the log for errors, including immediate teleport after a spawn and repeated
   dialog/grace transitions. Diagnostic state transitions are Debug only.

Installed after the closed-game guard with matching DLL/PDB hashes and restarted
the debug game as PID 85384. Backup:
out/before-temporary-trap-monsters-055cd310d9de48819dd6c286cda27ed7/.
DLL SHA256: 5EE6AB532778DF133B9752330E728ECA77A5F601BF7A470D3EC313E4083E55EA.
