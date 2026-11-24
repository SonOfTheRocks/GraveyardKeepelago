using GraveyardKeepelago.Items.Traps;

namespace GraveyardKeepelago.Tests;

public class MonsterSpawnPositionsTests
{
    [Fact]
    public void GroupSurroundsThePlayerAtOneTileDistance()
    {
        var positions = MonsterSpawnPositions.Find(1234, -5678, 4, (_, _) => false);
        Assert.Equal(4, positions.Distinct().Count());
        foreach (var (x, y) in positions)
            Assert.InRange(Math.Sqrt(Math.Pow(x - 1234, 2) + Math.Pow(y + 5678, 2)), 95.99, 96.01);
        Assert.Contains(positions, p => p.X > 1234 && Math.Abs(p.Y + 5678) < 0.01);
        Assert.Contains(positions, p => p.X < 1234 && Math.Abs(p.Y + 5678) < 0.01);
        Assert.Contains(positions, p => p.Y > -5678 && Math.Abs(p.X - 1234) < 0.01);
        Assert.Contains(positions, p => p.Y < -5678 && Math.Abs(p.X - 1234) < 0.01);
    }

    [Fact]
    public void BlockedDirectionsUseDistinctAlternativesEvenAfterWrapping()
    {
        // Only the four diagonals are clear. Every cardinal start must search.
        var positions = MonsterSpawnPositions.Find(0, 0, 4, (x, y) => Math.Abs(x) < 1 || Math.Abs(y) < 1);
        Assert.Equal(4, positions.Distinct().Count());
        Assert.All(positions, p => Assert.True(Math.Abs(p.X) > 1 && Math.Abs(p.Y) > 1));
    }

    [Fact]
    public void AGroupIsRejectedWhenOnlyThreeOfEightDirectionsAreClear()
        => Assert.Throws<InvalidOperationException>(() =>
            MonsterSpawnPositions.Find(0, 0, 4, (x, y) => x < 1));

    [Fact]
    public void FullyBlockedSurroundingsFailClearly()
        => Assert.Throws<InvalidOperationException>(() => MonsterSpawnPositions.Find(0, 0, 4, (_, _) => true));
}
