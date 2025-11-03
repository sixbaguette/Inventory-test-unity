using UnityEngine;

public class ArmorManager : MonoBehaviour
{
    [Header("Références")]
    public EquipementManager equipementManager;
    public HealthManager healthManager;

    [Header("Paramètres d’équilibrage")]
    [Tooltip("Facteur minimal de dégâts restants (évite l'invincibilité totale)")]
    [Range(0f, 1f)] public float minDamageFactor = 0.05f;

    private void Start()
    {
        if (equipementManager == null)
            equipementManager = FindFirstObjectByType<EquipementManager>();
        if (healthManager == null)
            healthManager = FindFirstObjectByType<HealthManager>();
    }

    /// <summary>
    /// Applique des dégâts localisés selon la partie du corps touchée.
    /// </summary>
    public void ApplyLocalizedDamage(float baseDamage, BodyPart.PartType partType)
    {
        if (healthManager == null)
        {
            Debug.LogWarning("[ArmorManager] Aucun HealthManager trouvé sur le joueur !");
            return;
        }

        float finalDamage = baseDamage;

        // 🔍 Récupère l’armure correspondante à la zone touchée
        ItemData armor = GetArmorForBodyPart(partType);

        if (armor != null && armor.isArmor)
        {
            float reduction = Mathf.Clamp01(armor.armorValue / 100f); // ex : 70 → 0.7
            float damageFactor = Mathf.Clamp(1f - reduction, minDamageFactor, 1f);

            finalDamage = baseDamage * damageFactor;

            Debug.Log($"[ArmorManager] {partType} protégé par {armor.itemName} " +
                      $"→ {baseDamage} → {finalDamage} après réduction {armor.armorValue}%");
        }
        else
        {
            Debug.Log($"[ArmorManager] {partType} non protégé → dégâts complets {baseDamage}");
        }

        healthManager.TakeDamage(finalDamage);
    }

    /// <summary>
    /// Retourne l’ItemData d’armure qui protège cette partie du corps.
    /// </summary>
    private ItemData GetArmorForBodyPart(BodyPart.PartType partType)
    {
        if (equipementManager == null || equipementManager.equipSlots == null)
            return null;

        EquipSlotType neededSlot = partType switch
        {
            BodyPart.PartType.Head => EquipSlotType.Helmet,
            BodyPart.PartType.Torso => EquipSlotType.Armor,
            BodyPart.PartType.Legs => EquipSlotType.Legging,
            _ => EquipSlotType.None
        };

        foreach (var slot in equipementManager.equipSlots)
        {
            if (slot == null || slot.CurrentItem == null) continue;
            var data = slot.CurrentItem.itemData;

            if (data != null && data.isArmor && data.equipSlotType == neededSlot)
                return data;
        }

        return null;
    }
}
