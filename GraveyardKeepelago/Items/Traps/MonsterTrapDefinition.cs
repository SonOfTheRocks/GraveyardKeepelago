using System.IO;

namespace GraveyardKeepelago.Items.Traps;

public sealed class MonsterTrapDefinition
{
    public string ID { get; }
    public string Name { get; }
    public string MonsterID { get; }
    public string SpriteName { get; }
    public int Count => 4;

    private MonsterTrapDefinition(string id, string name, string monsterID, string spriteName)
    {
        ID = id; Name = name; MonsterID = monsterID; SpriteName = spriteName;
    }

    // Native animation frames are present in the game's chars sprite atlas.
    public static readonly MonsterTrapDefinition Bats =
        new("bats", "Bats Trap", "bat_test", "300_bat_face_fly_1");
    public static readonly MonsterTrapDefinition GreenSlimes =
        new("green_slimes", "Green Slime Trap", "slime_test", "320_slime_jump_1");

    public static MonsterTrapDefinition Get(string id) => id == Bats.ID ? Bats :
        id == GreenSlimes.ID ? GreenSlimes :
        throw new InvalidDataException($"Unknown trap '{id}'.");
}
