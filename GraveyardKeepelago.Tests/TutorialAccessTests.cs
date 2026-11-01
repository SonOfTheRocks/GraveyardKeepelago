using GraveyardKeepelago.GameModifications;
using GraveyardKeepelago.GameModifications.UI;

namespace GraveyardKeepelago.Tests;

public class TutorialAccessTests
{
    [Theory]
    [InlineData(true, true, "in_tutorial", true)]
    [InlineData(false, true, "in_tutorial", false)]
    [InlineData(true, false, "in_tutorial", false)]
    [InlineData(true, true, "first_autopsi", false)]
    [InlineData(true, true, "lock_tp", false)]
    public void RemoveTutorialRestrictionsWithoutFakingQuestProgress(bool active, bool player, string parameter, bool bypass)
        => Assert.Equal(bypass, TutorialAccessPolicy.IgnoreTutorialStatus(active, player, parameter));

    [Theory]
    [InlineData(true, "bed_no_sleep_1", "need_put_body", "sleep")]
    [InlineData(true, "bed_no_sleep_2", "need_to_check", "sleep")]
    [InlineData(false, "bed_no_sleep_1", "need_put_body", "need_put_body")]
    [InlineData(false, "bed_no_sleep_2", "need_to_check", "need_to_check")]
    [InlineData(true, "first_bed", "first_sleep", "first_sleep")]
    [InlineData(true, "bed", "sleep", "sleep")]
    [InlineData(true, "unrelated", "need_to_check", "need_to_check")]
    public void TutorialBedsAllowSleepWhileFirstSleepStoryRemainsAvailable(bool active, string id, string script, string expected)
        => Assert.Equal(expected, TutorialAccessPolicy.ResolveBedScript(active, id, script));

    [Theory]
    [InlineData(true, "Teleport", "key_to_cellar", true)]
    [InlineData(false, "Teleport", "key_to_cellar", false)]
    [InlineData(true, "on_enter_skull_back_zone", "key_to_cellar", false)]
    [InlineData(true, "Teleport", "morgue_hatch_open", false)]
    [InlineData(true, "Teleport", "cup_20_reached", false)]
    public void CellarDoorBypassPreservesIntroductionAndOtherQuestDoors(bool active, string graph, string parameter, bool open)
        => Assert.Equal(open, TutorialAccessPolicy.OpenCellarDoor(active, graph, parameter));

    [Fact]
    public void ReturningHomeFromSnakeSceneCanUseExitsBeforeTalkingToTheDonkey()
    {
        // The native tp_house trigger completes get_out_from_house on return
        // from the cellar, then lock_tps sets reason 0. Both the house door and
        // hatch enter Teleport's same global lock check before their own paths.
        var leftHome = false;
        var talkedToDonkey = false;
        Assert.False(TutorialAccessPolicy.BypassHomeExitLock(true, "Teleport", "lock_tp", 0, leftHome, talkedToDonkey));
        leftHome = true;
        Assert.True(TutorialAccessPolicy.BypassHomeExitLock(true, "Teleport", "lock_tp", 0, leftHome, talkedToDonkey));
        Assert.True(TutorialAccessPolicy.OpenCellarDoor(true, "Teleport", "key_to_cellar"));
        // Existing/reloaded saves use the persisted quest results too; there is
        // no one-shot initialization flag and no quest completion is fabricated.
        Assert.True(TutorialAccessPolicy.BypassHomeExitLock(true, "Teleport", "lock_tp", 0, true, false));
        talkedToDonkey = true;
        Assert.False(TutorialAccessPolicy.BypassHomeExitLock(true, "Teleport", "lock_tp", 0, leftHome, talkedToDonkey));
    }

    [Theory]
    [InlineData(false, "Teleport", "lock_tp", 0f, true, false)]
    [InlineData(true, "npc_bishop", "lock_tp", 0f, true, false)]
    [InlineData(true, "Teleport", "lock_tp_param", 0f, true, false)]
    [InlineData(true, "Teleport", "key_to_cellar", 0f, true, false)]
    [InlineData(true, "Teleport", "lock_tp", 1f, true, false)]
    [InlineData(true, "Teleport", "lock_tp", 2f, true, false)]
    [InlineData(true, "Teleport", "lock_tp", 0f, false, false)]
    [InlineData(true, "Teleport", "lock_tp", 0f, true, true)]
    public void HomeExitFixPreservesOtherGraphReadsAndLaterStoryLocks(bool active, string graph,
        string parameter, float reason, bool leftHome, bool donkey)
        => Assert.False(TutorialAccessPolicy.BypassHomeExitLock(active, graph, parameter, reason, leftHome, donkey));

    [Fact]
    public void ConnectionAppearsBetweenContinueAndOptions()
    {
        var actions = new List<string> { "OnPressedContinue", "OnPressedOptions", "OnPressedSaveAndExit" };
        actions.Insert(PauseMenuPlacement.ConnectionIndex(actions), "Connection");
        Assert.Equal(new[] { "OnPressedContinue", "Connection", "OnPressedOptions", "OnPressedSaveAndExit" }, actions);
    }

    [Fact]
    public void UnsupportedMenusDoNotReceiveAnEntryAtAnArbitraryPosition()
    {
        Assert.Throws<InvalidOperationException>(() => PauseMenuPlacement.ConnectionIndex(new[] { "OnPressedOptions" }));
        Assert.Throws<InvalidOperationException>(() => PauseMenuPlacement.ConnectionIndex(new[] { "OnPressedOptions", "OnPressedContinue" }));
    }
}
