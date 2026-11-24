using System.IO;

namespace GraveyardKeepelago.Items.Traps;

internal sealed class MonsterTrapDefinition
{
    public string MonsterID { get; }
    public int Count => 4;
    private MonsterTrapDefinition(string monsterID) => MonsterID = monsterID;

    public static MonsterTrapDefinition Get(string id) => id switch
    {
        "bats" => new MonsterTrapDefinition("bat_test"),
        "green_slimes" => new MonsterTrapDefinition("slime_test"),
        _ => throw new InvalidDataException($"Unknown monster trap '{id}'."),
    };
}
