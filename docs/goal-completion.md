# Goal completion

Slot data must declare integer `goal` 0 (Portal) or 1 (Graveyard). Graveyard
also requires integer `graveyard_goal_rating` in 0–2484. The effective target
sent by the APWorld is used unchanged. Missing or invalid required fields reject
connection and reconnect; there is no implicit Portal fallback. Portal does not
use the rating field. Versioned logins remain blocked independently of this work.

Portal requires both `craft_barrel` and `craft_emitter` in the save's completed
one-time crafts. Native `GameSave.OnFinishedCraft` records completion, whereas
`CraftComponent.CraftAsPlayer` can merely start work. Both craft definitions run
`check_portal`, which activates `portal_activate_cutscene` after the second craft.
The goal is restoration; entering the portal is not required.

Graveyard reads `WorldZone.GetZoneByID("graveyard", false).GetTotalQuality()`,
the same source used by native `SaveSlotData.PrepareForSave`. A missing zone or
nonfinite rating cannot complete the goal. Target zero requires actual rating
at least zero, including cleaning/improving the initially negative graveyard.

Plugin.Update checks once per second on the Unity thread while the world is loaded,
outside loading screens, main menu and pause. Scripted player-input suppression
does not block reporting during the portal ending cutscene.
Goals use the existing save-check lifecycle and SaveProgress seed/team/slot record,
also in the unversioned path (without enabling versioned item/location processing).
Only a newly created save can establish a new record; arbitrary existing saves
cannot be adopted. A different seed, team or slot cannot reuse the binding.
Slot selection, leaving the game and explicit new connections clear it. Loaded
matching saves and successful reconnects reevaluate native state, so completion
can be reported even when it happened offline. No goal-specific Harmony hook or
static current manager remains.

Status is sent asynchronously through the captured validated session socket,
without the legacy helper's implicit reconnect. A completed socket send suppresses
further sends for that binding/connection. One send may be in flight; failed or
cancelled sends retry after five seconds. A reconnect starts a fresh reporter.
Archipelago's status packet has no separate acknowledgement; reevaluation after
reconnect covers an uncertain previous delivery. Old in-flight tasks cannot mark
a new reporter complete.

Evidence: local native Assembly-CSharp.dll and FlowGraphExports/check_portal.json;
shared data remains v9 at 1eabb840. No names, IDs or manifest version changed.
The prior PR #2 implementation was checked selectively; its zero-target shortcut
and reconnecting legacy report helper were not retained.

Before enabling versioned login, verify in game: craft each portal component
separately and finish the second; raise negative graveyard quality through zero;
load an already completed bound save; interrupt/reconnect the network; switch saves
and slots. Unit tests cover thresholds, field validation, completion polling,
failed transmissions, pending sends and readiness loss. They do not run Unity or
a live Archipelago server. The remaining v9 runtime contract still needs auditing.
