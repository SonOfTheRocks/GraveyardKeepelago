# Archipelago journal

Implemented 2026-10-28 according to the [agreed plan](archipelago-journal-plan.md).
The initial in-game screenshot was reviewed with the user. The revised layout below
has automated geometry coverage; its rendered result and physical mouse/controller
interaction still need a new in-game check.

## Behavior and implementation

`JournalPlayerTab` inserts a native `GameTabItemGUI` immediately after Map in the
existing GameGUI tab dictionary and physical table. It preserves all native tabs and
uses their normal click, selection and shoulder-button cycle. Both tab dictionaries
are restored on disposal. The journal is mounted only for an initialized AP save.

`JournalGUI` uses the existing Options/dialog bitmap fonts, sliced frames and text
input. Its layout fits the native Map viewport and follows resolution/UI-scale changes.
It uses the player menu's existing border/background, without a nested dialog frame.
`JournalLayout` assigns separate bounds to the filter bar, history, status/Latest row,
input/Send row, and help line. All left and right edges share those bounds. Button
labels use centered NGUI raw pivots so their glyphs align with the centers of their
frames without NGUI shifting the explicitly placed transforms.
Only enough row labels for the visible viewport are instantiated. Measured message
heights are cached, preserving scroll position as entries arrive and when the oldest
entries are evicted. Mouse-wheel, drag, keyboard and controller scrolling are wired.
The right-hand scrollbar reserves its own column outside the clipped text viewport.
Its draggable handle and clickable track use native selection widgets, with a minimum
handle height for long histories. It shares the same offset with all other scrolling
controls and Latest; it hides when all messages fit without scrolling.
Each row has a right-aligned time label and a separate message label: wrapped lines
align with the start of the message rather than extending into the time gutter.
Journal player/item colors use the dark-background palette, and timestamps are muted.
The input guards player-menu shortcuts while editing. Item notifications
wait for all other windows to close, so they do not interrupt journal reading.

`JournalMessageSubscription` captures plain messages with the identity of the actual
network session. It subscribes before the utility client's normal initialization and
unsubscribes on cleanup. Disposed callbacks are ignored, and queued messages from other
seeds, teams or slots are rejected. This includes callbacks delayed across reconnects.

Important installed-library detail: utilities 3.1.11's `GetSession()` calls
`MakeSureConnected()`. Calling it before base `InitializeAfterConnection()` sets
`IsConnected` would attempt another login. The override reads that version's private
authenticated `_session` through Harmony's field access instead. A lifecycle regression
test invokes the actual override with real AP helpers and no socket connection.

The server log is the single source of journal item-transfer entries. Item application
and the item-notification feed do not append a second entry. Ordinary transfers use sender team,
sender slot, recipient slot, source location and item ID as a deduplication key. Special
negative locations and cheat messages can legitimately repeat; hints are separate System
entries. Repeated chat is always retained. All retains general chat/system messages and
filters unrelated item transfers by default. Filtered-out entries are not marked read.
Reaching Latest marks that filter's history read; reading earlier entries marks visible
entries only.

The adapter preserves server message-part spacing and AP item flags. Text is escaped
before NGUI rendering so user messages cannot inject links, colors or alpha commands.
One message is limited to 4,096 text characters and 256 parts. Outgoing chat is limited
to 500 characters on one line and is sent via the current session's Say operation.
It is displayed when echoed by the server, without an additional optimistic local copy.

`ItemNotificationHud` shows received items during unobstructed gameplay using native HUD typography. The previous unread counter is removed. The Archipelago tab still shows an asterisk for unread messages.

`JournalStore` retains at most 2,000 entries in a versioned JSON file keyed by a SHA-256
hash of game, seed, team and slot. It writes a temporary file and atomically replaces the
primary with a backup. A corrupt primary can recover from the previous backup. Invalid
format/identity, invalid records and oversized files are rejected. If reading fails
without recovery or writing fails, history continues in memory; one diagnostic is logged
and storage is retried on the next explicit connection. Damaged files are not overwritten
by an empty history. The journal never grants items, sends checks, stores credentials or
changes the gameplay save schema.

