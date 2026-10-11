# Deferred Work

These topics were deliberately parked while establishing a clean client baseline. They should be
handled as separate work items in a later task rather than folded into unrelated changes.

## Main-menu connection flow

Finish the experimental connection panel for the main menu. The parked work currently adds a
Connect button, changes `Plugin.EnterGame` to accept host, port, and slot parameters, and uses
temporary hard-coded connection values. Hard-coded values are acceptable for the current
prototype, but the finished flow should eventually collect all required connection information,
including an optional password, show actionable connection errors, support retry, and avoid
duplicate patches, subscriptions, or connections.

Add lifecycle coverage for initial connection, failure and retry, reconnect, and save loading.

## Bootstrap recipe adjustments

Match the APWorld's intended early-game bootstrap changes in the client:

- Wooden Anvil: remove Simple Iron Parts from its construction requirements.
- Stone Cutter: remove Nails from its construction requirements.
- First Furnace: remove Simple Iron Parts from its construction requirements.

Do not guess the game recipe, workstation, or item IDs. Verify them against the game-data dumps,
decompiled game code, and the APWorld before implementation, then add regression coverage.

## Clotho lost-memories location checks

When quest-reward replacement is implemented, both mutually exclusive solutions to Clotho's
lost-memories quest must report the same three AP locations:

- `Clotho Reward - Give her the cauldron`
- `Clotho Reward - Give her health powder`
- `Clotho Reward - Give her a bee`

On the long route, `npc_witch` options 0, 2, and 3 report their corresponding location separately.
Choosing option 1 and giving Clotho a health potion instead must report all three locations at once.
The routes are mutually exclusive in the vanilla flow: option 0 unlocks options 2 and 3 and
immediately blacklists option 1; option 1 blacklists options 0, 1, 2, and 3. Preserve that behavior
when intercepting the choices, so a player can never claim part of the long route and then switch to
the shortcut for duplicate checks.
The three `pot_heal` items dropped after completing the long route are an asymmetric vanilla bonus;
do not replace, suppress, or report them as AP locations.

## Parked implementation

The experimental changes are stored in the Git stash named:

```text
WIP main-menu connection and bootstrap recipe notes
```

At the time this file was written it was `stash@{0}`, with stash commit:

```text
fbc675f15d4829dd90b282b195ebef3e14f6330f
```

Inspect the stash before applying it, and do not drop it until both topics have either been
implemented or intentionally superseded.
