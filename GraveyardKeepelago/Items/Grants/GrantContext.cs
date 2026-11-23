using System;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Items.Grants;

internal sealed class GrantContext
{
    private readonly GameSave _save;
    private readonly Action<string> _queueTrap;

    public ILogger Logger { get; }

    public GrantContext(GameSave save, ILogger logger, Action<string> queueTrap = null)
    {
        _save = save ?? throw new ArgumentNullException(nameof(save));
        Logger = logger;
        _queueTrap = queueTrap;
    }

    public GrantResult Apply(IItemGrant grant, Action<string> queueTrap)
    {
        var context = new GrantContext(_save, Logger, queueTrap);
        return grant.Apply(context);
    }

    public void QueueTrap(string trapID)
    {
        _queueTrap(trapID);
    }

    public GameSave Save
    {
        get
        {
            if (!ReferenceEquals(MainGame.me?.save, _save) || MainGame.me.player == null)
                throw new InvalidOperationException("Cannot grant an AP item outside its active save.");
            return _save;
        }
    }

    public WorldGameObject Player
    {
        get
        {
            _ = Save;
            return MainGame.me.player;
        }
    }
}
