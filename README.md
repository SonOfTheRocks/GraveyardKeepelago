# Graveyard Keepelago

Archipelago Client for Graveyard Keeper

The apworld can be found [here] (Not yet available)

## Setup
* Download the newest BepInEx 5.x x64 Release from their [releases page](https://github.com/BepInEx/BepInEx/releases)
* Extract the BepInEx folder to be located in the root folder of Graveyard Keeper
    * For example: `C:\Program Files (x86)\Steam\steamapps\common\Graveyard Keeper\`
* Start the game once for BepInEx to generate the necessary files
* Close the game again
* Download this mod from the [releases page](https://github.com/SonOfTheRocks/GraveyardKeepelago/releases)
* Put this mod (the .dll file) in the BepInEx/plugins folder
* Start the game once so BepInEx creates `BepInEx/config/SonOfTheRocks.GraveyardKeepelago.cfg`
* Configure `ServerHost`, `ServerPort`, `SlotName`, and the optional `Password` in the `[Archipelago]` section
* Load a save. If the connection fails after changing the config, press `F8` to reload it and retry without restarting the game

## Features

This mod can (depending on your chosen settings for the .apworld)


# Connecting to Archipelago

The Graveyard Keeper title screen includes an Archipelago logo button. Its logo is grey while disconnected
and colored while connected. It uses the native dialog-button frame and remains clickable while grey.
Select it to open the connection dialog, enter the server as `host:port`, the
slot name from the multiworld, and the optional room password, then select **Connect** before creating or
loading a save. The entered settings are saved in the BepInEx configuration and retained even when the
server is temporarily unavailable. DeathLink can be enabled in the same dialog.

If a connection made from the saved configuration fails while loading a game, correct the connection
details on the title screen and retry. `F8` remains available as a fallback retry shortcut.

The connection form uses the game's native modal dialog and NGUI input controls.
Its title uses the Options menu's native bitmap label and decorative title band.
Field captions clone the Options menu's bold bitmap labels at their native size and spacing.
All four controls share a compact two-column layout with captions on the left; the caption
column fits the longest label, and the native dialog height follows the row count.
Titles and field text use the native menu colors, outlines, and gradients; input boxes
reuse the selectors' native sliced background and opacity. DeathLink uses a matching two-choice
selector with native arrows and Enabled/Disabled text; click either arrow or use Left/Right while focused.
Connection changes are locked after loading a save. Debug builds provide `F9` to record the native input, dialog, toggle,
and mounted form hierarchies in `BepInEx/LogOutput.log`; field values and passwords are not logged.

The native connection menu was visually approved on 2026-10-25. For future UI changes, check mouse focus,
typing, selection, arrows, Home/End, Backspace/Delete, Tab navigation, password masking, the DeathLink
switch, invalid-address recovery, connect/disconnect, reopening, and blocked background menu clicks.
Repeat the layout/font checks at a second resolution or UI scale. Headless tests cover connection
decisions and retained form values, but cannot verify Unity rendering or mouse interaction.

