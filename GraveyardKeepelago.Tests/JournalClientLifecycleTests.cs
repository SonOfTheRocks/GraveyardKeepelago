using System.Reflection;
using Archipelago.MultiClient.Net;
using GraveyardKeepelago.Archipelago;
using KaitoKid.ArchipelagoUtilities.Net.Client;
using KaitoKid.Utilities.Interfaces;
using Moq;

namespace GraveyardKeepelago.Tests;

public class JournalClientLifecycleTests
{
    [Fact]
    public void JournalInitializationUsesAuthenticatedSessionBeforeBaseMarksItConnected()
    {
        var cache = Path.Combine(AppContext.BaseDirectory, "BepInEx", "plugins", "GraveyardKeepelago", "IdTables");
        Directory.CreateDirectory(cache);
        foreach (var path in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "IdTables"), "*.json"))
            File.Copy(path, Path.Combine(cache, Path.GetFileName(path)), overwrite: true);
        var client = new InspectableClient(Mock.Of<ILogger>());
        // Construct helpers without opening a socket or logging in. This reproduces the
        // library's state when it invokes InitializeAfterConnection during a real login.
        var session = ArchipelagoSessionFactory.CreateSession("localhost", 38281);
        typeof(ArchipelagoClient).GetField("_session", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(client, session);
        Assert.False(client.IsConnected);
        client.InitializeAuthenticatedSession();
        Assert.True(client.IsConnected);
        Assert.Same(session, client.GetSession());
        client.DisconnectAndCleanup();
        Assert.False(client.IsConnected);
    }

    private sealed class InspectableClient : GKArchipelagoClient
    {
        public InspectableClient(ILogger logger) : base(logger, null!, () => { }) { }
        public void InitializeAuthenticatedSession() => InitializeAfterConnection();
        protected override void InitializeDeathLink() { } // No Harmony/Unity in lifecycle tests.
    }
}
