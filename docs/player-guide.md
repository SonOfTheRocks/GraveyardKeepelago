# Player guide

[Back to the README](../README.md)

## Installation

You need Graveyard Keeper on Windows, BepInEx 5.x x64, and a client compatible with the
[Graveyard Keeper APWorld](https://github.com/SonOfTheRocks/GraveyardKeeper_APWorld) used to generate your seed.
Install any DLC enabled in the seed. Your multiworld host supplies the server address,
slot name and optional room password.

1. Download [BepInEx 5.x x64](https://github.com/BepInEx/BepInEx/releases) and extract it into the game's root folder, beside its executable.
2. Launch the game once, then close it.
3. Download [GraveyardKeepelago.dll](https://github.com/SonOfTheRocks/GraveyardKeepelago/releases) and place it in `BepInEx/plugins/GraveyardKeepelago/`. The DLL includes the client libraries and game data. To update, replace it with the compatible new DLL.
4. Launch the game and open the **Archipelago logo** on the title screen. Enter `host:port`, your slot name and optional password, then select **Connect**.
5. Start a **new game** for your first AP session.

## Connecting and saves

Each time you launch Graveyard Keeper, connect to your multiworld before loading its
AP save. Your connection details are remembered. To play a normal save, disconnect first;
normal saves cannot be converted into AP saves. Unavailable saves have a tooltip explaining why.

The pause menu's **Connection** entry shows the connection form. Settings are locked while
a save is loaded. If the connection drops, a notification appears on screen. Open **Connection** in the pause menu and select **Reconnect**.
Completed checks are retained and sent when the connection returns.

Keep the client version that works with your seed. If a newer client rejects it, you can
continue with a compatible older client, provided it also supports your save. A new seed
and save are only needed when you choose to move to an incompatible version.

## Research and rewards

Research still costs points and follows technology prerequisites. In an AP game, researching
a technology gives the randomized rewards shown in its preview, rather than its usual
recipes and unlocks. These rewards may belong to you or another player. To unlock a
randomized recipe or ability in your own game, receive the corresponding item from the
multiworld. Unlocks outside the seed's randomized pool retain their normal behavior.

Received items appear briefly at the screen's right edge. You can review item transfers
later in the Archipelago journal.

## Optional monster traps

If enabled in the seed, trap items can spawn enemies near you. These enemies disappear
when you die, teleport or leave the area. Trap settings are chosen when generating the seed.

## Archipelago journal

The **Archipelago** tab in the player menu contains chat, item transfers, hints and
server messages. Its history lets you look up earlier messages and received items.

