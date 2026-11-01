using GraveyardKeepelago.Archipelago;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Items.Traps
{
    public class TrapExecutor
    {
        public readonly MonsterSpawner MonsterSpawner;

        public TrapExecutor(ILogger logger, GKArchipelagoClient archipelago)
        {
            MonsterSpawner = new MonsterSpawner(logger);
        }

        public void SpawnMonster(string trapID)
        {
            var trap = MonsterTrapDefinition.Get(trapID);
            // Compatibility for the reserved old item only. New items always
            // choose their monster when the seed is generated.
            if (trap == MonsterTrapDefinition.Legacy)
                trap = UnityEngine.Random.Range(0, 2) == 0 ? MonsterTrapDefinition.Bats : MonsterTrapDefinition.GreenSlimes;
            MonsterSpawner.SpawnMonster(trap);
        }
    }
}
