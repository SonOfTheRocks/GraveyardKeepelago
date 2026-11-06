# Organ-workbench recipe icons

AP technology previews and received-item windows reuse an organ recipe's own
native icon when available. If it is missing, `RecipeIconFallbacks` selects the
normal recipe for the same product. Pairing comes from shared item keys ending
in `_ORGAN_WORKBENCH` and their normal counterparts, rather than a second manual
mapping of product names. Only `ITEM_LEARN_*` craft-unlock items are considered;
the workbench blueprint and other recipes are unchanged.

The seven pairs in the current shared data are:

| Organ craft | Normal craft used for a missing icon |
| --- | --- |
| `story_souls` | `story` |
| `chapter_souls` | `chapter` |
| `book_souls` | `book_hard` |
| `sack_clock_silver_souls` | `sack_clock_silver` |
| `@sack_clock_gold_souls` | `sack_clock_gold` |
| `sack_star_silver_souls` | `sack_star_silver` |
| `@sack_star_gold_souls` | `sack_star_gold` |

The native `@` prefix marks hidden technology unlocks. The two fertilizer-II
items contain only hidden craft IDs, so the previous notification entry filter
left their AP previews without an icon source. `ItemNotification.Create` now
adds a presentation entry for these known hidden-only organ recipes. It strips
`@` only in that presentation entry, using the actual canonical native craft ID.
The grant definition and applied unlock retain their original values. Other
hidden unlocks remain hidden.

`ItemNotificationWindow.GetData` checks the actual native craft/sprite
availability before choosing the normal recipe. Both technology previews and
receipt windows use that same path. Existing organ icons are preferred; if
neither recipe has an icon, the existing caller's AP-symbol fallback remains.
Preview category badges and ownership/grant behavior are unchanged.

Native `CraftDefinition.GetCraftIcon` uses an explicit icon or the first real
output's icon. The raw craft dump confirms the canonical fertilizer-II organ
IDs and their `i_sack_clock_gold` / `i_sack_star_gold` sprites, matching their
normal recipes. `book_souls` has no native craft definition in the current dump;
its normal book recipe supplies the icon instead.

Validation, 2026-10-28, from the client root:

```powershell
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-organ-icons/ --verbosity quiet
```

Result: **403 passed**, no failures or skips; existing compiler/analyzer warnings
remain. Tests cover all seven pairs, preservation of existing icons, unavailable
fallbacks, both hidden fertilizer-II preview entries, unchanged grant IDs and
unrelated/hidden recipes. The build is staged inside the client repository.

No shared-data, APWorld, version or seed change is needed. Manual rendering
still needs checking in the game after installing the client and restarting it.
