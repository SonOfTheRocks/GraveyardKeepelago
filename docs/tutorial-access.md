# AP tutorial access and pause-menu connection

## Evidence and changes

The game checks `in_tutorial` in `GameSave.IsInTutorial`, the general
`WorldGameObject.CheckIfDisabledInTutorial` interaction gate, and the `sleep`,
`mining_hut_sleep`, `meditate` and `throw_body_river` graphs. The AP player reads
this flag as zero, without modifying the save's underlying tutorial state.

Two bed definitions use refusal scripts: `bed_no_sleep_1` → `need_put_body`, and
`bed_no_sleep_2` → `need_to_check`. The AttachFlowScript patch changes these exact
object/script pairs to `sleep`. Normal energy/rest requirements still apply.
`first_bed` → `first_sleep` is preserved because that script advances the story.

The `Teleport` graph reads `key_to_cellar` before using the home hatch. Only this
graph's player-flag node takes its allowed branch in AP. The same flag in
`on_enter_skull_back_zone` must still perform Gerry's introduction and reward
checks. The earlier morgue-specific flags and flesh-extraction lock bypass remain.
Other quest doors and cinematic locks keep their existing behavior. No item,
technology, check or quest is granted merely by bypassing these restrictions.

Returning home from the cellar before meeting the donkey also completes the
native `get_out_from_house_tech` / `get_out_from_house` quests. The latter runs
`lock_tps`, setting the general `lock_tp` with reason `lock_tp_param = 0`. The
normal outside route clears it after `go_to_talk_with_donkey_first_time`; entering
home from the cellar first instead traps the player behind both exits. The
2026-10-27 runtime log records that exact sequence after the Snake introduction,
followed by both `teleport_inside` and `teleport_hatch` saying `lock_tp_0`.

`TutorialDoorAccess` now takes `Teleport`'s unlocked (`== 0`) flag branch only
when the saved home-exit quest has succeeded, the donkey-introduction quest has
not succeeded, and the lock reason is exactly 0. The same check applies to a
previously affected save without resetting flags or fabricating quest progress.
The existing hatch-key branch remains independent. The global lock value and
other readers are unchanged; later reason-0 cinematic locks and other lock
reasons retain their behavior.

The pause-menu Connection entry clones the native Options row, replaces its
event/localization, inserts it immediately before Options, and registers it in
the native menu and gamepad navigation. Attachment waits until BaseMenuGUI.Init
has populated its items; GUIElements can expose the object before this step.
The existing connection form remains
responsible for settings, modal behavior and the loaded-save connection lock.

## Validation

`dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-home-exit-fix/ --verbosity quiet`
passed 389 tests. Regression cases include both blocked beds, vanilla behavior,
the first-sleep story, the general tutorial flag, cellar-only branching without
skipping Gerry's introduction, other quest locks and pause-menu placement.
Existing compiler/analyzer warnings remain; no test failures occurred.
New tests cover the home → cellar/Snake → home lock window, saved-state reuse,
continued hatch access, transition after meeting the donkey, other graph reads,
non-AP games and later cinematic lock reasons. Native `Flow_PlayerFlag` defines
the exact `== 0` and `>= 1` output IDs used by the patch.

Interactive checks still required: pause → Connection → Close returns to the
paused menu; mouse and gamepad can select the entry between Continue and Options;
sleep before extracting flesh; enter/exit the home cellar before Gerry's unlock;
complete Gerry's introduction afterwards; verify normal AP ownership still
blocks unreceived recipes. Check with a fresh save and an existing tutorial save.
For the home-return regression, wake up, use the hatch first, watch Snake's scene,
return home, then verify both hatch and outside door. Repeat on the already
affected save; after talking to the donkey, check normal story transitions.
No APWorld contract or seed regeneration is required for these client fixes.
