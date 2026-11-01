# AP item colors

AP item names use the server's item flags in receipt dialogs and technology reward
previews, including their mouse/gamepad tooltips and research result dialogs.
Base icons, sender/recipient details, and location descriptions retain their native appearance.
Technology reward icons also carry a category badge in the native quality corner:

| Category | Corner badge |
| --- | --- |
| Progression | Gold star (`item_star_3`) |
| Useful | Silver star (`item_star_2`) |
| Filler | Copper star (`item_star_1`) |
| Trap | Native skull (`i_skull`) |

The badge uses the scouted server flags for both local and foreign-game items,
including the generic AP icon. Combined flags follow the same precedence as the
name colors below. Unknown flags show no badge until the placement is loaded.
In these AP previews the corner denotes category, rather than the game's item
quality. Normal native unlocks and received-item quality icons are unaffected.

Implementation: `TechnologyRuntime.TryGetPreview` sets the native
`TechUnlockData.quality_icon`. `TechTreeGUIUnlockItem.Draw` updates and hides the
existing `item_quality` widget on each draw, so no custom overlay objects, tooltip
changes or additional cleanup hooks are needed. This also applies to the native
research result dialog that reuses the preview rows.

The mapping was verified against the local game sources and asset export:
`ItemDefinition.GetQualityIconName` uses `item_star_<quality>`; seed qualities
1/2/3 use copper/silver/gold. The exported `i_skull` sprite is the native skull
item icon. No new bitmap assets or shared-data changes are required.

The default palette and precedence match the companion Archipelago checkout's `NetUtils.py`:

| Flag | Name color |
| --- | --- |
| Advancement | `AF99EF` |
| NeverExclude (useful) | `6D8BE8` |
| Trap | `FA8072` |
| None / unrecognized bits | `00EEEE` |

Combined flags use the first matching row. Unknown placement data stays neutral
until scouting completes; absent flags are distinct from a known filler item.

Scouting copies flags from `ScoutedItemInfo`, including foreign-game items, without
creating hints. The received-item wrapper omits flags, so receipt presentation
matches its index, item ID, source location ID, and sending slot against the original
session history. Each queued notification captures those flags when the item applies.
Canonical names and item processing stay independent of presentation markup.

NGUI color scopes ignore the native label tint and close after the item name.
Relations keep the existing NPC / Happiness two-line layout within that scope.
Technology reward previews use darker progression violet (`79439C`) for contrast
on the light background, including mouse/gamepad tooltips. Receipts keep the
original progression violet (`AF99EF`). Other item colors and flag precedence
are identical in both contexts. Technology tooltips use their original native
font and outline; the experimental black-outline patch was removed after the
in-game check showed insufficient contrast. Its marker test was replaced by
coverage of the two palettes and unknown classifications.

Receipt and technology preview icons share `ItemNotificationWindow.GetData`:

- Casual prayer (`preach_1`) uses `i_b_empty`, the prayer book with an empty circle.
  This is a presentation override; the granted game item remains unchanged.
- Research points use `techno_point_icon04` (red), `techno_point_icon03` (green),
  and `techno_point_icon02` (blue), matching the technology window's point symbols.
  The previous `i_r` / `i_g` / `i_b` icons were round pickup icons.

Icon names and appearance were checked against the local game asset export's
Sprite definitions and Texture2D images (`i_b_empty` is on the icons atlas).
Native reference: `TechTreeGUI.Redraw` / `PlayerComponent.GetTechPointsString`
render point totals with `(r)`, `(g)`, `(b)`; `TechTreeGUIItem.Draw` uses the
same point symbols in research prices.

Validation for the category badges and pending console-filter additions:
`dotnet test GraveyardKeepelago.sln --no-restore
-p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-category-badges/
--verbosity quiet` passed 359 tests with zero failures/skips. Output is in
`out/category-badges-suite.log`. The focused badge/color/console run passed 97
tests (`out/category-badges-tests.log`). Tests cover all four categories, combined
flags, foreign-game previews, unknown flags and replacement of cached previews.
Visual appearance still requires an in-game check after installing the build and
restarting the game. The current running game locks the installed client DLL;
the tested build is staged in the client output. No new seed is required.
