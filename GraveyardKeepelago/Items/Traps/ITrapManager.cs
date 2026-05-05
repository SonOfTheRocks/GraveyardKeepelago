namespace GraveyardKeepelago.Items.Traps;

public interface ITrapManager
{
    bool IsTrap(string itemName);
    bool TryExecuteTrapImmediately(string trapName);
}
