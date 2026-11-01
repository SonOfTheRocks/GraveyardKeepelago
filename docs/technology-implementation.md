# Technology and AP unlock implementation

Implemented 2026-10-26. Shared data version: 4; existing AP item/location names and numeric IDs are preserved.

## Runtime behavior

- `technologies.json` maps the 184 native technology IDs to 552 reserved research addresses,
  with explicit native parents and recipe/quest reward sources. New seeds generate 487 of
  those checks: one for each of 28 recipe books, three for ordinary research, and none for
  First slice/Grave plate/Iron, whose existing quest rewards already cover the action.
  The same resource is present in both shared-data checkouts. APWorld tests compare every mapping
  against the regions and generated location labels, including Spiritualism's existing special case.
- `TechnologyResearch` records research and reports those checks directly. Native `BuyTech` still
  handles its affordability/parent checks and deducts its normal cost before calling `UnlockTech`.
  Quest technology rewards retain their existing triggers and native completion/world effects,
  so First Slice opens descendant research while its AP-controlled flesh recipe still requires
  receipt. The 25 obsolete research-group items are absent from new pools; definitions and IDs
  remain reserved for a future option. Their dormant application path only reveals the root
  and cannot research it, grant recipes, or send research checks.
- Native technology reward lists are preserved. A preview-only `TechUnlock` list is returned to
  the research UI, with object-identity-based data and tooltip adapters. These objects are never
  inserted into game craft definitions or serialized as unlocked crafts.
- `ServerRewardPreviewSource` requests placements with `HintCreationPolicy.None`. Names, receiving
  players, and games come from the server. Local rewards use native icons where possible;
  cross-game or unavailable native icons use the AP sprite. The cache is accepted on the game
  thread. Pending/failed requests retain a loading state and can retry, without inventing rewards.
  Publishing AP hints is intentionally not exposed through this display path.
- `UnlockPolicy` derives controlled IDs from enabled direct grants, progressive stages, and
  composite perks. Comparison/persistence strips the technology-list `@` presentation prefix.
  Controlled recipes require receipt even if vanilla `needs_unlock` was false. The original flag
  is restored when leaving the save. Controlled vanilla pre-unlocks are removed from the active
  recipe/work lists; native unlock methods are called only for actual receipts or uncontrolled content.
- The old `Locations.TechsModifier`, generic AP technology-data patch and `CopyLists` interception
  have been removed. Historical unregistered experiments under `UnsortedPatches` are not activated.

## Persistence and lifecycle

`SaveProgress` stores its versioned, seed/team/slot-bound JSON as one namespaced Base64 record in
`GameSave.unlocked_phrases`. Inspection of the game sources confirmed this serialized string list
is only queried for membership of known phrase IDs; arbitrary entries are not resolved as game
definitions. Other phrases are preserved. Keeping the record inside the normal game save avoids
separate progress files getting ahead of the save when a player quits without saving.

Receipt fingerprints retain the server index, item, source location/player, and name. Reloads
validate the existing prefix and skip processed receipts; only new indices apply effects and
enqueue notifications. Progressive counters reconstruct from that prefix. Failed/unknown items
do not advance it. Controlled ownership and pending checks share the same saved record.

Checks are remembered before network transmission and filtered to the slot's enabled locations.
Reconnect resends saved checks. Identity validation runs before synchronization. F8 can retry the
existing in-game session. Save changes rebind the services; patch registration is reused rather
than stacked. Recipe flags are restored on leaving a save or plugin destruction.

Existing saves are validated in the native load callback before world restoration;
only matching AP records are admitted while connected. New saves receive their
binding before their first native save. Event recording is installed immediately
after the local player spawns, before the loader initializes quests and starts
their flow graphs. Research checks are no longer reconstructed from `unlocked_techs`.
See `save-startup-protection.md` for the guard, lifecycle and regression validation.

Legacy prototype saves have no receipt ledger. A fresh save is required for the first version-3
test: prior consumption of physical AP items cannot be reconstructed reliably. This is distinct
from normal reload/reconnect handling of saves created with this implementation.

## Technology scripts

The local vanilla export contains 19 technologies with scripts. Their exported graphs were
inspected for direct unlocks and other actions. The scripts include room decorations, bag and
recipe dialogue flags, zombie logistics checks, soul bounds, task/phrase updates and conversations.
The implementation preserves these graph/world actions while gating AP-controlled `UnlockCraft`
and `UnlockPerk` calls independently of the AP receipt scope. Pseudo-technology reveal dialogs
for controlled recipes are suppressed while invoking their continuation callback.

## Validation

