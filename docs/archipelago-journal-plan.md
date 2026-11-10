# Archipelago journal — agreed plan

Agreed with the user on 2026-10-28. The first implementation is now in the client;
see [implementation and verification notes](archipelago-journal.md). In-game visual
and interaction checks remain outstanding. The agreed scope is preserved below.

## Player experience

- Add a new **Archipelago** tab immediately to the right of **Map** in the
  player menu opened with Tab. Do not add the journal to the pause menu.
- Integrate with the existing native tab selection, including controller
  shoulder-button navigation. Provide a configurable direct-open shortcut.
- Show a scrollable chronological history with timestamps, player names and
  item names colored using the existing AP presentation palette.
- Offer **All**, **My Items**, **Chat** and **System** filters. Initially show
  item transfers involving the local player, together with general chat and
  system messages. An explicit setting can include other players' transfers.
- Append new entries at the bottom. Follow the latest messages only when the
  reader is already at the bottom. Preserve the reading position otherwise
  and offer a new-messages indicator with a way to jump to the latest entry.
- During gameplay, initially provide only a discreet new-messages indicator.
  Short message overlays are a later enhancement, not part of this version.
- Include a chat input and send action. Forward server commands such as
  `!hint` to the server; local convenience commands are outside this scope.
- Keep existing item receipt windows. They must wait while the player reads
  the journal, and dismissing a journal entry must never apply an item.
- Use the native game's visual style and support mouse, keyboard and controller.

## Message collection and storage

- Collect server chat, item transfers, hints, goal announcements and relevant
  connection/system messages. Keep diagnostic logs separate from the journal.
- Convert network messages to plain presentation records and process UI changes
  on the game thread. Preserve meaningful message parts for item/player colors;
  escape untrusted text so it cannot inject NGUI formatting.
- Avoid duplicate item entries from receipt processing, server announcements
  and reconnect/replay. Do not deduplicate legitimate repeated chat by text.
- Retain up to 2,000 entries in a separate, versioned journal file per game,
  seed, team and slot. Never persist passwords or connection credentials.
- Keep journal history independent of gameplay saves and progression state.
  Reading it never grants items or sends checks. Switching parties must not
  mix their histories.
- Missing, corrupt or unwritable journal files must not prevent loading or
  playing. Preserve recoverable history where practical and report failures
  through diagnostics without repeatedly interrupting gameplay.
- Record only messages actually received. Do not claim to reconstruct chat
  missed while disconnected or before the journal was installed.

## Implementation sequence

1. Add testable message classification, history/filter/unread behavior,
   party identity and bounded storage.
2. Wire the client message stream and connection lifecycle. Resolve duplicate
   item announcements without changing existing item receipt handling.
3. Add the native player-menu tab after Map, scrolling, filters, unread
   indicators, chat input and a configurable direct-open shortcut.
4. Run focused tests and the full client suite; document manual UI checks and
   the supported behavior in the README.

## Validation and compatibility

- Test message categories and default filters, chronological ordering, repeated
  chat, item replay/deduplication and arrivals during initial connection.
- Test persistence, the entry limit, corrupt/unwritable storage, party isolation,
  reconnects and shutdown/restart.
- Test unread tracking while viewing older entries or a different filter,
  plus send behavior when connected/disconnected and invalid/empty input.
- Verify the integration against the actual GameGUI/GameTabItemGUI behavior;
  source inspection shows native tabs are held in dictionaries and cycled by
  NextTab, so the new tab must participate in that same ordering.
- Manually check placement immediately after Map, tab switching, mouse wheel,
  controller navigation, chat editing, Escape/Tab behavior, new-message arrival
  while reading, and interaction with item windows at multiple resolutions/UI
  scales. Automated tests do not establish Unity rendering correctness.
- No APWorld, item/location IDs, slot-data or seed-format changes are expected.
  Check that expectation during implementation.

## Later enhancements

Short gameplay message overlays, additional local commands, expanded hint
management and an open-checks overview are separate follow-up work.
