using GraveyardKeepelago.Items.Traps;
using GraveyardKeepelago.Progression;
using GraveyardKeepelago.Tests.TestData;

namespace GraveyardKeepelago.Tests;

public class TemporaryTrapMonstersTests
{
    private static TrapMonsterContext Gameplay() => new()
    {
        Save = new object(), World = new object(), Player = new object(), Area = new object(),
        PlayerAlive = true, WorldReady = true, PlayerControllable = true,
    };

    private sealed class Monster(long id) : ITemporaryTrapMonster
    {
        public long ID => id;
        public bool Exists { get; set; } = true;
        public bool Suspended { get; private set; }
        public int Removed { get; private set; }
        public bool FailRemoval { get; set; }
        public void Suspend(bool suspended) => Suspended = suspended;
        public void Remove()
        {
            if (FailRemoval) throw new IOException("Native removal failed");
            Exists = false; Removed++;
        }
    }

    [Theory]
    [InlineData("talking")]
    [InlineData("cinematic")]
    [InlineData("window")]
    [InlineData("control")]
    [InlineData("loading")]
    public void AlreadySpawnedMonstersPauseAndCannotDealOrReceiveDamage(string reason)
    {
        var context = Gameplay();
        var monster = new Monster(10);
        var session = new TemporaryTrapMonsters();
        session.Add(monster, context, 0);
        Assert.False(monster.Suspended);
        switch (reason)
        {
            case "talking": context.Talking = true; break;
            case "cinematic": context.Cinematic = true; break;
            case "window": context.WindowOpen = true; break;
            case "control": context.PlayerControllable = false; break;
            case "loading": context.WorldReady = false; break;
        }
        session.Update(context, 1);
        Assert.True(monster.Suspended);
        Assert.True(session.BlocksCombat(10, 1));
        Assert.True(session.BlocksCombat(1, 10));
        Assert.False(session.BlocksCombat(2, 1)); // Ordinary enemies are untouched.
        Assert.Equal(0, monster.Removed);
    }

    [Fact]
    public void GraceStartsWhenControlReturnsAndDoesNotExtendOnEachSafeFrame()
    {
        var context = Gameplay();
        var monster = new Monster(10);
        var session = new TemporaryTrapMonsters();
        session.Add(monster, context, 0);
        context.Talking = true;
        session.Update(context, 1);
        session.Update(context, 100);
        context.Talking = false;
        session.Update(context, 200);
        session.Update(context, 201.9f);
        Assert.True(monster.Suspended);
        session.Update(context, 202);
        Assert.False(monster.Suspended);
        Assert.False(session.BlocksCombat(10, 1));
        Assert.False(session.BlocksCombat(1, 10));
    }

    [Fact]
    public void SpawnDuringAnUnsafeTransitionIsImmediatelyProtected()
    {
        var context = Gameplay();
        context.Talking = true;
        var monster = new Monster(10);
        var session = new TemporaryTrapMonsters();
        session.Add(monster, context, 0);
        Assert.True(monster.Suspended);
        Assert.True(session.BlocksCombat(10, 1));
        Assert.False(session.CanSerialize(10));
    }

    [Fact]
    public void ASpawnThatArrivesDuringDeathIsImmediatelyDismissed()
    {
        var context = Gameplay();
        context.PlayerAlive = false;
        var monster = new Monster(10);
        var session = new TemporaryTrapMonsters();
        session.Add(monster, context, 0);
        Assert.Equal(1, monster.Removed);
        Assert.Equal(0, session.Count);
    }

    [Fact]
    public void AnotherDialogDuringGraceStartsANewFullGracePeriod()
    {
        var context = Gameplay();
        var monster = new Monster(10);
        var session = new TemporaryTrapMonsters();
        session.Add(monster, context, 0);
        context.Talking = true; session.Update(context, 1);
        context.Talking = false; session.Update(context, 2);
        context.Cinematic = true; session.Update(context, 3);
        context.Cinematic = false; session.Update(context, 4);
        session.Update(context, 5.9f);
        Assert.True(monster.Suspended);
        session.Update(context, 6);
        Assert.False(monster.Suspended);
    }

