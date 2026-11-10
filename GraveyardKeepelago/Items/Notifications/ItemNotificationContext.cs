namespace GraveyardKeepelago.Items.Notifications;

public sealed class ItemNotificationContext
{
    public bool GameReady { get; set; }
    public bool PlayerControllable { get; set; }
    public bool WindowOpen { get; set; }
    public bool Talking { get; set; }
    public bool Cinematic { get; set; }
    public bool Busy { get; set; }

    public bool CanShow => GameReady && PlayerControllable &&
        !WindowOpen && !Talking && !Cinematic && !Busy;
}
