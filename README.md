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

## Building from source

Run `git submodule update --init` before building. The client embeds the shared data in its DLL.
This build includes the v9 data for development and validation. Slots declaring a shared data version require a client with matching gameplay support.
Saved v9 monster traps now have a game-thread execution path; versioned logins remain blocked pending complete runtime support and in-game validation. See [saved monster trap behavior](docs/saved-monster-traps.md).
Portal and Graveyard goal completion use native save state and safe save binding. A zero Graveyard target requires actual quality at least zero. See [goal completion](docs/goal-completion.md).
The six world reward checks use ground-collection and chest-transfer hooks with persistent protection for received AP items. See [world reward checks](docs/world-reward-checks.md).

## Features

This mod can (depending on your chosen settings for the .apworld)


