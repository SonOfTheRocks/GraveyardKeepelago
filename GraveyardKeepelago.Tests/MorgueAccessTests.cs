using GraveyardKeepelago.GameModifications;

namespace GraveyardKeepelago.Tests;

public class MorgueAccessTests
{
    [Fact]
    public void FleshExtractionLockDoesNotBlockExitBeforeMorgueDoorChecks()
    {
        Assert.True(MorgueAccessPolicy.ShouldBypassFlag(true, true, "lock_tp", 1f));
        Assert.False(MorgueAccessPolicy.ShouldBypassFlag(true, true, "lock_tp_param", 1f));
    }

    [Theory]
    [InlineData(true, true, 0f)] // Bishop and ordinary cinematic locks.
    [InlineData(true, true, 2f)]
    [InlineData(true, true, 1.1f)]
    [InlineData(false, true, 1f)]
    [InlineData(true, false, 1f)]
    public void OtherTeleportLockReasonsAndNonApPlayersStayBlocked(bool active, bool player, float reason)
    {
        Assert.False(MorgueAccessPolicy.ShouldBypassFlag(active, player, "lock_tp", reason));
    }

    [Theory]
    [InlineData("tp_from_morgue_only_with_body")]
    [InlineData("tp_to_morgue_locked")]
    public void ApPlayerCanLeaveAndReenterMorgueBeforeFirstBurial(string flag)
    {
        Assert.True(MorgueAccessPolicy.ShouldBypassFlag(true, true, flag));
    }

    [Theory]
    [InlineData(false, true, "tp_from_morgue_only_with_body")]
    [InlineData(false, true, "tp_to_morgue_locked")]
    [InlineData(true, false, "tp_from_morgue_only_with_body")]
    [InlineData(true, false, "tp_to_morgue_locked")]
    [InlineData(true, true, "cup_20_reached")]
    [InlineData(true, true, "first_autopsi")]
    [InlineData(true, true, "tp_locked")]
    [InlineData(true, true, "")]
    [InlineData(true, true, null)]
    public void OtherLocksObjectsAndVanillaKeepTheirOriginalBehavior(bool active, bool player, string? flag)
    {
        Assert.False(MorgueAccessPolicy.ShouldBypassFlag(active, player, flag));
    }
}
