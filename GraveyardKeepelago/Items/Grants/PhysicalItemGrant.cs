using System.IO;
using GraveyardKeepelago.GameModifications;

namespace GraveyardKeepelago.Items.Grants;

internal sealed class PhysicalItemGrant : IItemGrant
{
    public string ItemID { get; }

    public PhysicalItemGrant(string itemID) => ItemID = itemID;

    public GrantResult Apply(GrantContext context)
    {
        var player = context.Player;
        var item = new Item(ItemID, 1);
        if (item.definition == null)
            throw new InvalidDataException($"Unknown game item '{ItemID}'.");

        PhysicalItemDelivery.Deliver(ItemID, !item.definition.player_cant_throw_out,
            () => player.AddToInventory(item),
            () =>
            {
                var character = player.components.character;
                return DropResGameObject.Drop(character.tf.position, item, character.tf.parent, Direction.ToPlayer) != null;
            });
        return GrantResult.Applied;
    }
}
