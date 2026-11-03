# Monster traps

Shared data version 9 contains exactly two trap items, both classified `TRAP`:

| AP item | ID | Grant `trap_id` | Fixed effect | Main sprite |
| --- | --- | --- | --- | --- |
| Bats Trap | 488 | `bats` | Four bats (`bat_test`) | `300_bat_face_fly_1` |
| Green Slime Trap | 489 | `green_slimes` | Four green slimes (`slime_test`) | `320_slime_jump_1` |

The general random trap and its compatibility paths were removed before the
first release. ID 487 is unused, without a reservation; other item and location
IDs are unchanged. Both consumers must use version 9. Regenerate older local seeds before
playing with this client; changing percentage or weights also needs a new seed.

## Receiving and activating

The seed option `trap_percentage` replaces that percentage of filler copies,
rounded down. It defaults to 0. Useful and progression copies are preserved.
Recipes classified as filler can be replaced. This option does not change the
total pool size and is serialized in slot data for diagnostics.

```yaml
Graveyard Keeper:
  trap_percentage: 25
  bats_trap_weight: 50
  green_slime_trap_weight: 50
```

Both weights range from 0 to 100 and default to 50. They are relative: 1/3 and
25/75 produce the same probabilities. Zero excludes a type. Both zero is rejected
when traps are enabled, with an error explaining which options to change;
both zero is allowed when traps are disabled. Weights are serialized in slot
data for diagnostics; the client executes the received type without rolling again.

`GKItemFactory` builds the shared trap as `GKTrap`. Applying it calls
`PlayerActions.QueueTrap`, which adds its exact trap ID to `SaveProgress.PendingTraps`.
`ItemReceiptLedger` records the receipt and pending queue together. Later normal
items can apply immediately, even if a trap must wait. Reconnecting and loading
history do not queue an already recorded receipt again. Old save records without
the queue property deserialize with an empty queue; seed/slot identity checks remain.

`Plugin.Update` drains the queue on Unity's main thread. Activation requires the
same gameplay availability as receipt notifications, plus a living player with
positive health. Loading, menus, dialogue, cutscenes, sleep and similar restricted
contexts delay activation. There is a five-second initialization delay, a
two-second grace period after unsafe gameplay, and at least ten seconds between
activations. These timers restart when loading; pending queue entries persist.

`TrapExecutor` resolves the queued ID through `MonsterTrapDefinition`. Both
items always spawn their own specific monster. Unsupported queue IDs are rejected;
there is no random fallback. `MonsterSpawner` resolves four distinct clear positions
one tile (96 world units) around the player before spawning anything. The native
`WorldMap.SpawnWGO` creates standalone monsters. Each successful spawn is registered
as a temporary trap enemy for this play session; surviving trap monsters are excluded
from the world's serialized object list, so they do not return after loading.

### Active monster protection and lifetime

Already spawned trap enemies pause during dialogue (including speech bubbles and
answer selection), cinematic scenes, modal windows, loading/pauses and scripted loss
of player control. Normal attack or crafting animations alone do not trigger this
protection. The enemies' native component updates, behaviour trees and physics are
paused. A combat guard checks the live context at the point of impact, including
transitions occurring before the client's normal Update in the same frame. Hits to
and from those enemies are blocked while protected; ordinary enemies retain their
normal combat behavior. After control returns, the trap enemies remain paused for
two seconds before resuming. Only AI states paused by this feature are resumed.

Player death, teleport, leaving the current WorldZone, save/player/world replacement
and runtime shutdown dismiss all current trap enemies without death rewards or
inventory drops. Dismissal neither repeats nor refunds an activated trap. Pending
trap IDs remain in the existing save record. Saving itself does not remove active
enemies from gameplay: a postfix filters only the session's tracked IDs out of the
native world snapshot, including inactive/paused enemies. Dismissed objects are
detached immediately because Unity destruction is deferred until the end of the frame.

The distinction is session-only. Monsters written into saves by older client builds
have no reliable ownership marker and are not deleted indiscriminately; this feature
applies to trap monsters spawned with the new tracking enabled. No save schema,
AP item ID, slot data or APWorld generation behavior changed.

