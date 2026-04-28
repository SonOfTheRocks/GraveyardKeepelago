using System.Collections.Generic;
using KaitoKid.Utilities.Interfaces;

namespace GraveyardKeepelago.Items.Traps
{
    public class MonsterSpawner
    {
        private ILogger _logger;
        private static MobSpawner _spawner;
        private const string SpawnerId = "ap_spawner";

        private readonly Dictionary<Difficulty,List<SpawnerDefinition.MobDefinition>> _mobs = CreateMobs();

        private static Dictionary<Difficulty, List<SpawnerDefinition.MobDefinition>> CreateMobs()
        {
            var monsters = new Dictionary<Difficulty, List<SpawnerDefinition.MobDefinition>>();
            
            var bats = new SpawnerDefinition.MobDefinition
            {
                mob_name = "bat_test",
                weight = 1,
                mobs_count = 4,
            };
            var greenSlimes = new SpawnerDefinition.MobDefinition
            {
                mob_name = "slime_test",
                weight = 1,
                mobs_count = 4,
            };
            monsters.Add(Difficulty.Easy, new List<SpawnerDefinition.MobDefinition>{ bats, greenSlimes });
            
            var blueSlimes = new SpawnerDefinition.MobDefinition
            {
                mob_name = "slime_blue",
                weight = 10,
                mobs_count = 4,
            };
            var fireBats = new SpawnerDefinition.MobDefinition
            {
                mob_name = "fire_bat",
                weight = 10,
                mobs_count = 4,
            };
            var giantFlies = new SpawnerDefinition.MobDefinition
            {
                mob_name = "fly_mob",
                weight = 10,
                mobs_count = 4,
            };
            var lightningBalls = new SpawnerDefinition.MobDefinition
            {
                mob_name = "lightning_ball",
                weight = 5,
                mobs_count = 4,
            };
            monsters.Add(Difficulty.Medium, new List<SpawnerDefinition.MobDefinition> { fireBats, giantFlies, lightningBalls });
            
            var orangeSlimes = new SpawnerDefinition.MobDefinition
            {
                mob_name = "slime_orange",
                weight = 10,
                mobs_count = 4,
            };
            var poppets = new SpawnerDefinition.MobDefinition
            {
                mob_name = "slime_mask",
                weight = 10,
                mobs_count = 2,
            };
            var ironMaidens = new SpawnerDefinition.MobDefinition
            {
                mob_name = "iron_maiden",
                weight = 10,
                mobs_count = 2,
            };
            var spider = new SpawnerDefinition.MobDefinition
            {
                mob_name = "spider",
                weight = 10,
                mobs_count = 2,
            };
            var graphiteGolem = new SpawnerDefinition.MobDefinition
            {
                mob_name = "golem",
                weight = 10,
                mobs_count = 1,
            };
            var silverGolem = new SpawnerDefinition.MobDefinition
            {
                mob_name = "golem_blue",
                weight = 10,
                mobs_count = 1,
            };
            var goldenGolem = new SpawnerDefinition.MobDefinition
            {
                mob_name = "golem_red",
                weight = 10,
                mobs_count = 1,
            };
            monsters.Add(Difficulty.Hard, new List<SpawnerDefinition.MobDefinition>
                { orangeSlimes, poppets, ironMaidens, spider, graphiteGolem, silverGolem, goldenGolem });
            
            return monsters;
        }

        public MonsterSpawner(ILogger logger)
        {
            _logger = logger;
        }
        
        public void SpawnMonster(Difficulty difficulty)
        {
            CreateSpawnerIfNotExist();
            if (_mobs.TryGetValue(difficulty, out var mobs))
            {
                GameBalance.me.GetData<SpawnerDefinition>(SpawnerId).mobs = mobs;
                _spawner.ActivateSpawner();
                return;
            }

            _logger.LogError($"Could not create mobs for Difficultly: {difficulty}");
        }

        private void CreateSpawnerIfNotExist()
        {
            if (_spawner != null)
                return;
            
            //_spawner = Traverse.Create(MainGame.me.player_char).Field("_go").GetValue<GameObject>().AddComponent<MobSpawner>();
            _spawner = MainGame.me.world.gameObject.AddComponent<MobSpawner>();
            _spawner.spawner_id = SpawnerId;

            var spawnerDefinition = new SpawnerDefinition
            {
                id = SpawnerId,
                spawner_id = SpawnerId,
                dungeon_level = 0
            };

            GameBalance.me.AddData(spawnerDefinition);
        }
        
        public enum Difficulty
        {
            Easy,
            Medium,
            Hard,
        }
    }
}