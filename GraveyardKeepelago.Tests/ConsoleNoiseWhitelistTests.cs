using BepInEx.Logging;
using GraveyardKeepelago.GameModifications.Patches;
using GraveyardKeepelago.Utilities;
using HarmonyLib;

namespace GraveyardKeepelago.Tests;

public class ConsoleNoiseWhitelistTests
{
    private const string Message = "Cannot use 'MovePosition' on a static body.";
    private const string ActiveGraph = "<b>(NodeCanvas Warning)</b>: Graph is already Active.";
    private const string MissingWorldObject = "World Object not found";

    [Theory]
    [InlineData(LogLevel.Warning, ActiveGraph)]
    [InlineData(LogLevel.Warning, ConsoleNoiseWhitelist.NativeInventoryLayerDiagnostic)]
    [InlineData(LogLevel.Warning, ConsoleNoiseWhitelist.NativeInventoryLayerDiagnostic + "\n  at UIWidget.CheckLayer()")]
    [InlineData(LogLevel.Warning, ConsoleNoiseWhitelist.NativeLayerRepairWarning)]
    [InlineData(LogLevel.Error, MissingWorldObject)]
    [InlineData(LogLevel.Warning, "Error adding sprite to a library - duplicate name: 700_work_bot_up_01")]
    [InlineData(LogLevel.Warning, "Error adding sprite to a library - duplicate name: 312_hed_down")]
    [InlineData(LogLevel.Warning, "Error loading asset \"FlowCanvas/WGO Scripts/priest_test\"")]
    [InlineData(LogLevel.Warning, "Error loading asset \"objects\\crafting_skull_3\"")]
    [InlineData(LogLevel.Error, "Can't deserialize WGO with an empty id.")]
    [InlineData(LogLevel.Error, "Could't find zone [refugees_camp]")]
    [InlineData(LogLevel.Error, "Couldn't find zone [another_zone]")]
    public void RequestedUnityNoiseIsFilteredFromConsoleWithoutDroppingTheLogEvent(LogLevel level, string message)
    {
        using var source = new ManualLogSource("Unity Log");
        var logEvents = new List<LogEventArgs>();
        var consoleEvents = new List<LogEventArgs>();
        source.LogEvent += (_, args) =>
        {
            logEvents.Add(args);
            if (!ConsoleNoiseWhitelist.Suppress(args.Source.SourceName, args.Level, args.Data))
                consoleEvents.Add(args);
        };
        source.Log(level, message);
        Assert.Equal(message, Assert.Single(logEvents).Data);
        Assert.Empty(consoleEvents);
    }

    [Theory]
    [InlineData("GraveyardKeepelago", LogLevel.Warning, ActiveGraph)]
    [InlineData("NodeCanvas", LogLevel.Warning, ActiveGraph)]
    [InlineData("Unity Log", LogLevel.Error, ActiveGraph)]
    [InlineData("Unity Log", LogLevel.Fatal, ActiveGraph)]
    [InlineData("Unity Log", LogLevel.Warning, ActiveGraph + " Extra context")]
    [InlineData("Unity Log", LogLevel.Warning, "Graph is already Active.")]
    [InlineData("GraveyardKeepelago", LogLevel.Error, MissingWorldObject)]
    [InlineData("Unity Log", LogLevel.Warning, MissingWorldObject)]
    [InlineData("Unity Log", LogLevel.Fatal, MissingWorldObject)]
    [InlineData("Unity Log", LogLevel.Error, MissingWorldObject + ": npc")]
    [InlineData("Unity Log", LogLevel.Error | LogLevel.Warning, MissingWorldObject)]
    [InlineData("GraveyardKeepelago", LogLevel.Warning, "Error loading asset \"asset\"")]
    [InlineData("Unity Log", LogLevel.Error, "Error loading asset \"asset\"")]
    [InlineData("Unity Log", LogLevel.Warning, "Error loading asset \"asset\" Extra context")]
    [InlineData("Unity Log", LogLevel.Warning, "Error loading asset \"\"")]
    [InlineData("Unity Log", LogLevel.Warning, "Error loading asset \"asset\"\nException details")]
    [InlineData("GraveyardKeepelago", LogLevel.Warning, "Error adding sprite to a library - duplicate name: sprite")]
    [InlineData("Unity Log", LogLevel.Error, "Error adding sprite to a library - duplicate name: sprite")]
    [InlineData("Unity Log", LogLevel.Warning, "Error adding sprite to a library - duplicate name: ")]
    [InlineData("Unity Log", LogLevel.Warning, "Error adding sprite to a library - duplicate name: sprite\nException details")]
    [InlineData("Unity Log", LogLevel.Error, "Can't deserialize WGO with an empty id. Extra context")]
    [InlineData("Unity Log", LogLevel.Fatal, "Can't deserialize WGO with an empty id.")]
    [InlineData("GraveyardKeepelago", LogLevel.Error, "Could't find zone [refugees_camp]")]
    [InlineData("Unity Log", LogLevel.Warning, "Could't find zone [refugees_camp]")]
    [InlineData("Unity Log", LogLevel.Error, "Could't find zone [refugees_camp] Extra context")]
    [InlineData("Unity Log", LogLevel.Error, "Could't find zone []")]
    [InlineData("Unity Log", LogLevel.Error, "Could't find zone [refugees_camp]\nException details")]
    public void OtherSourcesSeveritiesAndAdditionalContextAreStillVisible(string source, LogLevel level, string message)
        => Assert.False(ConsoleNoiseWhitelist.Suppress(source, level, message));

