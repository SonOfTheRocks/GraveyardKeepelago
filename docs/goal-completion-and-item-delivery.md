# Goal completion and physical item delivery

## Completion contract

The client supports `goal: 0` (Portal) and `goal: 1` (Graveyard). Unknown supplied
goals fail explicitly. Older Portal slots may omit the goal or the Graveyard rating.
Graveyard slots must supply `graveyard_goal_rating` as an integer in `0..2484`;
there is no guessed default for older seeds missing this value.

Portal completion retains the existing criterion: the save contains both
`craft_barrel` and `craft_emitter` in `completed_one_time_crafts`. Graveyard uses
`WorldZone.GetZoneByID("graveyard").GetTotalQuality()`, the same native API used
by `SaveSlotData` and `EnvironmentEngine`. Rating zero follows the APWorld's
immediate-completion rule, even if the native graveyard currently has negative quality.

`GoalCompletionTracker` evaluates persisted progress and acknowledges one successful
report per connection. A new save runtime starts a new tracker. `GoalManager` checks
once per second on the game thread, guards save identity/world readiness, tolerates a
missing graveyard zone, and retries failed reports with bounded logging. Explicit
reconnect also resets reporting if the disconnected frame was not observed. Its craft
hook is installed idempotently regardless of which goal was loaded first. Save cleanup
clears the active manager so an old save cannot report another save's completion.

## Inventory capacity

`WorldGameObject.AddToInventory(Item)` returns false when native inventory insertion
fails. `PhysicalItemDelivery` only succeeds after insertion or a successful native
ground drop. Otherwise the exception leaves the item receipt unconfirmed for retry.
The previous `PlayerUtilities.IsDroppingItems` scope is restored in `finally`.

Native `DropResGameObject.Drop` can return null after a successful split multi-item
drop, so the fallback uses `force_stacked_drop: true` to obtain its spawned object.
The native implementation registers that object in `DropsList`; `DropsList.ToGameSave`
saves its resource, position and zone. This uses the same stacked-drop path as native
drop restoration. Items already routed to drops through `player_cant_throw_out` retain
that path. AP provenance remains on the dropped resource to avoid treating its later
pickup as a vanilla reward.

## Validation

`GoalSlotDataTests`, `GoalCompletionTests`, and `PhysicalItemDeliveryTests` cover invalid
slot data, integer bounds, fractional ratings, zero thresholds, absent worlds/zones,
offline completion, reconnect, reload, goal switches, report retries, inventory failure,
drop failure, receipt retry and duplicate prevention.

Native world adapters and ground-item pickup still need in-game verification. Automated
tests exercise the decisions without constructing Unity objects.
