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
and colored while connected. Select it to open the connection dialog, enter the server as `host:port`, the
slot name from the multiworld, and the optional room password, then select **Connect** before creating or
loading a save. The entered settings are saved in the BepInEx configuration and retained even when the
server is temporarily unavailable. DeathLink can be enabled in the same dialog.

If a connection made from the saved configuration fails while loading a game, correct the connection
details on the title screen and retry. `F8` remains available as a fallback retry shortcut.

