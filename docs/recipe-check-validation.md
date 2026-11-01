# Recipe checks, shared data version 4 — validation

Validated on 2026-10-26.

From `C:/Users/Kai/PycharmProjects/Archipelago_GraveyardKeeper`:

```powershell
.\.venv\Scripts\python.exe ModuleUpdate.py --yes
.\.venv\Scripts\python.exe -m pytest worlds/graveyard_keeper/test -q -p no:cacheprovider
```

Setup succeeded. **7,687 tests and 6,056 subtests passed**, zero failures. Existing
warnings concern optional `_speedups` and deprecated `pkg_resources`.
Log: `output/recipe-check-suite.log`.

From `worlds/graveyard_keeper/data/shared`, using that repository's venv:

```powershell
C:/Users/Kai/PycharmProjects/Archipelago_GraveyardKeeper/.venv/Scripts/python.exe validate_data.py
C:/Users/Kai/PycharmProjects/Archipelago_GraveyardKeeper/.venv/Scripts/python.exe -m unittest discover -s tests -t .
```

Shared data valid: version 4, 486 item definitions, 286 authored reward locations.
**11 tests passed**. The changed shared files match byte-for-byte in both repos.

From `C:/Users/Kai/RiderProjects/GraveyardKeepelago` (uses global.json SDK):

```powershell
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=bin/checked-client/
```

**244 tests passed**, zero failures/skips. Existing compiler/analyzer warnings
remain. Log: `GraveyardKeepelago.Tests/bin/notification-validation/recipe-check-tests.log`.

Fresh generation from the APWorld root:

```powershell
.\.venv\Scripts\python.exe Generate.py --player_files_path players --outputpath output/gk_recipe_checks_20261026_data4 --multi 1 --spoiler 1
```

Succeeded with Player1, portal goal and all three DLC options enabled. Generated
`AP_98443308963503938137.zip`; its embedded multidata was independently inspected:

- 773 locations / 773 item copies, data version 4;
- 615 progression, 78 useful, 80 filler copies;
- no legacy technology-group items;
- 28 recipe books have one active check each; ordinary research retains three;
- the three mapped quest roots retain their quest reward, with nine duplicate
  research checks retired;
- surviving runtime locations retain their public addresses; the historical
  addresses of retired checks and all 25 group-item definitions remain reserved.

Native item-use expressions and parent IDs were read from unmodified game_data
with Unity serialization. The source list is in `evidence/recipe-item-sources.json`.
The temporary startup exporter was removed before the production build.

The code retains native researched state/world effects without granting
AP-controlled recipes, works or perks. Book preview/recovery lists match. Quest
root previews use the actual quest placement, while their research/recovery lists
remain empty. Preview reads use HintCreationPolicy.None.

The speed-potion instruction data entry has no established obtainable source;
none was supplied by the user. No location was invented. Generic repeatable
alchemy discovery scrolls and memory-potion alchemy instructions were not part of
the existing three-check technology pool; this change does not add checks for them.

The tested DLL and data were installed; their DLL hashes matched. Server startup
confirmed a new session without an existing save, at 127.0.0.1:38281. Universal
Tracker was launched from this development project's venv and connected as Player1.
The game loaded the plugin successfully. Interactive recipe use, quest flow and
visual rendering still need the user's in-game test with a new AP save.
