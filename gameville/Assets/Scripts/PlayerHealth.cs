using UnityEngine;

namespace Gameville
{
    public class PlayerHealth : MonoBehaviour
    {
        public int maxHealth = 100;
        private int currentHealth;

        private void Start()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(int damage)
        {
            currentHealth -= damage;
            Debug.Log($"Player took {damage} damage. Health: {currentHealth}");

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(int amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            Debug.Log($"Player healed for {amount}. Health: {currentHealth}");
        }

        private void Die()
        {
            Debug.Log("Player died!");
            // Respawn logic here
        }

        public int GetHealth()
        {
            return currentHealth;
        }

        public int GetMaxHealth()
        {
            return maxHealth;
        }
    }
}
