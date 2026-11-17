using System.IO;

namespace GraveyardKeepelago.Items.Grants;

internal sealed class ResearchPointsGrant : IItemGrant
{
    public string Color { get; }
    public int Amount { get; }

    public ResearchPointsGrant(string color, int amount) => (Color, Amount) = (color, amount);

    public GrantResult Apply(GrantContext context)
    {
        var player = context.Player;
        var (red, green, blue) = Color switch
        {
            "red" => (Amount, 0, 0),
            "green" => (0, Amount, 0),
            "blue" => (0, 0, Amount),
            _ => throw new InvalidDataException($"Unknown research point color '{Color}'."),
        };
        TechPointsDrop.Drop(player.transform.position + player.transform.forward, red, green, blue);
        return GrantResult.Applied;
    }
}
