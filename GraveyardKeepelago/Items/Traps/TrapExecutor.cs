using GraveyardKeepelago.Archipelago;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Items.Traps
{
    public class TrapExecutor
    {
        private static GKArchipelagoClient _archipelago;
        private static ILogger _logger;

        public readonly MonsterSpawner MonsterSpawner;

        public TrapExecutor(ILogger logger, GKArchipelagoClient archipelago)
        {
            _logger = logger;
            _archipelago = archipelago;
            MonsterSpawner = new MonsterSpawner(logger);
        }

        public void SpawnMonster()
        {
            MonsterSpawner.SpawnMonster(MonsterSpawner.Difficulty.Easy);
        }
    }
}