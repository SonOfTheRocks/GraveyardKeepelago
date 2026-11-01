# Morgue tutorial access in AP games

The vanilla `crafting_skull_3` flow first sets `lock_tp = 1` and `lock_tp_param = 1`
(node 55), immediately before the First slice reward. `Teleport` checks this
general lock before the morgue-specific flags and displays `lock_tp_1`: "I need
to cut off some flesh first." The first fix missed this earlier gate; an in-game
test confirmed that bypassing only the two flags below was insufficient.

The flow later sets `tp_from_morgue_only_with_body` and
`tp_to_morgue_locked` after Gerry's first autopsy conversation. `Teleport` checks
these using `Flow_PlayerFlag` (player `GetParamInt` → `GetParam`). They normally
clear in `on_after_first_bureal`. With AP withholding Extract: Flesh, these locks
can trap the player or prevent returning for the unfinished autopsy.

`MorgueAccessPatch` returns zero for these two player parameters and for `lock_tp`
only when its raw `lock_tp_param` equals 1, while the AP
technology runtime owns the current save. It intercepts `GetParam`, rather than
the tiny `GetParamInt` wrapper that the runtime may inline. Saved flags remain
intact, and the read override also handles saves already inside the tutorial.
Other door flags, non-player objects, dialogue/cinematic locks and vanilla games
retain their existing behavior. No recipe, item or location check is granted.

Validation: `dotnet test GraveyardKeepelago.sln --no-restore
-p:PluginsPath=bin/checked-client/` — 167 passed, including 17 access-policy cases.
Manual check after restarting the client: before receiving Extract: Flesh, enter
with Gerry, leave without carrying the corpse, return, and confirm extraction is
still locked. Repeat with a save already blocked inside the morgue. Interactive
Unity/Harmony behavior still requires this in-game check.
