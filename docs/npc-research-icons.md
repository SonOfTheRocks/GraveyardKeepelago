# NPC placement in research tiles

Relation-reward previews in the compact technology tree now move the plain NPC
sprite downward by half of the previous rounded one-fifth-height offset
(2.5 pixels for 25px icons, 3 for 32px, 4 for 40px, 5 for 48px), as requested
after the in-game test. Odd previous offsets retain their exact half-pixel value.
Icon size is preserved.
The tile, labels, colliders and any child frame/overlay keep their positions.
Receipt-dialog portraits continue to use their existing native light and pedestal.

The outer `TechTreeGUIUnlockItem.Draw(TechUnlock, bool)` patch only applies to
scouted local relation rewards under a `TechTreeGUIItem`. Missing NPC sprites
retain the fallback icon. It restores the captured positions before redraws and
row reuse, so offsets cannot accumulate or affect subsequent non-NPC rewards.
Session cleanup restores the original positions before the patches are removed.

Validation from the client checkout, using the SDK from global.json:

```powershell
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=bin/checked-client/
```

Result: 256 passed, zero failures/skips. Tests cover halved downward
placement, repeated redraws, reuse for other items, dialog exclusion and invalid
sizes. Existing compiler/analyzer warnings remain. Log:
`GraveyardKeepelago.Tests/bin/notification-validation/npc-tree-placement-tests.log`.

This is a client presentation change; shared data, IDs and server state do not
change. The installed DLL takes effect after restarting the game. Interactive
visual confirmation is still needed from the user.
