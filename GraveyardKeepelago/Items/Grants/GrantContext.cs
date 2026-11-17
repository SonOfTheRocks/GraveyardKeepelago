using System;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Items.Grants;

internal sealed class GrantContext
{
    private readonly GameSave _save;

    public Func<string, bool> AcceptTrap { get; }
    public ILogger Logger { get; }

    public GrantContext(GameSave save, Func<string, bool> acceptTrap, ILogger logger)
    {
        _save = save ?? throw new ArgumentNullException(nameof(save));
        AcceptTrap = acceptTrap;
        Logger = logger;
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
