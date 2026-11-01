# Troubleshooting

[Back to the README](../README.md)

## The Archipelago button is missing

Check that BepInEx 5.x x64 is installed beside the game executable. Launch once after
installing BepInEx, close the game, and check that it created `BepInEx/plugins`.
Place the standalone `GraveyardKeepelago.dll` in `BepInEx/plugins/GraveyardKeepelago/` and restart the game. No separate client libraries or game-data files are required.

## Connection fails

Check the server address (`host:port`), slot name and optional room password against
the multiworld's details. Use the slot name, rather than the game character's name.
Saved details only prefill the form: select **Connect** to establish the connection.
Use matching client/APWorld versions and install any DLC enabled in the seed.

After a connection loss, open **Connection** in the pause menu and select **Reconnect**. To change connection
details, return to the title screen and open the Archipelago connection form.

## A save is unavailable

Read its tooltip. AP saves require a connection to their original multiworld and slot.
Normal saves cannot be loaded while connected to AP and cannot be converted into AP saves;
disconnect to play them normally. A first AP session needs a new game started while connected.
If the client rejects an older seed, use a compatible older client that also supports the save, or start a new seed and save with the newer version.

## Reporting a problem

Describe what happened, what you expected, and how to reproduce it. Include your client
and APWorld versions, installed DLC, and relevant messages from `BepInEx/LogOutput.log`.
For layout problems, include a screenshot and your resolution/UI scale.
Review logs before sharing them and remove passwords or other private information.



