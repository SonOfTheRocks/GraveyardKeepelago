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
        var counter = Color switch
        {
            "red" => "r",
            "green" => "g",
            "blue" => "b",
            _ => throw new InvalidDataException($"Unknown research point color '{Color}'."),
        };
        // The native drop credits this saved counter after its animation.
        player.AddToParams(counter, Amount);
        context.Save.achievements.CheckKeyQuests("tech_collect_" + counter, Amount);
        GUIElements.me?.hud?.tech_points_bar?.Redraw();
        return GrantResult.Applied;
    }
}
