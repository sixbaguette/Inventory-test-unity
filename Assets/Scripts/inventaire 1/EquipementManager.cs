using System.Linq;
using UnityEngine;
using static ItemUI;

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

        // 🔧 IMPORTANT : libère les anciens slots d’inventaire (sinon ils restent marqués comme occupés)
        if (itemUI.occupiedSlots != null)
        {
            foreach (var s in itemUI.occupiedSlots)
                if (s != null)
                    s.ClearItem();
        }
        itemUI.occupiedSlots = null;
        itemUI.currentSlot = null;

        // 🔖 Retire proprement de la liste d'inventaire (logique)
        InventoryManager.Instance?.DetachWithoutDestroy(itemUI);

        // 3️⃣ Équipe dans ce slot
        slot.OnDrop(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        {
            pointerDrag = itemUI.gameObject
        });

        // 🔊 Sons
        if (data.equipSlotType == EquipSlotType.Primary || data.equipSlotType == EquipSlotType.Secondary)
            InventoryAudioManager.Instance.Play("equip_weapon");
        else if (data.equipSlotType == EquipSlotType.Armor || data.equipSlotType == EquipSlotType.Helmet || data.equipSlotType == EquipSlotType.Legging)
            InventoryAudioManager.Instance.Play("equip_armor");

        // ✅ Met à jour la propriété Owner (suivi logique)
        itemUI.Owner = ItemOwner.Equipment;

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

        // 1️⃣ Déséquipe visuellement (le slot s’en charge)
        slot.UnequipItem();

        Debug.Log($"[Unequip] {itemUI.itemData.itemName} retiré du slot {slot.name}");

        // 2️⃣ Replace l’objet dans l’inventaire joueur
        var inv = InventoryManager.Instance;
        if (inv != null)
        {
            bool placed = inv.TryAutoPlace(itemUI);
            if (placed)
            {
                inv.AddToInventoryList(itemUI);
                itemUI.Owner = ItemOwner.Player;
                Debug.Log($"[Unequip] {itemUI.itemData.itemName} replacé dans l’inventaire joueur.");
            }
            else
            {
                Debug.LogWarning("[Unequip] Inventaire plein, impossible de replacer l’item.");
            }
        }

        // 3️⃣ Si c’était une arme → retirer visuellement
        if (itemUI.itemData != null &&
            (itemUI.itemData.equipSlotType == EquipSlotType.Primary || itemUI.itemData.equipSlotType == EquipSlotType.Secondary))
        {
            var hotbar = FindFirstObjectByType<HotbarManager>();
            if (hotbar != null && hotbar.playerEquipHandler != null)
            {
                hotbar.playerEquipHandler.UnequipAll();
                Debug.Log("[EquipementManager] Arme retirée visuellement du joueur");
            }
        }
    }

    public bool IsEquipped(ItemUI ui)
    {
        if (ui == null || equipSlots == null) return false;
        foreach (var s in equipSlots)
            if (s != null && s.CurrentItem == ui)
                return true;
        return false;
    }

    // Déséquipe et replace dans l’inventaire joueur
    public bool UnequipToInventory(ItemUI ui)
    {
        if (ui == null) return false;

        // ton UnequipItem actuel devrait déjà vider le slot + nettoyer les refs
        UnequipItem(ui);

        // puis on replace proprement côté joueur
        var inv = InventoryManager.Instance;
        if (inv == null) return false;

        // Essaye placement auto (orientation incluse)
        if (inv.TryAutoPlace(ui))
        {
            inv.AddToInventoryList(ui);
            ui.Owner = ItemOwner.Player;
            return true;
        }

        // pas de place -> on peut échouer (ou dropper si tu veux)
        Debug.LogWarning("[Equipement] Inventaire plein, impossible de replacer l’item déséquipé.");
        return false;
    }
}
