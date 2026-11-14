using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json;

namespace GraveyardKeepelago.Items;

internal sealed class ItemReceipt
{
    [JsonProperty(Required = Required.Always)] public int Index { get; set; }
    [JsonProperty(Required = Required.Always)] public long ItemID { get; set; }
    [JsonProperty(Required = Required.Always)] public long LocationID { get; set; }
    [JsonProperty(Required = Required.Always)] public long PlayerID { get; set; }
    [JsonProperty(Required = Required.Always)] public string Name { get; set; }

    public bool Matches(NetworkItem item) =>
        ItemID == item.Item && LocationID == item.Location && PlayerID == item.Player;
}
