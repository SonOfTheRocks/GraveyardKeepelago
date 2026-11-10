# Offline AP sessions: implementation comparison and proposed architecture

Reviewed 2026-10-28. This is a source review and implementation proposal, not an
implemented offline feature or a runtime test result.

**Decision after review:** the user cancelled support for starting AP saves
offline. The proposal below is retained as research only, not planned work.
AP saves instead require a connection at load time; disconnected save lists
offer only non-AP saves. No runtime snapshot, migration or new save format is
being introduced. Existing check and item persistence remains unchanged.

## Reference implementations

StardewArchipelago was inspected at commit
`3c29e0a0afa7026fb5ee024fbab23947666a1bbc`:

- [ArchipelagoStateDto](https://github.com/agilbert1412/StardewArchipelago/blob/3c29e0a0afa7026fb5ee024fbab23947666a1bbc/StardewArchipelago/Serialization/ArchipelagoStateDto.cs)
  stores `LocationsChecked` separately from `ItemsReceived`, as well as cached
  scouted locations and other game state.
- [ModEntry](https://github.com/agilbert1412/StardewArchipelago/blob/3c29e0a0afa7026fb5ee024fbab23947666a1bbc/StardewArchipelago/ModEntry.cs),
  `OnSaving` (lines 221-241), copies the checker's complete local check list and
  processed items into the DTO and writes it through SMAPI save data.
  `OnSaveLoaded` (261-282) reads saved data, attempts a connection and initializes
  AP services only on success. Connection failure offers retry/error handling;
  closing that dialog exits to title (349-440). Thus this revision is **not**
  evidence that starting a complete session offline is supported.
- Its daily processing (508-511) imports server-known checks, resends local
  checks and processes received items. These are distinct operations.

DLCQuestipelago was inspected at commit
`04cf025f3070e80990b398b1b514c7caaf6d4d30`:

- [Plugin](https://github.com/agilbert1412/DLCQuestipelago/blob/04cf025f3070e80990b398b1b514c7caaf6d4d30/DLCQuestipelago/Plugin.cs)
  requires a successful startup connection (92-123). Its `EnterGame` creates a
  checker with an empty local list and imports server checks (181-192).
  `SaveGame` synchronizes checks and saves processed items to the campaign
  (211-229). This startup model does not satisfy our offline-session requirement.
- An `ArchipelagoStateDto` containing `LocationsChecked` exists in the tree,
  but its existence alone is not proof that this list is actually persisted by
  the current startup/save path. It is not used as evidence for that claim here.

The shared utility source was inspected both at current head
`8c168d605e97deda948bc44c7cdf668914b8bef9` and at
`b608caded7996beb8d825e36e0bcad10ae7a3e64`, the repository commit recorded in
the locally installed KaitoKid.ArchipelagoUtilities.Net **3.1.11** package:

- [LocationChecker at our installed package commit](https://github.com/agilbert1412/ArchipelagoUtilities/blob/b608caded7996beb8d825e36e0bcad10ae7a3e64/KaitoKid.ArchipelagoUtilities.Net/KaitoKid.ArchipelagoUtilities.Net/LocationChecker.cs)
  separates remembering checks from sending them. `SendAllLocationChecks`
  returns while disconnected. However, `GetAllLocations` and
  `GetAllLocationsNotChecked` also return empty while disconnected.
- [ArchipelagoClient at the same commit](https://github.com/agilbert1412/ArchipelagoUtilities/blob/b608caded7996beb8d825e36e0bcad10ae7a3e64/KaitoKid.ArchipelagoUtilities.Net/KaitoKid.ArchipelagoUtilities.Net/Client/ArchipelagoClient.cs)
  obtains the active location set from the session. Its local Data Package can
  resolve individual IDs offline, but that global table does not establish
  which locations exist in a particular generated slot.

The [AP network protocol, LocationChecks](https://github.com/ArchipelagoMW/Archipelago/blob/main/docs/network%20protocol.md#locationchecks),
also checked in the companion APWorld checkout, explicitly permits sending
previously sent checks again. A durable complete set is sufficient; deleting a
check after sending it or maintaining a separate delivery queue is unnecessary.

## Findings in GraveyardKeepelago

- `SaveProgress.Checks` already serializes checks independently of received item
  receipts. `GKLocationChecker.AddCheckedLocations` remembers and stores checks
  before attempting to send them. Native saving makes that record durable on
  disk; updating the in-memory record is not itself a disk save.
- `Plugin.EnterGame` uses `ManualConnectionSession.TryEnterSave`; this requires
  an explicit connection. `InitializeAfterConnection` creates the location and
  technology runtimes and reads options/active locations from the session.
  A fresh process loading an AP save offline therefore lacks these services.
- After an established session disconnects, direct event recording through the
  existing registry/checker can continue. But inherited location enumeration
  still returns empty offline. The earlier claim that disconnection support was
  complete was too broad; every detection path needs an offline audit.
- `TechnologyRuntime` uses active locations, DLC settings and saved item-owned
  unlocks. AP reward replacement and unlock ownership must remain active
  offline, otherwise play can grant vanilla rewards or bypass AP progression.
- Goal detection currently gates on connection. An offline goal must also be
  remembered or re-evaluated reliably after reconnect.

## Proposed implementation

1. Keep versioned AP state inside the native save. Retain the complete local
   check set and independent receipt ledger. Add a seed-specific runtime
   snapshot: game/seed/team/slot identity, contract data version, runtime options
   (including DLCs and goal), and the actual active location name-to-ID map.
   Do not derive the active set from the global item/location tables. Do not
   copy passwords into this state.
2. Separate local save initialization from network attachment. A valid AP save
   initializes event recording, reward replacement, technology locks and unlock
   ownership before load-triggered quest events, regardless of connection.
   A vanilla save initializes ordinary gameplay when disconnected.
3. Base all local check enumeration and filtering on the snapshot and saved
   check set. Audit inherited non-virtual helpers and every direct session query;
   a small explicit local interface is preferable to pretending the network
   session exists. Validate snapshot consistency and installed DLC support.
4. Treat sending as an optional operation. Remember a check locally before any
   network call, update its serialized record immediately and persist it with
   the native save. Repeated events must remain idempotent. Persist goal state
   or re-evaluate it from reliable native state after reconnect.
5. On connection, validate game/seed/team/slot and contract/runtime snapshot
   before sending checks or applying queued item callbacks. Reconcile saved
   checks with server-known checks, resend the durable local set, and process
   received items using the existing receipt ledger. Checks reported by the
   server do not prove that corresponding items have already been applied.
   Network completion callbacks must not mutate Unity state off the game thread.
6. Offline rewards remain pending on the server: completing a check locally
   does not synthesize its AP item, including an item assigned to the same slot.
   Cached reward previews can remain visible; uncached previews use a neutral
   unknown presentation without fabricated item names or classification.
7. Add a deliberate migration for existing AP records without a runtime
   snapshot. One matching online load can populate and save it. Do not silently
   invent missing options/active locations or bind the save to another seed.
   If offline initialization cannot be validated, explain the missing upgrade
   rather than continuing the AP save with vanilla behavior. Preserve backups.

These are client persistence/lifecycle changes. No item or location rename,
renumbering or new seed should be necessary; check the snapshot against existing
APWorld slot data and active locations. The save schema/migration changes still
need explicit compatibility handling.

## Required regression tests for implementation

- Fresh process, no connection at any point, load AP save, complete technology,
  quest and crafting checks, native save, restart, connect to matching server:
  all saved checks are synchronized and receipts are not duplicated.
- Cover direct triggers and helpers that enumerate missing/all locations;
  positive/negative enabled-location and DLC cases must work offline.
- Disconnect after initialization and repeat the same sequence. Repeat events,
  repeated loads, reconnects and lost send acknowledgments must be safe.
- Wrong seed/team/slot/game/data version: no checks sent and no items applied.
- Offline AP reward suppression and received-item ownership remain effective;
  vanilla saves stay unaffected when switching between saves in one process.
- Offline goal completion is reported on reconnect; cached/uncached previews
  work without contacting the server.
- Existing record migration, missing/corrupt snapshots, and native-save
  round trips preserve identity, checks and receipt order. Include a manual
  Unity pass because pure serialization tests cannot prove native load order.

No production code was changed and no automated tests were run for this review.