No APWorld, shared data, item/location IDs or slot-data changes were made. The existing
client contract tests remain the compatibility check.

## Automated verification

Run from the client repository with the SDK selected by `global.json`. Build deployment
is redirected into the repository so validation does not install into the running game:

```powershell
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) '.dotnet-home'
dotnet test GraveyardKeepelago.sln --no-restore --filter FullyQualifiedName~Journal -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal/ --verbosity quiet
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal/ --verbosity quiet
```

The original baseline passed 455 tests. The focused journal run passed 26 tests;
the final full run passed 481 tests, with zero failures or skipped tests. Results
are recorded in `out/journal-focused-tests.log` and `out/journal-full-tests.log`.
Existing nullable/analyzer warnings remain. No new journal-source warnings were
reported. The staged plugin output is `out/codex-journal/`; it has not been
installed into the debug game by these validation commands.

Coverage includes classification and default filters, item deduplication, repeated chat,
AP flags and spacing, NGUI escaping, unread behavior, scroll anchoring, bounded history,
party-specific storage/restart, backups/corruption, failed reads/writes, initial batches,
late session callbacks, subscribe/unsubscribe across reconnects, authenticated-session
initialization, native tab order, outgoing chat validation and server-command forwarding.

## Required in-game checks

1. Connect, load a matching AP save and open Tab. Confirm Archipelago sits immediately
   right of Map, every original tab works, and shoulder-button cycling wraps correctly.
2. Open the player menu with Tab and select Archipelago. Verify normal tab switching and controller navigation.
3. Receive and send an item, including one assigned to your own slot. Confirm one journal
   transfer per source location, the correct recipient, item colors and unchanged receipts.
4. Send chat and a server command. Check Enter, Send, focus, selection, arrows, Home/End,
   Backspace/Delete, Tab, Escape and controller focus/send. Confirm the player menu does
   not switch tabs or close while the text field owns those keys.
5. Exercise every filter and Other items. Arrivals while reading earlier entries must
   preserve the position. Latest should clear the current filter's unread messages.
6. Check long wrapped entries, a full history, mouse wheel, drag, keyboard and controller
   scrolling. Verify item notifications hide and pause during reading; the former HUD unread count is removed.
7. Disconnect, continue recording existing-session checks, receive messages after reconnect,
   close/reopen the journal, exit/reload, restart the game and switch AP parties. Check
   retained history and absence of duplicates or messages leaking between parties.
8. Repeat at another resolution and UI scale. Check the full native tab strip, bottom chat
   controls, input font/baseline, readable colors, clipping and HUD indicator placement.

## Screenshot layout revision

The user's initial screenshot showed captions against the upper edges of buttons,
two footer text lines extending into the input, a redundant inner window border,
excess space between filters and the first message, and wrapped messages extending
back into the timestamp column. These issues are addressed by the shared bounds,
centered button pivots, separate footer rows and separate timestamp/message labels.
Message spacing was reduced; the history now begins half a text line below the filters.

Validation for this revision:

```powershell
dotnet test GraveyardKeepelago.sln --no-restore --filter FullyQualifiedName~Journal -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal-layout/ --verbosity quiet
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal-layout/ --verbosity quiet
```

The focused run passed 37 tests; the full run passed 492 tests, with no failures or
skipped tests. Existing compiler/analyzer warnings remain. Logs are in
`out/journal-layout-focused-tests.log` and `out/journal-layout-full-tests.log`.
The output for this revision is `out/codex-journal-layout/`. It takes effect only
after restarting the game with the updated plugin.
Windows initially refused to replace the running game's loaded DLL. After the user
closed the game, this revision was installed and its DLL hash verified against the
tested output. The debug game was then restarted for the next visual check.

## Text focus, native panels and log review

