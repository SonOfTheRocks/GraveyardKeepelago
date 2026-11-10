# Technology-group items: obsolete availability model

Status after the data version 4 implementation: the findings below describe the
old behavior. New pools omit all 25 groups; native parent/quest access replaces
their AP item gates. Each technology recipe book has one check, and the three
explicitly mapped quest technologies retain only their quest check. See
technology-implementation.md for the implemented contract and validation.

## Vanilla evidence

There is no TechDefinition with ID `Smithing` in the exported tech data. Smithing
is the AP category label for the smithing branch; its root node is `Iron`.
Iron is hidden initially. The `npc_blacksmith` graph explicitly grants it via
Flow_UnlockTech (export node 46). Its original visible rewards are the primitive
furnace (`mf_wood_builddesk:p:mf_furnace_0_place`), iron ingot (`ingot_metal`) and
iron mining work (`t_iron_ore_1`). The ordinary native dialog draws those three
rewards rather than a single bespoke Smithing icon. Iron's own icon field is empty.
Native UnlockTech also marks the node purchased and reveals hidden nodes.

## Active leftovers in APWorld

The shared items/unlocks.json still defines 25 progression items with `unlocks`
and `researches`. Unlock: Smithing lists 21 smithing technology regions in
`unlocks` and Iron in `researches`. rules.py still loops over every `unlocks`
entry and gates its technology region with Has(the group item). The declared
item pool includes these items. test_access_dependencies.py explicitly tests
that the research locations cannot be reached without their group unlock.

Zombie Workstations has a list of gated technology regions but no automatic
research roots. Its item_grants.json technology_ids list is therefore empty,
matching that old model; it is not simply an accidentally missing icon mapping.

## Active leftovers in the client

GKItemFactory instantiates GKTechnologyUnlock for these grants. It calls
PlayerActions.UnlockTechnologies and GameSave.UnlockTech for each research root.
TechnologyRuntime routes those through its research handling: nodes become
researched and their research checks are reported. The client does not implement
the APWorld's additional group-wide availability gate. Consequently the two
sides express different parts of the abandoned design.

The recent icon changes only adapted presentation to the existing contract. They
did not establish that this contract was still wanted. Treating the group items
as current design without checking their provenance was a mistaken assumption.

## Required coordinated cleanup (not performed by this audit)

Remove obsolete technology-availability group items from newly generated pools
and replace the APWorld's group-item region gates with the intended native
research/quest prerequisites. Keep researching technologies as checks, with AP
reward previews, and keep actual recipe/work/perk receipts as grants. Native
quest-driven technology visibility must no longer depend on a randomized group
item. Remove the corresponding active group-item application path in the client.

This is a contract/logic change: preserve numeric identities of all surviving
items/locations, do not shift enumeration IDs by simply deleting entries, update
both shared-data checkouts/versioning and tests, and test fresh generation.
Existing generated seeds containing the obsolete group items need an explicit
compatibility decision. The audit does not silently reinterpret their items.

## Quest and recipe-book overlap audit (before changing generation)

The existing three-check research mapping is not a count of vanilla rewards.
`data.py:load_locations` creates three locations for every technology region;
`TechDefinition.MAX_VISIBLE_UNLOCKS = 3` is only the native dialog's presentation
limit. Hidden (`@`) craft entries must not be counted as additional visible rewards.

Three technologies currently have both an explicitly authored quest reward and
three generated research locations:

| Native technology | Explicit quest reward location | Additional research locations |
| --- | --- | --- |
| First slice | LOCATION_TUTORIAL_REWARD_ENTER_MORGUE_WITH_CORPSE | First Slice - Unlock 1/2/3 |
| Grave plate | LOCATION_TUTORIAL_REWARD_TALK_TO_BISHOP_1 | Humble Marker - Unlock 1/2/3 |
| Iron | LOCATION_KREZVOLD_REWARD_ASK_ABOUT_METAL_ORE | Iron - Unlock 1/2/3 |

The other `technology_reward` triggers, Persistence/Industriousness/Eloquence,
all map to the same Game of Crone story-completion location and are not three
separate checks. Those technologies have no entries in technologies.json.

The client currently handles an explicitly mapped forced-technology dialog by
reporting the quest reward and immediately invoking its continuation. It skips
the native dialog and the subsequent GameSave.UnlockTech entirely. The same
quest mapping takes precedence in the UnlockTech prefix. Consequently this path
does not mark the technology researched or execute its progression effects.
The old group item can later reach TechnologyRuntime.Research via its item
application scope, bypassing quest replacement and reporting the three research
locations. Removing that item without addressing the overlap strands those
locations and can leave technology descendants unavailable in the game.

For comparison, the book demonstrated by the user, Simple and Tasty, maps to
native technology ID `2min`. It has three generated research checks and no
explicit `technology_reward` trigger. The native SmartExpression `UnlockTech`
function opens the forced technology dialog, whose confirmation calls
GameSave.UnlockTech; an unmapped technology therefore goes through the normal
research handler and reports its three checks. The screenshot confirms the
three-location behavior. The exported item expressions omit the underlying
expression text, so those dumps alone cannot enumerate every recipe-book source.

Required distinction for the coordinated change:

- Revealing a node/area changes visibility and creates no reward check.
- Completing a technology must preserve native researched state and quest/world
  effects, including descendants becoming researchable.
- Actual AP-controlled recipes, works and perks remain receipt-only.
- An explicit quest reward and its automatically completed technology must not
  acquire a second set of checks solely because they describe the same action.
- Normal purchased research and book-triggered research need a documented reward
  count; retaining three checks for books versus changing to one is a separate
  generation decision, not an incidental consequence of the cleanup.
- Reload recovery and AP item application must use the same source mapping;
  neither may recreate retired checks or send checks merely for receiving items.

Native prerequisite data needs additional evidence: the current public-field
balance exports omit TechDefinition._parents and serialize GameRes prices as
empty objects. Missing exported parents/prices are not evidence that research is
free or has no prerequisites. CanBuyTech explicitly checks both at runtime.

This section records inspection only; generation and runtime behavior have not
yet been changed by this audit.

## Gerry relation exception

The captured game log reports a NullReferenceException in PlayerActions.ApplyRelation
after receiving Gerry - Happiness +10. The old code increased the saved relation,
then required a loaded crafting_skull_3 world object to show a cosmetic bubble.
That object was absent. The exception prevented the receipt ledger from recording
the already applied reward, potentially causing a repeated increase on retry.

RelationReward now ensures the known-NPC record, performs the relation update,
then treats the world bubble as optional. World lookup uses ignore_not_found_error;
missing NPCs and presentation exceptions cannot fail a successful grant. Required
save-update failures still propagate. The change does not guess whether an old
partially applied reward was later saved and does not silently edit save values.

Validation: `dotnet test GraveyardKeepelago.sln --no-restore
-p:PluginsPath=bin/checked-client/` — 240 passed, zero failures/skips. Regressions
cover absent NPCs, throwing cosmetic bubbles, later items continuing, history
replay without repeated happiness, and required save-update failures. Existing
compiler/analyzer warnings remain. No APWorld behavior changed in this audit.
