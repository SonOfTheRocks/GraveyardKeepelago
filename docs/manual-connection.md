# Explicit connection before AP activation

Previously Plugin.EnterGame called ConnectToArchipelago with the saved config.
GlobalEventsCheck invokes EnterGame for both new and loaded saves, so merely
loading a save connected and consumed its item history without a menu Connect.

EnterGame now only initializes an already connected session authorized by a
successful Connection-menu action. ManualConnectionSession holds that approval
in memory, never in config/save data. Disconnect and initialization failure clear
it. Stored credentials only prefill the menu. The parameterless auto-connect
path was removed. F8 can retry an existing initialized session, but no longer
reloads credentials or creates a new session while disconnected/uninitialized.
An explicit Connect from the pause menu activates the current save immediately,
including existing identity/history validation. An existing manual connection
can still be used when loading another matching save.

Validation: `dotnet test GraveyardKeepelago.sln --no-restore
-p:PluginsPath=bin/checked-client/` — 219 passed, zero failures/skips. The new
regressions cover save creation/loading without menu approval (even if a socket
exists), explicit connection, disconnect then load, and disconnected/reconnected
sessions. Existing compiler/analyzer warnings remain.

Interactive check: restart, create/load without Connect, confirm no AP connection
or receipts; then open pause-menu Connection and explicitly Connect. Also test
Connect/Disconnect in the main menu before loading, and F8 before any Connect.
