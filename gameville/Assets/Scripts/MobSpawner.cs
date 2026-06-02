using UnityEngine;

namespace Gameville
{
    public class MobSpawner : MonoBehaviour
    {
        public GameObject mobPrefab;
        public Transform spawnPoint;
        public int mobCount = 5;
        public float spawnRadius = 20f;
        private static MobSet mobDatabase;
        public static MobSet MobDatabase { get { return mobDatabase; } }

        private void Start()
        {
            LoadMobDatabase();
            SpawnMobs();
        }

        public static void LoadMobDatabase()
        {
            if (mobDatabase != null)
                return;

            string mobPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Mobs/mobs.json");
            if (System.IO.File.Exists(mobPath))
            {
                string json = System.IO.File.ReadAllText(mobPath);
                mobDatabase = JsonUtility.FromJson<MobSet>(json);
                Debug.Log($"Loaded {mobDatabase.mobs.Count} mob types.");
            }
            else
            {
                mobDatabase = new MobSet();
                CreateDefaultMobs();
            }
        }

        private static void CreateDefaultMobs()
        {
            MobDefinition goblin = new MobDefinition
            {
                id = 1,
                name = "Goblin",
                maxHealth = 20,
                damage = 5,
                speed = 3f,
                attackRange = 2f,
                sightRange = 10f,
                experienceReward = 50
            };
            goblin.lootTable.Add(new LootTable { itemId = 1, dropChance = 0.5f });
            mobDatabase.mobs.Add(goblin);

            MobDefinition orc = new MobDefinition
            {
                id = 2,
                name = "Orc",
                maxHealth = 50,
                damage = 15,
                speed = 2f,
                attackRange = 2.5f,
                sightRange = 15f,
                experienceReward = 150
            };
            orc.lootTable.Add(new LootTable { itemId = 2, dropChance = 0.3f });
            mobDatabase.mobs.Add(orc);

            MobDefinition skeleton = new MobDefinition
            {
                id = 3,
                name = "Skeleton",
                maxHealth = 30,
                damage = 8,
                speed = 2.5f,
                attackRange = 2f,
                sightRange = 12f,
                experienceReward = 75
            };
            skeleton.lootTable.Add(new LootTable { itemId = 1, dropChance = 0.4f });
            mobDatabase.mobs.Add(skeleton);
        }

        private void SpawnMobs()
        {
            if (mobPrefab == null || spawnPoint == null)
            {
                Debug.LogError("MobSpawner: mobPrefab or spawnPoint not assigned.");
                return;
            }

            for (int i = 0; i < mobCount; i++)
            {
                MobDefinition mobDef = mobDatabase.mobs[Random.Range(0, mobDatabase.mobs.Count)];
                Vector3 spawnPos = spawnPoint.position + Random.insideUnitSphere * spawnRadius;
                spawnPos.y = spawnPoint.position.y;

                GameObject mobInstance = Instantiate(mobPrefab, spawnPos, Quaternion.identity);
                mobInstance.name = mobDef.name;

                Mob mobComponent = mobInstance.GetComponent<Mob>();
                if (mobComponent != null)
                {
                    mobComponent.definition = mobDef;
                }
            }

            Debug.Log($"Spawned {mobCount} mobs.");
        }

        public static MobDefinition GetMobDefinition(int mobId)
        {
            return mobDatabase?.mobs.Find(m => m.id == mobId);
        }
    }
}
