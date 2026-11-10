namespace GraveyardKeepelago.GameModifications;

public static class TutorialAccessPolicy
{
    // Keep the saved tutorial/quest state, but remove its interaction restrictions.
    public static bool IgnoreTutorialStatus(bool apActive, bool isPlayer, string parameter) =>
        apActive && isPlayer && parameter == "in_tutorial";

    public static string ResolveBedScript(bool apActive, string objectID, string script) =>
        apActive && ((objectID == "bed_no_sleep_1" && script == "need_put_body") ||
                     (objectID == "bed_no_sleep_2" && script == "need_to_check"))
            ? "sleep" : script;

    // Only bypass the door check. The same key flag controls Gerry's separate
    // cellar introduction and rewards, which must still run normally.
    public static bool OpenCellarDoor(bool apActive, string graphName, string parameter) =>
        apActive && graphName == "Teleport" && parameter == "key_to_cellar";

    // Returning from the cellar fires get_out_from_house too. Its lock_tps
    // success script blocks every exit until talking to the donkey outside.
    // Scope the bypass to that saved tutorial window and Teleport's read; reason
    // 0 is also used by later story scenes, which must keep their normal locks.
    public static bool BypassHomeExitLock(bool apActive, string graphName, string parameter,
        float lockReason, bool leftHome, bool talkedToDonkey) =>
        apActive && graphName == "Teleport" && parameter == "lock_tp" &&
        lockReason == 0f && leftHome && !talkedToDonkey;
}
