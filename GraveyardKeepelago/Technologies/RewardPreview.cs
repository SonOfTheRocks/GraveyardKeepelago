using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net.Enums;

namespace GraveyardKeepelago.Technologies;

public sealed class RewardPreview
{
    public string Location { get; set; }
    public string Item { get; set; }
    public string Recipient { get; set; }
    public string Game { get; set; }
    public ItemFlags? Flags { get; set; }
}

public interface IRewardPreviewSource
{
    // Reading placement data is separate from publishing AP hints.
    Task<IReadOnlyList<RewardPreview>> ReadPlacementsAsync(IEnumerable<string> locations);
}

public sealed class RewardPreviewCache
{
    private readonly IRewardPreviewSource _source;
    private readonly Dictionary<string, RewardPreview> _items = new();
    public RewardPreviewCache(IRewardPreviewSource source) => _source = source;
    public RewardPreview Get(string location) => _items.TryGetValue(location, out var item) ? item : null;
    public async Task<IReadOnlyList<RewardPreview>> FetchAsync(IEnumerable<string> locations)
        => await _source.ReadPlacementsAsync(locations.Distinct().ToArray()).ConfigureAwait(false);
    public void Accept(IEnumerable<RewardPreview> items)
    {
        foreach (var item in items) _items[item.Location] = item;
    }
}
