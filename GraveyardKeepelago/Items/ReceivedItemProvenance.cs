namespace GraveyardKeepelago.Items;

internal static class ReceivedItemProvenance
{
    // Item._params is natively serialized and copied by Item(Item).
    private const string Parameter = "graveyard_keepelago_archipelago_item";
    internal static void Mark(Item item) => item.SetParam(Parameter, 1f);
    internal static bool IsReceived(Item item) => item != null && item.GetParam(Parameter) == 1f;
}
