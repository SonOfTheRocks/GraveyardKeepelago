using System.Collections.Generic;
using GraveyardKeepelago.Items;

namespace GraveyardKeepelago.Logic;

public class GKItemRegistry
{
    private readonly Dictionary<string, IAPItem> _permanentBuffsByName;
    private readonly Dictionary<string, IAPItem> _recipesByName;
    private readonly Dictionary<string, GKPerk> _perksByName;
    private readonly Dictionary<string, GKRelation> _relationItemsByName;
    private readonly Dictionary<string, GKIngameItem> _ingameItemsByName;

    public GKItemRegistry()
    {
        _permanentBuffsByName = new Dictionary<string, IAPItem>();
        _recipesByName = new Dictionary<string, IAPItem>();
        _perksByName = new Dictionary<string, GKPerk>();
        _relationItemsByName = new Dictionary<string, GKRelation>();
        _ingameItemsByName = new Dictionary<string, GKIngameItem>();
    }

    public void AddBuff(string key, IAPItem item) => _permanentBuffsByName[key] = item;
    public IAPItem GetBuff(string key) => _permanentBuffsByName.TryGetValue(key, out var item) ? item : null;

    public void AddRecipe(string key, IAPItem item) => _recipesByName[key] = item;
    public IAPItem GetRecipe(string key) => _recipesByName.TryGetValue(key, out var item) ? item : null;

    public void AddPerk(string key, GKPerk perk) => _perksByName[key] = perk;
    public GKPerk GetPerk(string key) => _perksByName.TryGetValue(key, out var perk) ? perk : null;

    public void AddRelation(string key, GKRelation relation) => _relationItemsByName[key] = relation;
    public GKRelation GetRelation(string key) => _relationItemsByName.TryGetValue(key, out var rel) ? rel : null;

    public void AddIngameItem(string key, GKIngameItem item) => _ingameItemsByName[key] = item;
    public bool TryGetIngameItem(string key, out GKIngameItem item) => _ingameItemsByName.TryGetValue(key, out item);
}
