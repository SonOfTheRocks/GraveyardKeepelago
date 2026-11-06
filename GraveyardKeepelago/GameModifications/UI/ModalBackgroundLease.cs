using System;

namespace GraveyardKeepelago.GameModifications.UI;

public sealed class ModalBackgroundLease : IDisposable
{
    private Action _restore;
    public ModalBackgroundLease(Action hide, Action restore)
    {
        _restore = restore;
        hide();
    }

    public void Dispose()
    {
        var restore = _restore;
        _restore = null;
        restore?.Invoke();
    }
}
