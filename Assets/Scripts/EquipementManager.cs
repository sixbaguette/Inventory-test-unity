using UnityEngine;
using System.Linq;

public class EquipementManager : MonoBehaviour
{
    public static EquipementManager Instance;

    [Header("Slots d’équipement du joueur")]
    public EquipementSlot[] equipSlots; // <– Tu pourras les glisser ici dans l’inspecteur

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Essaie d’équiper un item dans un slot compatible.
    /// Retourne true si l’objet a été équipé.
    /// </summary>
    public bool TryEquipItem(ItemUI itemUI)
    {
        if (itemUI == null || itemUI.itemData == null)
            return false;

        ItemData data = itemUI.itemData;

        // 1️⃣ On ne traite que les objets équipables
        if (!data.isEquipable)
        {
            Debug.Log($"[Equip] {data.itemName} n’est pas équipable.");
            return false;
        }

        // 2️⃣ Recherche d’un slot compatible libre
        EquipementSlot slot = equipSlots
            .FirstOrDefault(s => s != null && s.IsCompatible(data) && s.CurrentItem == null);

        if (slot == null)
        {
            Debug.Log($"[Equip] Aucun slot compatible ou libre pour {data.itemName} !");
            UIEffects.Shake(itemUI.rectTransform);
            return false;
        }

        // 3️⃣ Équipe dans ce slot
        slot.OnDrop(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        {
            pointerDrag = itemUI.gameObject
        });

        Debug.Log($"[Equip] {data.itemName} équipé dans {slot.name}");
        return true;
    }

    public void UnequipItem(ItemUI itemUI)
    {
        if (itemUI == null) return;

        // Trouve le slot qui le contient
        var slot = equipSlots.FirstOrDefault(s => s != null && s.CurrentItem == itemUI);
        if (slot == null)
        {
            Debug.LogWarning($"[Unequip] Impossible de trouver le slot pour {itemUI.itemData.itemName}");
            return;
        }

        // Déséquipe normalement (le slot s’en charge)
        slot.UnequipItem();

        Debug.Log($"[Unequip] {itemUI.itemData.itemName} retiré du slot {slot.name}");
    }
}
