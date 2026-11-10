using System.Collections.Generic;
using GraveyardKeepelago.GameModifications;

namespace GraveyardKeepelago.Items;

public class GKResearchPoints : IAPItem
{
    private readonly string _color;
    private readonly int _amount;
    private readonly IPlayerActions _playerActions;

    public GKResearchPoints(string color, int amount, IPlayerActions playerActions)
    {
        _color = color;
        _amount = amount;
        _playerActions = playerActions;
    }

    public void Apply() => _playerActions.AddResearchPoints(_color, _amount);
}

public class GKTechnologyUnlock : IAPItem
{
    private readonly List<string> _technologyIDs;
    private readonly IPlayerActions _playerActions;

    public GKTechnologyUnlock(List<string> technologyIDs, IPlayerActions playerActions)
    {
        _technologyIDs = technologyIDs;
        _playerActions = playerActions;
    }

    public void Apply() => _playerActions.UnlockTechnologies(_technologyIDs);
}

public class GKFlowScript : IAPItem
{
    private readonly string _scriptID;
    private readonly IPlayerActions _playerActions;

    public GKFlowScript(string scriptID, IPlayerActions playerActions)
    {
        _scriptID = scriptID;
        _playerActions = playerActions;
    }

    public void Apply() => _playerActions.RunFlowScript(_scriptID);
}