    [Fact]
    public void UnknownLayerMismatchesAndErrorsRemainVisible()
    {
        var known = ConsoleNoiseWhitelist.NativeInventoryLayerDiagnostic;
        Assert.False(ConsoleNoiseWhitelist.Suppress("Unity Log", LogLevel.Warning, known.Replace("Inventory GUI", "AP Journal")));
        Assert.False(ConsoleNoiseWhitelist.Suppress("Unity Log", LogLevel.Warning, known.Replace("layer 0", "layer 4")));
        Assert.False(ConsoleNoiseWhitelist.Suppress("Unity Log", LogLevel.Error, known));
        Assert.False(ConsoleNoiseWhitelist.Suppress("Another plugin", LogLevel.Warning, known));
        Assert.False(ConsoleNoiseWhitelist.Suppress("Unity Log", LogLevel.Warning, "[AP UI diagnostics] Unknown problem"));
    }

    [Fact]
    public void ExactUnityPhysicsWarningIsSuppressedOnlyAtConsoleListener()
    {
        Assert.True(ConsoleNoiseWhitelist.Suppress("Unity Log", LogLevel.Warning, Message));
        var patch = Assert.Single(typeof(ConsoleNoisePatch).GetCustomAttributes(typeof(HarmonyPatch), false).Cast<HarmonyPatch>());
        Assert.Equal(typeof(ConsoleLogListener), patch.info.declaringType);
        Assert.Equal(nameof(ConsoleLogListener.LogEvent), patch.info.methodName);
        Assert.NotEqual(typeof(DiskLogListener), patch.info.declaringType);
    }

    [Theory]
    [InlineData("GraveyardKeepelago", LogLevel.Warning, Message)]
    [InlineData("Unity Log", LogLevel.Error, Message)]
    [InlineData("Unity Log", LogLevel.Fatal, Message)]
    [InlineData("Unity Log", LogLevel.Warning, "Cannot use 'MovePosition' on a dynamic body.")]
    [InlineData("Unity Log", LogLevel.Warning, Message + " Extra context")]
    [InlineData("Unity Log", LogLevel.Warning, "Prefix: " + Message)]
    [InlineData("Unity Log", LogLevel.Warning, "World Object not found")]
    public void UnlistedMessagesLevelsAndSourcesRemainVisible(string source, LogLevel level, string message)
        => Assert.False(ConsoleNoiseWhitelist.Suppress(source, level, message));

    [Fact]
    public void NullOrNonStringPayloadIsNotSilenced()
    {
        Assert.False(ConsoleNoiseWhitelist.Suppress("Unity Log", LogLevel.Warning, null!));
        Assert.False(ConsoleNoiseWhitelist.Suppress("Unity Log", LogLevel.Warning, new object()));
    }

    [Theory]
    [InlineData("GraveyardKeepelago", LogLevel.Debug, "Packet received: RoomInfo")]
    [InlineData("GraveyardKeepelago", LogLevel.Debug, "Game gifted craft: recipe")]
    [InlineData("Unity Log", LogLevel.Info, "RedrawCurrentInteractiveHint")]
    [InlineData("Unity Log", LogLevel.Info, "Graveyard Keeper_Data")]
    [InlineData("Unity Log", LogLevel.Message, "GameGUI redraw")]
    [InlineData("Another plugin", LogLevel.Debug, "Internal diagnostic")]
    public void DiagnosticAndUnityChatterIsHiddenFromConsole(string source, LogLevel level, string message)
        => Assert.True(ConsoleNoiseWhitelist.Suppress(source, level, message));

    [Theory]
    [InlineData("GraveyardKeepelago", LogLevel.Info, "Received Item 'Miss Chain - Happiness +10'")]
    [InlineData("GraveyardKeepelago", LogLevel.Info, "Sending AP check: Give beer to Gerry")]
    [InlineData("GraveyardKeepelago", LogLevel.Info, "AP check saved offline: Test check")]
    [InlineData("GraveyardKeepelago", LogLevel.Info, "Message received: Friend joined")]
    [InlineData("GraveyardKeepelago", LogLevel.Message, "Connected to Archipelago")]
    [InlineData("Unity Log", LogLevel.Warning, "Real problem")]
    [InlineData("Unity Log", LogLevel.Error, "Sprite not found")]
    [InlineData("Unity Log", LogLevel.Fatal, "Cannot continue")]
    [InlineData("GraveyardKeepelago", LogLevel.Warning | LogLevel.Debug, "Keep warning with mixed flags")]
    [InlineData("Unity Log", LogLevel.Error | LogLevel.Info, "Keep error with mixed flags")]
    public void PlayerMessagesAndProblemsRemainVisible(string source, LogLevel level, string message)
        => Assert.False(ConsoleNoiseWhitelist.Suppress(source, level, message));

    [Fact]
    public void DebugMessagesStillReachTheLogSourceWhileTheConsoleFiltersThem()
    {
        using var source = new ManualLogSource("GraveyardKeepelago");
        var diskEvents = new List<LogEventArgs>();
        var consoleEvents = new List<LogEventArgs>();
        source.LogEvent += (_, args) =>
        {
            diskEvents.Add(args);
            if (!ConsoleNoiseWhitelist.Suppress(args.Source.SourceName, args.Level, args.Data))
                consoleEvents.Add(args);
        };
        var logger = new LogHandler(source);
        logger.LogDebug("Packet received: full packet");
        logger.LogInfo("Received Item 'Recipe'");
        logger.LogWarning("Connection interrupted");
        Assert.Equal(3, diskEvents.Count);
        Assert.Equal(LogLevel.Debug, diskEvents[0].Level);
        Assert.Equal(new[] { LogLevel.Info, LogLevel.Warning }, consoleEvents.Select(entry => entry.Level));
    }
}