Version 4 recipe/source change: the client suite passed 244 tests. The APWorld
suite passed 7,687 tests and 6,056 subtests; shared validation passed 11 tests.
Fresh all-DLC generation succeeded (AP_98443308963503938137.zip).
The removed group-item access tests are replaced with preservation/pool tests:
requiring an item intentionally absent from generation is no longer valid.
The human-healing quest regression replaces its obsolete Spiritualism item
assertion with the native soul_sins_2 research requirement.
The APWorld
regressions verify one check per book, retired slots absent from generation but
still reserved, native quest/parent access, balanced pools and unchanged IDs.
All-DLC generation has 773 locations, 716 declared item copies and 57 additional
research-point filler copies (80 filler items including the declared filler).

Evidence is recorded in `evidence/recipe-item-sources.json`: original item-use
expressions were exported with Unity serialization because the older public-field
dumps omit them and native `_parents`. All 28 mapped books call UnlockTech on a
single technology, including bag and gravestone books outside the cookery branch.
The Cyrillic native ID `Сomfort of faith` had a UTF-8 decoding error in the old
mapping; its key is corrected without altering its public locations or addresses.

`recipe_pot_speed` is an additional data entry whose script reveals an alchemy
formula. No obtainable source is established, including by the user; it does not
gain an invented location. The generic repeatable `rand_alchemy_unlock` instead
calls UnlockRandomAlchemy and is a different source, described by the wiki at
https://graveyardkeeper.fandom.com/wiki/Alchemy_recipe. Alchemy discovery rolls
are outside the existing technology-check pool and are not converted into an
unbounded stream of checks by this change.

The old human-soul-healing quest depended on the removed Unlock: Spiritualism
item. It now explicitly requires access to native soul_sins_2 research, whose
tech_soul_sins_2 script opens that conversation. Currency/research-point costs
remain enforced by native BuyTech; the existing AP reachability model does not
model the accumulation of those currencies.

Old three-location recipe seeds use data version 3 and are rejected by version 4
clients. Generate a new seed and use a matching new AP save; receipt/save identity
checks intentionally prevent reusing old progress with a different seed.

The commands and results below document the earlier version 3 baseline.

Run from the client checkout (the plugin output stays inside the repository):

```powershell
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=bin/checked-client/
```

Result on 2026-10-26: **150 passed, 0 failed, 0 skipped**. Production and test projects both use
KaitoKid utilities/interfaces 3.1.11. The checked plugin and its data are in
`GraveyardKeepelago/bin/checked-client/`. Existing analyzer/compiler and test-fixture nullability
warnings remain; they did not cause failures.

Run from the APWorld checkout:

```powershell
.\.venv\Scripts\python.exe ModuleUpdate.py --yes
.\.venv\Scripts\python.exe -m pytest worlds/graveyard_keeper/test -q -p no:cacheprovider
.\.venv\Scripts\python.exe -m unittest discover -s worlds/graveyard_keeper/data/shared/tests -t worlds/graveyard_keeper/data/shared
```

Results: setup succeeded; the APWorld suite passed **7,673 tests and 5,777 subtests**; the shared
data suite passed **11 tests**. `validate_data.py` confirmed version 3 with 486 item definitions
and 286 explicitly authored locations. The 552 research locations are checked separately against
the APWorld's generated locations. Existing Python warnings concern optional `_speedups` and
deprecated `pkg_resources`. Shared changed files were compared byte-for-byte between checkouts.

Headless tests cover ownership, canonical IDs, typed grants, all DLC switches, receipt/save replay,
conflicting identities/history, progressive reload/excess copies, offline check retention, mapping
parity, research state/world-effect idempotence, actual preview data, retries and the no-hints API policy.
They cannot confirm Unity rendering, Harmony/JIT behavior or interactive gamepad/mouse operation.
The running game still has the previous plugin loaded; the new build has not been deployed into
that live process. The current version-2 test server is incompatible with the new version-3 client.

The manual acceptance path is: fresh seed/save → receive Tough Mood from the morgue reward → flesh
stays locked → receive Extract: Flesh → extraction works → inspect actual technology rewards →
research once → verify costs/checks/child access → save/reload → verify no repeated grants →
disconnect, complete a check, F8 reconnect → verify synchronization. Repeat the UI checks at another
resolution/UI scale and with the relevant DLC combinations.

## Pre-existing data gaps

The earlier raw-dump comparison found 15 grant IDs absent from the two exported recipe lists,
including the single string `paper_bad, paper_from_herb`. This change does not guess replacements
or silently rename those grants. See the historical review for the comparison context. The main
regression case (both flesh recipes) is present in the game data and covered by ownership tests.