Queue consumption is stored before the world effect to prevent replay of a
partially completed spawn. If storage fails, the queue entry is restored and no
spawn is attempted. If spawning fails, an error is logged and the trap remains
consumed, including when no suitable nearby positions exist. Other receipts and
traps can continue. Saving uses the game's normal save lifecycle; this is not a
separate disk transaction.

Receipt presentation uses the AP item name and the monster frame from the native
chars atlas. The skull is only the `quality_icon` in the existing star corner.
Technology previews independently select the main icon and category badge:
local traps show the monster, foreign-game rewards keep the AP icon and still
show the skull when their server flags classify them as traps. A foreign item
with the same name cannot accidentally acquire our monster icon.
Queue/activation messages are Info;
spawn details are Debug and therefore remain out of the terminal.

## Validation

Client tests cover the actual factory/receipt/queue path, unsafe contexts,
subsequent normal items, save/reload, history replay, cooldown, storage failure,
partial spawn failure, old save records, unknown IDs and receipt metadata.
Spawn-position tests cover distance from the player, blocked directions,
distinct fallback positions and insufficient space for a complete group.
APWorld tests cover percentages 0, 1, 25, 50 and 100 across all eight DLC combinations,
stable IDs, deterministic pools, per-player settings and restrictive fill for both goals.
Weight tests cover zero-weight exclusion across all DLC combinations, relative
scaling, changed distributions, invalid all-zero settings and serialized values.
Client tests cover mixed trap types across reload and main-icon/category separation.

The native spawn integration still requires a manual gameplay check. Use a new
version 9 seed with traps enabled, receive each trap during a menu/dialogue, return
to gameplay, check the spawn and cooldown, then save/reload with pending traps
and with surviving monsters. Automated tests do not execute the Unity scene.

### Local validation, 2026-10-27

The following is historical validation of development data version 6, not the
current version 9 contract (488 item definitions and no generic monster trap).

From the APWorld root, dependency setup completed successfully first:

```powershell
.venv\Scripts\python.exe ModuleUpdate.py --yes
.venv\Scripts\python.exe -m pytest worlds/graveyard_keeper/test -q --tb=short -p no:cacheprovider
.venv\Scripts\python.exe -m pytest worlds/graveyard_keeper/test/test_traps.py::test_weights_are_relative_and_do_not_need_to_sum_to_one_hundred -q --tb=short -p no:cacheprovider
```

Result: full suite **7825 passed, 5998 subtests passed**, plus **1 passed** for the
relative-weight scaling test added after full-suite collection. No failures. Existing warnings:
optional `_speedups` extension unavailable and deprecated `pkg_resources`.
An old slot-data test initially expected only the previous option names; its
exact expectation now includes the percentage and both weights. Its assertion excluding quest
trigger metadata remains unchanged.

From the client root, with `DOTNET_CLI_HOME` pointing to the local `.dotnet-home`:

```powershell
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-trap-types/ --verbosity quiet
```

Result: **380 passed**, no failures or skipped tests. Existing compiler/analyzer
warnings remain. The deployment override stages the build inside this repository;
the running game installation was not updated.

From each shared-data checkout, using the APWorld virtual environment:

```powershell
python validate_data.py
python -m unittest discover -s tests -t .
```

Result: valid version 6 data, 489 item definitions, 286 authored locations;
**15 tests passed** in each consumer. All 17 runtime JSON files agree across
the two checkouts. All 486 historical item IDs and 838 location IDs match the
captured pre-trap mappings; complete mapping hashes are covered by regression tests.

Real seed generation from the APWorld root:

```powershell
.venv\Scripts\python.exe Generate.py --player_files_path output/gk_trap_types_validation_20261027/players --outputpath output/gk_trap_types_validation_20261027 --seed 2026102707 --spoiler 1
```

Result: `AP_84662741306562188031.zip`, 773 checks, 60 traps at 25%, weights 80/20,
all three DLCs enabled, portal goal. Decoded multidata confirms 46 Bats Traps
(ID 488), 14 Green Slime Traps (ID 489), trap flag 4, data version 6, all three
option values and no old random trap. This test server was not started.
