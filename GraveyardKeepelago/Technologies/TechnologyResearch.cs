using System;
using System.Collections.Generic;

namespace GraveyardKeepelago.Technologies;

public sealed class TechnologyResearch
{
    private readonly TechnologyCatalog _catalog;
    private readonly ISet<string> _activeLocations;
    private readonly ICollection<string> _researched;
    private readonly Action<string> _worldEffects;
    private readonly Action<string[]> _check;
    public TechnologyResearch(TechnologyCatalog catalog, ISet<string> activeLocations,
        ICollection<string> researched, Action<string> worldEffects, Action<string[]> check)
    {
        _catalog = catalog; _activeLocations = activeLocations; _researched = researched;
        _worldEffects = worldEffects; _check = check;
    }
    public bool Research(string id)
    {
        if (id == null || !_catalog.Definitions.ContainsKey(id)) return false;
        var checks = _catalog.GetLocations(id, _activeLocations);
        if (checks.Length == 0) return false;
        Complete(id);
        _check(checks);
        return true;
    }

    public bool Complete(string id)
    {
        if (id == null || !_catalog.Definitions.ContainsKey(id)) return false;
        if (!_researched.Contains(id))
        {
            _researched.Add(id);
            _worldEffects(id);
        }
        return true;
    }
}
