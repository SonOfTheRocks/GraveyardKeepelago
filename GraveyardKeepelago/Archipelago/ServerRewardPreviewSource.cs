using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using System;
using GraveyardKeepelago.Technologies;

namespace GraveyardKeepelago.Archipelago;

public sealed class ServerRewardPreviewSource : IRewardPreviewSource
{
    private readonly Func<ILocationCheckHelper> _locations;
    private readonly Func<Dictionary<string, long>> _locationIDs;
    public ServerRewardPreviewSource(GKArchipelagoClient client)
        : this(() => client.GetSession()?.Locations, client.GetAllLocations) { }
    public ServerRewardPreviewSource(Func<ILocationCheckHelper> locations, Func<Dictionary<string, long>> locationIDs)
    { _locations = locations; _locationIDs = locationIDs; }
    public async Task<IReadOnlyList<RewardPreview>> ReadPlacementsAsync(IEnumerable<string> locations)
    {
        var helper = _locations();
        if (helper == null) throw new InvalidOperationException("No AP session for technology previews.");
        var byName = _locationIDs();
        var ids = locations.Where(byName.ContainsKey).Select(name => byName[name]).ToArray();
        if (ids.Length == 0) return new RewardPreview[0];
        var placements = await helper.ScoutLocationsAsync(HintCreationPolicy.None, ids).ConfigureAwait(false);
        return placements.Values.Select(item => new RewardPreview
        {
            Location = item.LocationName, Item = item.ItemName,
            Recipient = string.IsNullOrEmpty(item.Player.Alias) ? item.Player.Name : item.Player.Alias, Game = item.ItemGame,
            Flags = item.Flags,
        }).ToArray();
    }
}
