using UnityEngine;
using System.Collections.Generic;

namespace Gameville
{
    [System.Serializable]
    public class LootTable
    {
        public int itemId;
        public float dropChance;
        public int minQuantity = 1;
        public int maxQuantity = 1;
    }

    [System.Serializable]
    public class MobDefinition
    {
        public int id;
        public string name;
        public int maxHealth;
        public int damage;
        public float speed;
        public float attackRange;
        public float sightRange;
        public List<LootTable> lootTable = new List<LootTable>();
        public int experienceReward;
        public List<int> abilities = new List<int>(); // Ability IDs this mob can use
    }

    [System.Serializable]
    public class MobSet
    {
        public List<MobDefinition> mobs = new List<MobDefinition>();
    }

    public class Mob : MonoBehaviour
    {
        public MobDefinition definition;
        public int currentHealth;
        public bool isAlive = true;

        private Rigidbody rb;
        private Transform playerTarget;
        private float attackCooldown;
        private Collider mobCollider;
        private AbilityTracker abilityTracker;
        public GameObject gravityEffectPrefab; // optional particle prefab
        public AudioClip gravitySfx; // optional sound effect

        private void Start()
        {
            if (definition != null)
            {
                currentHealth = definition.maxHealth;
                rb = GetComponent<Rigidbody>();
                mobCollider = GetComponent<Collider>();

                // Initialize ability system
                AbilityManager.Initialize();
                abilityTracker = new AbilityTracker();
            }
        }

        private void Update()
        {
            if (!isAlive)
                return;

            FindPlayer();
            if (playerTarget != null)
            {
                MoveTowardPlayer();
                if (Vector3.Distance(transform.position, playerTarget.position) <= definition.attackRange)
                {
                    TryAttackPlayer();
                }
            }

            if (attackCooldown > 0)
                attackCooldown -= Time.deltaTime;

            if (abilityTracker != null)
                abilityTracker.Update(Time.deltaTime);
        }

        private void FindPlayer()
        {
            if (playerTarget != null)
                return;

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null && Vector3.Distance(transform.position, player.transform.position) <= definition.sightRange)
            {
                playerTarget = player.transform;
            }
        }

        private void MoveTowardPlayer()
        {
            if (playerTarget == null)
                return;

            Vector3 direction = (playerTarget.position - transform.position).normalized;
            if (rb != null)
            {
                rb.velocity = new Vector3(direction.x * definition.speed, rb.velocity.y, direction.z * definition.speed);
            }
            else
            {
                transform.position += direction * definition.speed * Time.deltaTime;
            }
        }

        private void TryAttackPlayer()
        {
            if (attackCooldown > 0)
                return;

            PlayerHealth playerHealth = playerTarget.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // 30% chance to use ability instead of basic attack
                if (definition.abilities.Count > 0 && Random.value < 0.3f)
                {
                    TryUseAbility(playerHealth);
                }
                else
                {
                    playerHealth.TakeDamage(definition.damage);
                }
                attackCooldown = 1f;
            }
        }

        private void TryUseAbility(PlayerHealth playerHealth)
        {
            // Pick a random ability
            int abilityId = definition.abilities[Random.Range(0, definition.abilities.Count)];
            AbilityDefinition ability = AbilityManager.GetAbility(abilityId);

            if (ability == null || !abilityTracker.IsAbilityReady(abilityId))
                return;

            // Gravity-control special case
            if (ability.type == "utility" && ability.name.ToLower().Contains("gravity"))
            {
                StartCoroutine(ApplyGravityControl(ability.duration, ability.gravityStrength));
                abilityTracker.UseAbility(abilityId, ability.cooldown);
                Debug.Log($"{definition.name} used {ability.name} to alter gravity (strength {ability.gravityStrength}) for {ability.duration} seconds!");
                return;
            }

            // Use the ability as damage/heal as default
            float totalDamage = ability.damage + definition.damage;
            playerHealth.TakeDamage((int)totalDamage);
            abilityTracker.UseAbility(abilityId, ability.cooldown);

            Debug.Log($"{definition.name} used {ability.name} for {totalDamage} damage!");
        }

        private System.Collections.IEnumerator ApplyGravityControl(float duration, float gravityStrength)
        {
            Vector3 oldGravity = Physics.gravity;
            Vector3 newGravity = new Vector3(0f, gravityStrength, 0f);

            // Spawn effect and play sound if provided
            GameObject effect = null;
            if (gravityEffectPrefab != null)
            {
                effect = Instantiate(gravityEffectPrefab, transform.position, Quaternion.identity);
                effect.transform.parent = transform;
            }
            if (gravitySfx != null)
            {
                AudioSource.PlayClipAtPoint(gravitySfx, transform.position);
            }

            Physics.gravity = newGravity;
            // Screen shake telegraph for players
            float shakeIntensity = Mathf.Clamp(Mathf.Abs(gravityStrength) / 20f, 0.5f, 10f);
            if (ScreenShake.Instance != null)
            {
                ScreenShake.Instance.Shake(shakeIntensity, duration);
            }
            yield return new WaitForSeconds(duration);

            Physics.gravity = oldGravity;

            if (effect != null)
                Destroy(effect);
        }

        public void TakeDamage(int damage)
        {
            currentHealth -= damage;
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            isAlive = false;
            if (mobCollider != null)
                mobCollider.enabled = false;

            DropLoot();
            Destroy(gameObject, 0.5f);
        }

        private void DropLoot()
        {
            foreach (LootTable loot in definition.lootTable)
            {
                if (Random.value <= loot.dropChance)
                {
                    int quantity = Random.Range(loot.minQuantity, loot.maxQuantity + 1);
                    // Drop as pickup prefab
                    Debug.Log($"Dropped {quantity}x item {loot.itemId}");
                }
            }
        }

        public int GetHealth()
        {
            return currentHealth;
        }
    }
}
