using System;
using UnityEngine;

namespace LoneTruth.Combat
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0;

        public event Action<int, int> OnDamaged;
        public event Action OnDeath;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(int damage)
        {
            if (IsDead || damage <= 0)
            {
                return;
            }

            currentHealth = Mathf.Max(0, currentHealth - damage);
            OnDamaged?.Invoke(damage, currentHealth);

            if (currentHealth <= 0)
            {
                OnDeath?.Invoke();
            }
        }

        /// <summary>Sets health directly (loading a save).</summary>
        public void SetCurrent(int value)
        {
            currentHealth = Mathf.Clamp(value, 0, maxHealth);
        }

        /// <summary>A raised maximum also adds the difference to current health.</summary>
        public void SetMaxHealth(int value)
        {
            int gained = value - maxHealth;
            maxHealth = value;
            currentHealth = Mathf.Clamp(currentHealth + Mathf.Max(0, gained), 0, maxHealth);
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0)
            {
                return;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }
    }
}
