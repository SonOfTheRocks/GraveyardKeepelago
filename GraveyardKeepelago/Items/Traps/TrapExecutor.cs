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
            MonsterSpawner.SpawnMonster(trap);
        }
    }
}
