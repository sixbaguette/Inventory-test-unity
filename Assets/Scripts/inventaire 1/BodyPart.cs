using UnityEngine;

public class BodyPart : MonoBehaviour
{
    public enum PartType { Head, Torso, Legs }

    [Header("Partie du corps")]
    public PartType partType;

    [Header("Multiplicateurs de dégâts")]
    public float headMultiplier = 2f;
    public float torsoMultiplier = 1f;
    public float legMultiplier = 0.8f;

    private ArmorManager armorManager;
    private HealthManager healthManager;

    void Start()
    {
        armorManager = GetComponentInParent<ArmorManager>();
        healthManager = GetComponentInParent<HealthManager>();

        if (armorManager == null)
            Debug.LogWarning($"[BodyPart:{name}] Aucun ArmorManager trouvé sur le parent !");
        if (healthManager == null)
            Debug.LogWarning($"[BodyPart:{name}] Aucun HealthManager trouvé sur le parent !");
    }

    /// <summary>
    /// Reçoit les dégâts depuis une balle et applique le multiplicateur selon la partie touchée.
    /// </summary>
    public void ApplyDamage(float baseDamage, BulletType bulletType)
    {
        // 🎯 Applique le multiplicateur selon la zone
        float zoneMultiplier = 1f;

        switch (partType)
        {
            case PartType.Head:
                zoneMultiplier = headMultiplier;
                break;
            case PartType.Torso:
                zoneMultiplier = torsoMultiplier;
                break;
            case PartType.Legs:
                zoneMultiplier = legMultiplier;
                break;
        }

        float scaledDamage = baseDamage * zoneMultiplier;
        Debug.Log($"[BodyPart] {partType} touché → base {baseDamage} x{zoneMultiplier} = {scaledDamage}");

        // ⚙️ Envoie les dégâts à l’ArmorManager (qui applique la réduction d’armure)
        if (armorManager != null)
        {
            armorManager.ApplyLocalizedDamage(scaledDamage, partType);
        }
        else if (healthManager != null)
        {
            // Fallback sans armure
            healthManager.TakeDamage(scaledDamage);
        }
    }
}