    [Theory]
    [InlineData("death")]
    [InlineData("area")]
    [InlineData("player")]
    [InlineData("save")]
    [InlineData("world")]
    [InlineData("unload")]
    public void DeathOrWorldContextChangeDismissesTheWholeActiveGroup(string change)
    {
        var context = Gameplay();
        var monsters = new[] { new Monster(10), new Monster(11), new Monster(12), new Monster(13) };
        var session = new TemporaryTrapMonsters();
        foreach (var monster in monsters) session.Add(monster, context, 0);
        switch (change)
        {
            case "death": context.PlayerAlive = false; break;
            case "area": context.Area = new object(); break;
            case "player": context.Player = new object(); break;
            case "save": context.Save = new object(); break;
            case "world": context.World = new object(); break;
            case "unload": context.World = null; break;
        }
        session.Update(context, 1);
        session.Clear();
        Assert.All(monsters, monster => Assert.Equal(1, monster.Removed));
        Assert.Equal(0, session.Count);
        Assert.False(session.Suspended);
    }

    [Fact]
    public void SnapshotExcludesOnlyOwnedMonstersIncludingWhileSuspended()
    {
        var context = Gameplay();
        var session = new TemporaryTrapMonsters();
        session.Add(new Monster(10), context, 0);
        session.Add(new Monster(11), context, 0);
        Assert.Equal(new long[] { 1, 12 }, new long[] { 1, 10, 11, 12 }.Where(session.CanSerialize));
        context.Talking = true; session.Update(context, 1);
        Assert.Equal(new long[] { 1, 12 }, new long[] { 1, 10, 11, 12 }.Where(session.CanSerialize));
        Assert.Equal(2, session.Count); // Saving does not end an active trap.
    }

    [Fact]
    public void DestroyedMonstersAreForgottenWithoutLeavingAShieldForTheNextGroup()
    {
        var context = Gameplay();
        var monster = new Monster(10);
        var session = new TemporaryTrapMonsters();
        session.Add(monster, context, 0);
        context.Talking = true; session.Update(context, 1);
        monster.Exists = false; session.Update(context, 2);
        Assert.Equal(0, session.Count);
        context.Talking = false;
        var next = new Monster(11);
        session.Add(next, context, 3);
        Assert.False(next.Suspended);
    }

    [Fact]
    public void ExplicitDismissalForTeleportIsIdempotentAndLeavesNoGraceInTheNextArea()
    {
        var context = Gameplay();
        var monster = new Monster(10);
        var session = new TemporaryTrapMonsters();
        session.Add(monster, context, 0);
        session.Add(monster, context, 0); // Accidental duplicate registration.
        context.Talking = true; session.Update(context, 1);
        session.Clear(); session.Clear();
        Assert.Equal(1, monster.Removed);
        context = Gameplay();
        var next = new Monster(11);
        session.Add(next, context, 2);
        Assert.False(next.Suspended);
    }

    [Fact]
    public void FailedDismissalStaysOutOfTheSaveAndDoesNotPreventOtherMonstersBeingRemoved()
    {
        var failures = new List<Exception>();
        var context = Gameplay();
        var bad = new Monster(10) { FailRemoval = true };
        var good = new Monster(11);
        var session = new TemporaryTrapMonsters(failures.Add);
        session.Add(bad, context, 0); session.Add(good, context, 0);
        session.Clear();
        Assert.Single(failures);
        Assert.Equal(1, good.Removed);
        Assert.False(session.CanSerialize(10));
        bad.FailRemoval = false; session.Clear();
        Assert.Equal(0, session.Count);
    }

    [Fact]
    public void DismissingAnActivatedTrapDoesNotRequeueItAndPreservesPendingTrapsAcrossReload()
    {
        var progress = new SaveProgress { Seed = "Seed", Team = 0, Slot = 1 };
        var records = new List<string>();
        var session = new TemporaryTrapMonsters();
        var context = Gameplay();
        var now = 0f;
        var executions = 0;
        var queue = new TrapManager(new FakeLogger(), progress, () => progress.Store(records),
            _ => { executions++; session.Add(new Monster(10), context, now); }, () => true, () => now);
        queue.Enqueue("bats"); queue.Enqueue("green_slimes");
        now = 10; queue.DequeueTrap();
        Assert.Equal(1, executions);
        Assert.Equal(1, session.Count);
        Assert.False(session.CanSerialize(10));
        session.Clear(); // Death, teleport, quit or reload.
        var restored = SaveProgress.Load(records, "Seed", 0, 1);
        Assert.Equal(new[] { "green_slimes" }, restored.PendingTraps);
        Assert.Equal(0, session.Count);
        Assert.Equal(1, executions);
    }
}
