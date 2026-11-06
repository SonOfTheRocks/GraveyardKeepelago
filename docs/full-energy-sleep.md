# Sleeping at full energy

In all saves, the player can enter bed even when energy and sanity are full.
No Archipelago connection or AP metadata is required. The patch is installed
once during mod startup alongside intro skipping, rather than during AP save
initialization. The patch changes the
entry decision only: no energy or sanity is deducted, and the native recovery,
manual waking, automatic waking and save-on-wake callbacks remain intact.
At full energy and health, the native automatic wake occurs after entry.

## Native evidence

The exported `SleepGUI.Open` and installed game's `Assembly-CSharp.dll` agree:
energy.EqualsTo(max_energy) followed by sanity.EqualsTo(max_sanity) invokes
on_doesnt_need_sleep and returns. The installed IL calls
`ExtentionTools.EqualsTo(float, float, float)` at IL_004d and IL_0078; both
results branch to IL_0086 (normal opening) when false. `FullEnergySleepPatch`
discards the first comparison's result and substitutes false for every save.
The original comparison and player stat reads remain intact. A different
comparison count fails patch installation explicitly instead of guessing.

`SleepGUI.Update` still wakes when energy and health are full or the interaction
key is pressed. `WakeUp` still performs the native save and quest callbacks.
This is a client-only change; item/location IDs, slot data and seeds are unchanged.

## Validation, 2026-10-30

```powershell
dotnet test GraveyardKeepelago.sln --no-restore -p:PluginsPath=C:/Users/Kai/RiderProjects/GraveyardKeepelago/out/codex-full-energy-sleep/ --verbosity quiet
```

Result after the user's scope correction: **669 passed**, zero failures/skips.
The original eight AP-specific policy cases were replaced because their vanilla
restriction contradicted the user's requirement. Seven tests now execute the
rewritten instruction stream for full/partial energy and sanity, check the
original guard's behavior for comparison, and reject unexpected native layouts.
Existing compiler/analyzer warnings remain. Native DLL IL was inspected read-only
with Mono.Cecil to confirm the exact patched branch. A pre-existing trailing
blank line at the end of README.md is reported by `git diff --check`.

The tested build is staged in the output folder above, not installed into the
running game. Manual Unity verification remains: full-energy bed entry in a
non-AP save without ever connecting, the same in an AP save, native save-on-wake,
low-energy recovery/manual waking, and intro skipping in both modes. The unit
tests execute generated IL without running Unity or installing Harmony patches.
