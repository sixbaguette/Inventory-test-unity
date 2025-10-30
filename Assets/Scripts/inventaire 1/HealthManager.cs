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

        // 🔹 Met à jour l'UI au démarrage
        if (HealthBarUI.Instance != null)
            HealthBarUI.Instance.UpdateHealthBar(currentHealth, maxHealth);
    }

    private void Start()
    {
        Debug.Log($"[HealthManager] Initial Health = {currentHealth}/{maxHealth}");
        if (HealthBarUI.Instance != null)
            HealthBarUI.Instance.UpdateHealthBar(currentHealth, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        Debug.Log($"{gameObject.name} took {amount} damage → {currentHealth}/{maxHealth}");

        if (HealthBarUI.Instance != null)
            HealthBarUI.Instance.UpdateHealthBar(currentHealth, maxHealth);

        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        float before = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        Debug.Log($"[{gameObject.name}] healed {amount} → {before}→{currentHealth}/{maxHealth}");

        if (HealthBarUI.Instance != null)
            HealthBarUI.Instance.UpdateHealthBar(currentHealth, maxHealth);
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} died.");
        Destroy(gameObject);
    }
}
