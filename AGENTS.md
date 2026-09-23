# GraveyardKeepelago Client — Agent Guide

## Scope and repository role

This repository contains the **Graveyard Keeper Archipelago client**, implemented as a BepInEx 5 Unity mod. It connects the game to a multiworld, converts game actions into checks, applies received items, and reports completion.

The companion APWorld lives in `C:\Users\Kai\PycharmProjects\Archipelago_GraveyardKeeper`, primarily under `worlds/graveyard_keeper`. Names, IDs, slot data, goals, DLC behavior, and Data Package assumptions form a shared contract and must be checked there too.

## Reference implementations

Use these references in increasing complexity:

1. `agilbert1412/DLCQuestipelago` for a smaller C# client.
2. `agilbert1412/StardewArchipelago` for mature connection handling, persistence, item processing, location checks, testing, and organization.
3. The installed `Archipelago.MultiClient.Net` and `KaitoKid.ArchipelagoUtilities.Net` versions for APIs actually available here.
4. The companion APWorld and its `/docs/network protocol.md` for the server/client contract.

Borrow patterns selectively. StardewArchipelago is much larger; do not reproduce its architecture unless Graveyard Keeper needs it.

When behavior is unclear, inspect game assemblies, dumps, references, and the APWorld. Do not guess internal IDs, save semantics, event order, or protocol behavior. Ask the user when evidence does not resolve ambiguity.

## Technology and architecture

- Production target: `netstandard2.0`.
- The SDK in `global.json` is authoritative.
- Mod loader: BepInEx 5.x.
- Patching: Harmony through Harmonize.
- Network client: `Archipelago.MultiClient.Net` through KaitoKid utilities.
- Tests: xUnit, Moq, and .NET Test SDK.

Important areas:

- `Plugin.cs`: composition root and lifecycle.
- `Archipelago/`: connection, slot data, DLC validation, Data Package cache.
- `Items/`: received-item pipeline and traps.
- `Locations/`: check detection and patches.
- `Goals/`: completion detection/reporting.
- `GameModifications/Patches/`: general patches.
- `Deathlink/`: DeathLink integration.
- `Logic/`: AP names mapped to game behavior.
- `IdTables/`: local Data Package/cache artifacts.
- `GraveyardKeepelago.Tests/`: tests.

Keep Harmony patches thin. Put decisions and business logic in testable services/handlers rather than prefixes and postfixes.

## APWorld ↔ client contract

Treat this boundary as a versioned public API, including exact game name, item/location names and IDs, progressive stages, slot-data keys/types/enums/defaults, goals, DLC identifiers, compatibility versions, and Data Package behavior.

Until a shared manifest exists, check every contract change in both repositories and test it where practical. Do not silently hide an APWorld mismatch behind a different client default. Fail clearly or preserve explicitly documented compatibility.

Avoid hand-maintaining a second mapping where the planned shared manifest can generate it. Until then, isolate mappings and add parity tests to prevent drift.

## Runtime correctness

The client must remain correct across saves, disconnects, reconnects, and replayed packets.

- Persist enough state for idempotent item processing.
- Track the received-item index or equivalent library state correctly.
- Persist or reconstruct checks so one-time offline actions are sent after reconnect.
- Never grant a non-repeatable item twice after reload/reconnect.
- Validate that a save belongs to the expected seed, slot, and game.
- Handle the initial item batch even if it arrives during connection/setup.
- Prevent duplicate patches or subscriptions on reconnect.
- Report completion safely while tolerating retry.
- Do not terminate the game for a recoverable connection error; provide an actionable retry/menu path.
- Never hard-code production host, port, slot, or password.
- Marshal Unity/game-object work from network callbacks to the game thread when needed.

Consume authoritative slot data for DLC/option behavior and reject incompatible local DLC state clearly.

## Item and location behavior

- Keep received-item routing deterministic and ordered.
- Unknown items need useful diagnostics without corrupting processing state.
- Progressive items must handle first receipt, later stages, duplicates, and vanilla pre-unlocks.
- Trap behavior must be independently testable and must not permanently block the queue.
- Prefer canonical AP names over fuzzy matching when exact mapping exists.
- One-time game events must be recoverable if they happen offline.
- Reward-replacement patches must prevent duplication without breaking quest/technology progression.

### Hidden technology-tree unlocks

In `TechDefinition` unlock lists, a leading `@` is metadata that marks an unlock as hidden in the
technology-tree presentation; it is not part of the canonical game ID. The game strips the prefix
before persisting craft, work, phrase, and perk unlocks. `TechUnlock` also strips it and sets
`visible = false`. Preserve the prefix only when reproducing a hidden technology-tree unlock, and
compare or look up the underlying object using the unprefixed ID. Do not add or remove `@` merely
to satisfy a test: verify the corresponding entry in the game-data dumps or decompiled code.

## Testing is mandatory

High coverage is a core requirement. Every implementation or refactor includes tests as part of the work.

- Add or update tests for every behavior change and bug fix; prefer a regression test that fails first.
- Test handlers/services without launching the game where possible.
- Wrap static game APIs behind narrow interfaces when this enables reliable tests.
- Cover success, invalid input, duplicate delivery, reconnect/replay, ordering, and recovery.
- Add contract tests for slot data, enums, defaults, names, IDs, and generated manifest data.
- Add lifecycle tests for initial connect, delayed initialization, disconnect, reconnect, and save reload.
- For patches, unit-test extracted decisions and use integration tests only for unavoidable wiring.
- Never remove or weaken a failing test merely to make the suite green.
- Align production/test dependency versions unless a reason is documented.

Standard command:

```bash
dotnet test GraveyardKeepelago.sln
```

Run focused filters while iterating. At completion, report exact commands/results and pre-existing failures. A build alone is not sufficient verification.

## Build and local dependencies

```bash
dotnet build GraveyardKeepelago.sln
```

The project references local Graveyard Keeper assemblies and has a post-build copy into BepInEx. Before running/changing builds:

- inspect `global.json` and `GraveyardKeepelago.csproj`, not stale prose;
- do not assume all developers or CI share the relative game path;
- avoid new machine-specific absolute paths;
- distinguish missing proprietary assemblies from code failures;
- do not claim success if post-build deployment failed.

The `Dump` configuration enables `DUMP_MODE`. Dumps are evidence, not automatically authoritative; validate important conclusions in game or tests.

## Completion checklist

- Names, IDs, slot data, enums, goals, and DLC behavior agree with the APWorld.
- Initial connection, reconnect, and save reload are safe.
- Replayed packets/callbacks cannot duplicate rewards.
- Unity calls use a safe lifecycle/thread context.
- Option and DLC behavior is explicit.
- Positive, negative, regression, and lifecycle tests pass.
- Connection failures are recoverable.
- Production paths contain no developer connection data.
- README/setup matches the actual flow.
- Unresolved behavior is raised to the user rather than guessed.
