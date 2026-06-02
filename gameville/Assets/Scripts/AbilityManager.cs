using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MoonSharp.Interpreter;

namespace Gameville
{
    [System.Serializable]
    public class AbilityDefinition
    {
        public int id;
        public string name;
        public string type; // "damage", "heal", "buff", "debuff", "utility"
        public float damage;
        public float healing;
        public float cooldown;
        public float cost; // mana/energy cost
        public float range; // effect range
        public float duration; // buff/debuff duration
        // Optional extra parameters for certain abilities
        public float gravityStrength = -9.81f;
        public string description;
    }

    public class AbilityManager : MonoBehaviour
    {
        public static Dictionary<int, AbilityDefinition> abilities = new Dictionary<int, AbilityDefinition>();

        private static bool initialized = false;

        public static void Initialize()
        {
            if (initialized) return;

            LoadAbilityDatabase();
            initialized = true;
        }

        private static void LoadAbilityDatabase()
        {
            string jsonPath = Path.Combine(Application.streamingAssetsPath, "Abilities", "abilities.json");

            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning($"Ability database not found at {jsonPath}. Using defaults.");
                return;
            }

            try
            {
                string json = File.ReadAllText(jsonPath);
                AbilityDatabase db = JsonUtility.FromJson<AbilityDatabase>(json);

                abilities.Clear();
                foreach (var ability in db.abilities)
                {
                    abilities[ability.id] = ability;
                }

                Debug.Log($"Loaded {abilities.Count} abilities from JSON");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load ability database: {e.Message}");
            }
        }

        public static AbilityDefinition GetAbility(int abilityId)
        {
            if (abilities.ContainsKey(abilityId))
                return abilities[abilityId];

            Debug.LogWarning($"Ability {abilityId} not found");
            return null;
        }

        public static bool CanUseAbility(float currentCost, AbilityDefinition ability)
        {
            return currentCost >= ability.cost;
        }

        public static float UseAbility(AbilityDefinition ability, float currentCost)
        {
            if (CanUseAbility(currentCost, ability))
            {
                return currentCost - ability.cost;
            }
            return currentCost;
        }
    }

    [System.Serializable]
    public class AbilityDatabase
    {
        public List<AbilityDefinition> abilities = new List<AbilityDefinition>();
    }

    /// <summary>
    /// Ability tracker for player/mob ability usage
    /// </summary>
    public class AbilityTracker
    {
        private Dictionary<int, float> abilityCooldowns = new Dictionary<int, float>();

        public bool IsAbilityReady(int abilityId)
        {
            if (!abilityCooldowns.ContainsKey(abilityId))
                return true;

            return abilityCooldowns[abilityId] <= 0f;
        }

        public void UseAbility(int abilityId, float cooldown)
        {
            abilityCooldowns[abilityId] = cooldown;
        }

        public void Update(float deltaTime)
        {
            var keys = new List<int>(abilityCooldowns.Keys);
            foreach (var abilityId in keys)
            {
                abilityCooldowns[abilityId] -= deltaTime;
                if (abilityCooldowns[abilityId] < 0f)
                    abilityCooldowns[abilityId] = 0f;
            }
        }

        public float GetRemainingCooldown(int abilityId)
        {
            if (!abilityCooldowns.ContainsKey(abilityId))
                return 0f;

            return Mathf.Max(0f, abilityCooldowns[abilityId]);
        }
    }
}
