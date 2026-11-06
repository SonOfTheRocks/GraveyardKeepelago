# Connection modal and AP reward presentation

The pause-menu Connection form deactivates the pause menu's GameObject while the
dialog is open, matching the native Options transition. It leaves the paused
state and native window stack intact. Closing the dialog returns to the pause
menu through its native OnAboveWindowClosed behavior. An idempotent background
lease also restores visibility on dialog construction failure/disposal.

The technology preview previously displayed the recipient twice: in parentheses
after the item name and again before the game/location description. Both values
came from RewardPreview.Recipient; neither represented a sender. Names now show
only the item. The description retains recipient, game and source check.

Forced technology reward dialogs backed by AP research checks use the title
`AP items unlocked` and subtitle `The following AP items were unlocked:`.
Regular research purchase confirmations retain their technology title, and
unrelated vanilla dialogs retain their text. These are the check's rewards,
which may be sent to another player; they are not necessarily local receipts.

Relation grants now produce a notification entry for their NPC. The receipt text
uses an explicit line break: localized NPC name, then `Happiness +10` (using the
actual grant amount). The amount is captured with the queued notification.
The icon follows
NPCItemGUI.Draw, the native NPC/relation overview: `char_` plus the NPC alias when
present, otherwise its ID. The row keeps the AP happiness item name, including
the amount in AP preview rows. The same data supplies technology preview icons. Missing portraits
fall back to the AP icon.

Relation receipts also clone the overview's full portrait group. Runtime inspection
confirmed `char scene place` (`char_wndw_j_frame_stand`, 82×100), its `character spr`
child and `light` (`char_wndw_j_frame_light`). The group keeps the native widget
colors, relative positions and draw order. Relation receipts reserve the full
native 82×100 portrait area inside a 90×108 portrait frame, with at least 120
units of row height instead of shrinking it into the small item-icon slot.
Runtime inspection confirmed that `icon/bg` is an independent 40×40 frame and
the outer `back spr` is 226×46. Both now resize together, with 6-unit outer
padding and an 8-unit gap before the text. The text width is reduced to the
remaining 116 units and wraps inside that area. Previously moving the text
without reducing its width made the table wider and shifted its boxes outside
the dialog. The earlier height-only layout assertion was replaced with full
frame-and-text bounds assertions to cover that regression.
The name moves right to leave room;
the original row height and text position return when the receipt closes.
It contains no interactive controls or source callbacks. Closing
the notification removes the group and restores the normal row icon, so the
shared native dialog can draw subsequent non-relation items normally.

Validation command: `dotnet test GraveyardKeepelago.sln --no-restore
-p:PluginsPath=bin/checked-client/`. Result: 214 passed, none failed or skipped.
Regression coverage includes recipient display, all relation grant mappings,
portrait alias selection, proportional stage sizing, invalid geometry fallback,
pending preview text and modal background restoration.
Existing compiler/analyzer warnings remain. Native visual layering and portrait
appearance still require an interactive check after deployment.

The receipt subheader no longer repeats the item name. Item application passes
the original ReceivedItem to the queued notification so sender metadata is
captured with that specific receipt: `<Player> sent you:`, `You found:` for the
local slot, or `The server sent you:` for server deliveries. Self detection uses
slot IDs, not names. Unknown sender names use `Another player sent you:`. Item
names remain in the item rows. Tests cover these headers and preservation of
the original sender through successful application.