The next screenshot exposed global Inventory/Techs/NPCs/Map shortcuts bypassing
the GameGUI typing guard. The LazyInput query now blocks menu navigation while
any UIInput is selected, including configurable key bindings. Native submit,
cancel and mouse input remain available. Character's `dark back`, `top --------`
and mirrored triangle separators supply the journal texture, header boundary and
footer divider. The divider has reserved clearance on both sides. The HUD count
uses rawPivot (the pivot setter moved the label) and clamps its rendered bounds.

The disk log contained repeated missing ObjectGroupDefinition `p_t_lifestone`
and empty-sprite errors when opening Techs. GameBalance and the native technology
unlock list identify this as a perk, whereas both shared item_grants copies used
work_ids. Both copies now use perk_ids; the regression test verifies the applied
unlock type. Names and network IDs are unchanged. Already-consumed receipts are
not replayed by this correction.

Repeated NGUI layer warnings occurred when opening Character, and World Object
not found also occurred when opening Map. The available log does not identify
the offending objects/callers. A diagnostic prefix leaves NGUI's warning and repair
in place and records each affected widget's hierarchy, panel, layers and call stack
once. This remains an open runtime investigation, not a verified layer fix.

Validation:

```powershell
dotnet test GraveyardKeepelago.sln --no-restore --filter 'FullyQualifiedName~Journal|FullyQualifiedName~LimestoneGathering' -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal-panels/ --verbosity quiet
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal-panels/ --verbosity quiet
```

56 focused tests and all 511 tests passed, with no failures or skips. Existing
compiler/analyzer warnings remain. Logs: `out/journal-panels-test.log` and
`out/journal-panels-full-test.log`. Both shared grant files have identical SHA256
hashes. Native sprite appearance, text input and the remaining runtime warnings
still require the next in-game check.

## Completion and decorative-edge revision (2026-10-29)

The follow-up screenshot confirmed text focus and HUD placement. Corner origins
were incorrectly treated as the ends of the divider. Their native offsets from
the line's rendered bounds are now preserved, including the vertical pivot offset.
The plain Character `dark back` prototype was indeed a flat fill. A reusable stone
texture, `char_wndw_techtree_bg_1`, has been visually inspected in the exported game
assets and is now resolved from the native technology UI or sprite collection.
If unavailable, the existing fill remains; no panel-specific text/artwork is copied.

Command completion previews a suggestion in the status row while the caret is at
the end of an unselected input. Tab inserts it without sending or changing focus;
Ctrl+Space cycles candidates. Native Tab navigation remains available when there
are no suggestions. Both UIInput.OnKey and UIKeyNavigation.OnKey are intercepted
for this journal field because NGUI handles Tab in both places. Other inputs retain
their normal Tab behavior. Server commands are a documented baseline verified
against this checkout's ClientMessageProcessor, not a server-discovered schema.
Local cached item/location names complete the corresponding command parameters.
Unknown commands, slash-prefixed text, free arguments and mid-line editing remain
untouched; no admin/password suggestions or network requests are made by completion.
Group names and arbitrary server extensions are not currently discovered. Names
may include content outside the current seed; the server remains authoritative.

The new diagnostic log identified the layer mismatch as the native Inventory GUI's
Multi inventory panel/scroll panel/content table/separator: InventoryPanelGUI.Open
creates a default-layer GameObject and attaches it under the layer-13 panel without
assigning its layer. NGUI repairs it. This is a native invisible spacing widget,
not a cloned journal widget. Its warning remains visible. The previous limestone
lookup/empty-sprite errors did not recur in the new run. Other native world-object
and cleanup warnings remain; this revision does not claim a warning-free game.

Validation:

```powershell
dotnet test GraveyardKeepelago.sln --no-restore --filter 'FullyQualifiedName~Journal' -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal-completion/ --verbosity quiet
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal-completion/ --verbosity quiet
```

