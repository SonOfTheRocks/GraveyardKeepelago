namespace GraveyardKeepelago.GameModifications;

public static class MorgueAccessPolicy
{
    // crafting_skull_3 sets these after the first autopsy conversation; Teleport reads
    // them until on_after_first_bureal clears them. AP can withhold the flesh recipe.
    public static bool ShouldBypassFlag(bool apActive, bool isPlayer, string parameter,
        float teleportLockReason = 0) =>
        apActive && isPlayer &&
        (parameter == "tp_from_morgue_only_with_body" || parameter == "tp_to_morgue_locked" ||
         (parameter == "lock_tp" && teleportLockReason == 1f));
}
