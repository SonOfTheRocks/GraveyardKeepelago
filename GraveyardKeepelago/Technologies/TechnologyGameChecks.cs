using System;
using System.Collections.Generic;
using System.Linq;
using GraveyardKeepelago.Locations;
using GraveyardKeepelago.Logic;

namespace GraveyardKeepelago.Technologies;

internal sealed class TechnologyGameChecks
{
    private readonly LocationTriggerService _locations;
    private readonly TechnologyCatalog _catalog;
    private readonly HashSet<string> _scripts;
    private readonly HashSet<string> _crafts = new(StringComparer.Ordinal);
    private readonly HashSet<string> _works = new(StringComparer.Ordinal);
    private readonly HashSet<string> _perks = new(StringComparer.Ordinal);

    public TechnologyGameChecks(LocationTriggerService locations, TechnologyCatalog catalog,
        IEnumerable<ClientContractItem> enabledItems, IEnumerable<string> technologyScripts)
    {
        _locations = locations;
        _catalog = catalog;
        _scripts = new HashSet<string>(technologyScripts, StringComparer.Ordinal);

        foreach (var item in enabledItems)
        {
            var grant = item.Grant;
            Add(_crafts, grant.CraftIDs);
            Add(_works, grant.WorkIDs);
            Add(_perks, grant.PerkIDs);
            if (grant.PerkID != null)
                _perks.Add(Canonical(grant.PerkID));
            if (grant.Stages != null)
            {
                var controlled = grant.Type == "progressive_craft_unlock" ? _crafts : _works;
                foreach (var stage in grant.Stages)
                    Add(controlled, stage);
            }
        }
    }

    public bool TryUnlock(string technologyID, bool purchased, Action complete, bool fromSharedGrant = false)
    {
        if (fromSharedGrant)
        {
            complete();
            return true;
        }

        if (!_locations.HandleTechnology(technologyID))
        {
            if (!_locations.HasTechnologyResearch(technologyID))
                return false;

            if (purchased && _catalog.Definitions[technologyID].RecipeItem == null)
                _locations.HandleTechnologyResearch(technologyID);
        }

        complete();
        return true;
    }

    public bool TryUseRecipeBook(string itemID, string technologyID, Action complete)
    {
        if (itemID == null || technologyID == null || !_catalog.Definitions.TryGetValue(technologyID, out var definition) ||
            definition.RecipeItem != itemID || !_locations.HasTechnologyResearch(technologyID))
            return false;

        // The catalog permits exactly one research location for a recipe book.
        _locations.HandleTechnologyResearch(technologyID);
        complete();
        return true;
    }

    public bool ShouldSuppressScriptCraft(string graph, string craftID)
        => _scripts.Contains(graph) && _crafts.Contains(Canonical(craftID));

    public void Complete(string technologyID, ICollection<string> researched, Action applyWorldEffects)
    {
        if (researched.Contains(technologyID))
            return;

        researched.Add(technologyID);
        applyWorldEffects();
    }

    public IEnumerable<string> GetCrafts(IEnumerable<string> ids) => Uncontrolled(ids, _crafts);
    public IEnumerable<string> GetWorks(IEnumerable<string> ids) => Uncontrolled(ids, _works);
    public IEnumerable<string> GetPerks(IEnumerable<string> ids) => Uncontrolled(ids, _perks);
    public static string Canonical(string id) => id?.TrimStart('@');

    private static IEnumerable<string> Uncontrolled(IEnumerable<string> ids, HashSet<string> controlled)
        => ids.Select(Canonical).Where(id => !string.IsNullOrEmpty(id) && !controlled.Contains(id));

    private static void Add(HashSet<string> target, IEnumerable<string> ids)
    {
        if (ids != null)
            target.UnionWith(ids.Select(Canonical));
    }
}