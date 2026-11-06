# Console noise whitelist and receipt icons

Only BepInEx.ConsoleLogListener.LogEvent is patched. Debug diagnostics and normal
Unity Info/Message output are suppressed at this listener. Player-facing mod
Info/Message output remains visible: AP connection state, completed/sent/offline
checks, received items and server messages. Raw network packets, GUI inspections,
native crafting callbacks, inventory initialization and resource/dump details
are Debug. Errors from keyboard actions are Error rather than Info.

Warnings, errors and fatal messages remain visible, including mixed severity flags.
Exact exceptions from source `Unity Log` are:

- Warning: `Cannot use 'MovePosition' on a static body.`
- Warning: `<b>(NodeCanvas Warning)</b>: Graph is already Active.`
- Error: `World Object not found`
- Warning: `Error adding sprite to a library - duplicate name: <sprite>`
- Warning: `Error loading asset "<asset path>"`
- Error: `Can't deserialize WGO with an empty id.`
- Error: `Could't find zone [<zone>]` (the game's spelling), also `Couldn't`.

Variable sprite names, quoted asset paths and bracketed zone names must be
nonempty and contained on one line. The asset and zone messages must end at the
closing quote/bracket; extra exception/context lines remain visible.

Near matches, other sources/severities and additional context are not suppressed.
The event is not mutated or removed from the logger;
DiskLogListener remains unpatched and receives it normally. Keep
`Logging.Disk.LogLevels = All` in BepInEx.cfg to retain Debug diagnostics.
The old global Unity filterLogType=Error assignment was removed because it could
suppress messages before any listener, including file logging, received them.

Research-point grants now carry presentation entries for their color. The native
sprite assets `techno_point_icon04`, `techno_point_icon03`, `techno_point_icon02`
match the red, green and blue symbols in technology prices and point totals.
These replace the round pickup sprites `i_r`, `i_g`, `i_b`. Rows retain the AP
item name/amount. See `ap-item-colors.md` for the current icon and tooltip styling.

Technology receipt rows retain the AP name: `Unlock: Smithing` no longer appears
as its internal root technology `Iron`. Empty technology icons fall back to the
first visible original native unlock with an icon, then the AP logo. This uses
GetUnlocksList, not the randomized GetVisibleUnlocksList, avoiding recursive AP
reward previews or an icon belonging to a different randomized reward.

The existing Zombie Creation/Workstations grants have empty technology ID lists.
Their notification now has an explicit AP-item entry with the AP logo. This is a
presentation fallback; the empty grant data and actual unlock behavior were not
changed. No native technology IDs or item/location identities were invented.

All receipt rows, including presentation failures, now have an AP-icon fallback.
Research/technology preview icons in the tree use the same resolver.

Miss Chain's historical grant names `npc_mrs_chain`, while the game's
ObjectDefinition ID and corresponding portrait name use a space: `npc_mrs chain`
and `char_npc_mrs chain`. The runtime's exact legacy-ID translation is shared by
relation application and presentation. It does not rename AP items, change the
shared manifest or require a new seed. It also leaves other NPCs and DLC variants
unchanged. Already-recorded historical receipts are not replayed by this change.

Validation: `dotnet test GraveyardKeepelago.sln --no-restore
-p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-console-validation/
--verbosity quiet` passed 308 tests, zero failures/skips. Output is in
`out/console-and-npc-tests.log`. Tests include packet severity, console-only
routing with full source events retained, real AP messages, mixed severities,
Miss Chain's canonical reward/portrait identity and unchanged NPC/DLC IDs.
The old routing assertion expected the raw grant ID; it now expects the native
ID because the legacy shared identifier is intentionally translated at runtime.
Existing compiler/analyzer warnings remain.

The tested build was installed into the local debug game and its hash verified.
Console LogLevels are Info, Message, Warning, Error, Fatal; Disk LogLevels remain
All. The installed client takes effect after restarting the game. The current
seed remains compatible. Interactive confirmation of Miss Chain's portrait and
the console filter is needed after that restart.

Additional exact graph/world-object filters were validated with:
`dotnet test GraveyardKeepelago.Tests/GraveyardKeepelago.Tests.csproj --no-restore
--filter 'FullyQualifiedName~ConsoleNoiseWhitelistTests'
-p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-console-validation/
--verbosity quiet`. All 62 focused tests passed; output is in
`out/console-filter-additions-tests.log`. Tests verify that both events remain
available to file logging and that other sources, severities and longer messages
remain visible on the console.