79 focused and all 535 tests passed, with no failures or skips. Existing compiler
warnings remain. Logs: `out/journal-completion-focused.log` and
`out/journal-completion-full.log`. Build: `out/codex-journal-completion/`.
After the user's shutdown confirmation this build was installed and its DLL/PDB
hashes verified against the tested output. The debug game was restarted (PID 94380)
and the fresh log confirmed GraveyardKeepelago loaded without a startup exception.
The prior build and log were saved under
`out/journal-before-completion-eb32cec3d13a44c7b18788a1322a3448/`.
That previous log records a shutdown NullReferenceException in
InteractionBubbleGUI.ChangeBubblesVisibility via JournalGUI.Hide/BaseGUI.Hide,
where BaseCharacterComponent.player was unavailable. This needs lifecycle
investigation; a successful restart does not verify that shutdown path.
In-game verification of corners, texture, Tab focus and Ctrl+Space is still required.

## Popup, contrast and timestamp revision

The user confirmed the stone texture but found insufficient contrast, truncated
timestamps and clipped divider ornaments. They also rejected the status-row
completion and reported that Ctrl+Space inserted a space. That shortcut has been
removed. An opaque native-widget popup now displays up to six matching commands
or argument names, a selection highlight, count and description. Up/Down moves
the selection and scrolls it into view. Mouse hover/click and wheel are supported.
Tab or Enter accepts without sending; a subsequent Enter sends. Escape dismisses
the popup first. Typing resumes completion. Native UIInput.ProcessEvent is patched
before caret movement, and both native Tab handlers remain guarded. Normal spaces
remain text input. The popup has its own panel above the history and decorations.

The existing game stone texture is darkened by a widget tint; no image or icon was
generated. The user's prohibition on AI-generated media is recorded in both project
AGENTS.md files. Only code, documentation and data files may be AI-generated.

Time strings were already formatted HH:mm; the proportional bitmap font made the
00:00-only width estimate insufficient. All 1,440 timestamps are now measured once
at construction, with outline/rounding clearance. Timestamp widgets cannot clamp
their final glyph. Decoration widgets now use the full draw region and an unclipped
panel above the history. Their native inset/pivot offsets are retained.

The shutdown log again showed BaseGUI.Hide invoking character-dependent callbacks
after the character was destroyed. Journal cleanup now skips tab activation and
those callbacks when the game/player/character is unavailable, removes its window
stack entry, disables navigation and preserves the journal flush. Normal in-game
close behavior remains native. This still needs a real shutdown check.

Validation:

```powershell
dotnet test GraveyardKeepelago.sln --no-restore --filter 'FullyQualifiedName~Journal' -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal-popup/ --verbosity quiet
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal-popup/ --verbosity quiet
```

91 focused and all 547 tests passed; no failures or skips. Existing compiler warnings
remain. Logs: `out/journal-popup-focused.log`, `out/journal-popup-full.log`.
Tested build: `out/codex-journal-popup/`. Native popup rendering, mouse/keyboard
interaction, contrast and the shutdown path still require in-game verification.

The debug game had already closed, so this build was installed with verified
DLL/PDB hashes and restarted (PID 26916). SHA256:
`AD09F3098356BC5521BC0A9AD6D90673478F07B46C46440B5AA8DD4296B34536`.
Previous plugin/log backup:
`out/journal-before-popup-d9f60445c35b45c6ad8a66a983ede387/`.
The fresh start log confirms the mod loaded; no startup exception or patch failure
was observed. Native missing FlowCanvas assets still produce warnings.

## Local-day separators and immediate parameter suggestions

The filtered history now starts each consecutive local calendar day with a centered
dd.MM.yyyy date separator. Grouping uses TimeZoneInfo.Local, as do message timestamps;
dates are not inferred from UTC. Original arrival order is retained. Headers are
presentation rows only: they do not enter persistence, unread accounting or the
message count. Pooled labels reset alignment/width/color when reused for messages.
Scroll anchoring accounts for the header height when old messages leave the bounded
history. Tests cover empty/same-day history, local midnight, year boundaries, filtered
entries and preservation of order/read state.

