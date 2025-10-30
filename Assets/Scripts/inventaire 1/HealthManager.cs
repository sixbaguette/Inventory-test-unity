using UnityEngine;

public class HealthManager : MonoBehaviour
{
    [Header("Santé du joueur")]
    public float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    public float CurrentHealth => currentHealth; // propriété publique pour lecture par HealthBarUI

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        Debug.Log($"{gameObject.name} took {amount} damage → {currentHealth}/{maxHealth}");

        // ✅ On ne fait plus d'appel manuel, la HealthBarUI écoute toute seule
        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        Debug.Log($"{gameObject.name} healed {amount} → {currentHealth}/{maxHealth}");
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} died.");
        Destroy(gameObject);
    }
}
