using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Gameville
{
    public class GameLoader : MonoBehaviour
    {
        [SerializeField] private bool autoLoadOnAwake = true;
        [SerializeField] private bool debugLog = true;

        private static bool initialized = false;

        private void Awake()
        {
            if (autoLoadOnAwake && !initialized)
            {
                LoadGame();
                initialized = true;
            }
        }

        public void LoadGame()
        {
            Debug.Log("[GameLoader] Starting game initialization...");

            try
            {
                // Load all ability definitions
                AbilityManager.Initialize();
                LogLoadStatus("Abilities", AbilityManager.abilities.Count);

                // Load item definitions
                Inventory.LoadItemDatabase();
                LogLoadStatus("Items", Inventory.ItemDatabase.items.Count);

                // Load mob definitions
                MobSpawner.LoadMobDatabase();
                LogLoadStatus("Mobs", MobSpawner.MobDatabase.mobs.Count);

                // Initialize skin system
                SkinManager.InitializeSkins();
                LogLoadStatus("Skins", GetSkinCount());

                Debug.Log("[GameLoader] ✓ Game initialization complete!");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameLoader] ✗ Failed to load game: {e.Message}\n{e.StackTrace}");
            }
        }

        private void LogLoadStatus(string systemName, int count)
        {
            if (debugLog)
                Debug.Log($"[GameLoader] Loaded {systemName}: {count} entries");
        }

        private int GetSkinCount()
        {
            // Try to get skin count from file
            string jsonPath = Path.Combine(Application.streamingAssetsPath, "Skins", "skins.json");
            try
            {
                if (File.Exists(jsonPath))
                {
                    string json = File.ReadAllText(jsonPath);
                    SkinDatabase db = JsonUtility.FromJson<SkinDatabase>(json);
                    return db.skins.Count;
                }
            }
            catch { }
            return 0;
        }

        [System.Serializable]
        private class SkinDatabase
        {
            public List<SkinData> skins = new List<SkinData>();
        }

        [System.Serializable]
        private class SkinData
        {
            public string name;
            public int type;
        }
    }
}