Accepting a command with a known argument catalog now immediately opens its parameter
list (for example !hint_location followed by a space). Accepting an actual parameter
still dismisses completion; commands with free-text arguments or unavailable name
catalogs do not open an empty list. The same-frame guard prevents one Tab/Enter event
from accepting both the command and the first parameter.

The opaque suggestion popup now has a bright native dialog-frame border with a
transparent center, above its fill and selection rows. The footer divider's two
ornaments move inward by two source pixels, respecting scale and mirroring. The top
divider retains its existing position. All artwork remains native game assets.

Validation:

```powershell
dotnet test GraveyardKeepelago.sln --no-restore --filter 'FullyQualifiedName~Journal' -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal-days/ --verbosity quiet
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal-days/ --verbosity quiet
```

105 focused and all 561 tests passed, with no failures/skips. Existing compiler
warnings remain. Logs: `out/journal-days-focused.log`, `out/journal-days-full.log`.
Build: `out/codex-journal-days/`. Native-frame appearance, date-row rendering and the
command-to-parameter transition still require in-game verification.

Installed after confirming the debug game had closed; DLL/PDB hashes matched the
tested output. Restarted PID: 68028. DLL SHA256:
`949F3E4A43FEBB2929EEAD43BFCF7411589AA3E27AFAE10C0FA0B0832330EF78`.
Previous build/log backup: `out/journal-before-days-93cce828837f4b46bc149165090f4856/`.

## Simple popup rim and independent panel-rail alignment

The next screenshot confirmed that the decorated dialog frame covered the first
characters and outer suggestion rows. It has been removed. The existing native
selection-background widget supplies a thin bright rim behind an inset opaque
fill; both render behind the highlight and text. The border stays inside the
existing popup bounds and outside the text padding, without a modal frame's wide
artwork or transparent-center slicing assumptions.

Moving the entire footer ornament inward had aligned its lower stem but displaced
the upper one. The upper rail now keeps the original native position. A second
native copy follows the lower panel's existing inset. Hard-edged NGUI clip panels
meet at the divider, revealing only the corresponding halves, preserving the
original sprite geometry/UVs and avoiding new artwork. The top divider is unchanged.
Tests verify matching half boundaries, no negative clips at edge cases, and border
clearance inside text padding at each supported layout size.

Validation:

```powershell
dotnet test GraveyardKeepelago.sln --no-restore --filter 'FullyQualifiedName~Journal' -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal-simple-border/ --verbosity quiet
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-journal-simple-border/ --verbosity quiet
```

110 focused and all 566 tests passed, with no failures or skips. Existing compiler
warnings remain. Logs: `out/journal-simple-border-focused.log` and
`out/journal-simple-border-full.log`. Build: `out/codex-journal-simple-border/`.
Native border visibility and the upper/lower ornament alignment still require an
in-game rendering check.

Installed with verified DLL/PDB hashes after the debug game had closed, then
restarted (PID 32492). Fresh log confirms the client loaded without a startup
exception. DLL SHA256:
`574124F65E901571C426E0A81E0D0D3032F24F796F5EFC8A74F6393C2F53F050`.
Backup: `out/journal-before-simple-border-a3e3e717282c43eaa39102f550031ff3/`.

## Passive item notifications

The gameplay HUD unread-message count has been replaced by a text-only received-item feed.
The journal and tab unread marker remain. Receipt dialogs no longer open. Native HUD labels
show source and colored AP item names, wrap long names, stack newest at the bottom and animate
older rows upward. Four receipts can be visible, paced at 1.2 seconds; overflow stays queued.
Each receipt remains fully readable for ten seconds, plus 0.2 seconds fading in and 0.8 seconds fading out. During dialogue, cutscenes and menus the feed hides and
its clock stops. Both pending and visible rows are cleared on save/session changes. No artwork
is generated or added. Unity rendering still needs manual verification in the debug game.



The separate journal hotkey and Journal/OpenKey configuration were removed on 2026-10-30; use Tab > Archipelago.

