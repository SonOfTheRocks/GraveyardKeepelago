namespace GraveyardKeepelago.GameModifications.UI;

public static class JournalLifecyclePolicy
{
    public static bool UseGameCallbacks(bool gameStarted, bool playerAvailable, bool characterAvailable)
        => gameStarted && playerAvailable && characterAvailable;
}
