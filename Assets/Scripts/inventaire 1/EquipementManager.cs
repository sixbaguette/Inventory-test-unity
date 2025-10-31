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
        if (itemUI == null || itemUI.itemData == null) return false;
        var data = itemUI.itemData;

        // 0) PRIORITÉ : si stackable → tenter de fusionner dans un stack déjà équipé
        if (data.isStackable)
        {
            bool merged = TryMergeIntoEquipment(itemUI);
            if (merged)
            {
                // si la source a été vidée, on a terminé
                if (itemUI == null || itemUI.currentStack <= 0) return true;
                // sinon, on continue (peut rester du reste à équiper)
            }
        }

        // 1) Si pas d’équipement possible pour ce type → stop
        if (!data.isEquipable)
        {
            Debug.Log($"[Equip] {data.itemName} n’est pas équipable.");
            return false;
        }

        // 2) Cherche un slot compatible LIBRE
        var slot = equipSlots.FirstOrDefault(s => s != null && s.IsCompatible(data) && s.CurrentItem == null);
        if (slot == null)
        {
            Debug.Log($"[Equip] Aucun slot compatible libre pour {data.itemName} (après tentative de merge).");
            UIEffects.Shake(itemUI.rectTransform);
            return false;
        }

        // 3) Libère les anciens slots d’inventaire
        if (itemUI.occupiedSlots != null)
        {
            foreach (var s in itemUI.occupiedSlots)
                s?.ClearItem();
        }
        itemUI.occupiedSlots = null;
        itemUI.currentSlot = null;

        // 4) Retire proprement de la liste inventaire
        InventoryManager.Instance?.DetachWithoutDestroy(itemUI);

        // 5) Équipe via OnDrop (garde toute ta logique visuelle)
        slot.OnDrop(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        {
            pointerDrag = itemUI.gameObject
        });

        // 6) Sons (inchangé)
        if (data.equipSlotType == EquipSlotType.Primary || data.equipSlotType == EquipSlotType.Secondary)
            InventoryAudioManager.Instance.Play("equip_weapon");
        else if (data.equipSlotType == EquipSlotType.Armor || data.equipSlotType == EquipSlotType.Helmet || data.equipSlotType == EquipSlotType.Legging)
            InventoryAudioManager.Instance.Play("equip_armor");

        itemUI.Owner = ItemUI.ItemOwner.Equipment;

        Debug.Log($"[Equip] {data.itemName} équipé dans {slot.name}");
        return true;
    }

    public void UnequipItem(ItemUI itemUI)
    {
        if (itemUI == null) return;

        var slot = equipSlots.FirstOrDefault(s => s != null && s.CurrentItem == itemUI);
        if (slot == null)
        {
            Debug.LogWarning($"[Unequip] Impossible de trouver le slot pour {itemUI.itemData.itemName}");
            return;
        }

        // 1) Vide le slot
        slot.UnequipItem();

        // 🔥 Forcer la désinstanciation si cet item est en main
        var hotbar = FindFirstObjectByType<HotbarManager>();
        if (hotbar != null && hotbar.playerEquipHandler != null)
            hotbar.playerEquipHandler.UnequipIfHolding(itemUI.itemData);
        else
            FindFirstObjectByType<PlayerEquipHandler>()?.UnequipIfHolding(itemUI.itemData);

        Debug.Log($"[Unequip] {itemUI.itemData.itemName} retiré du slot {slot.name}");

        // 2) Replace dans l'inventaire
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

    public bool TryMergeIntoEquipment(ItemUI source)
    {
        if (source == null || source.itemData == null) return false;
        if (!source.itemData.isStackable) return false;
        if (equipSlots == null || equipSlots.Length == 0) return false;

        bool merged = false;

        foreach (var eq in equipSlots)
        {
            if (eq == null || eq.CurrentItem == null) continue;

            var equipped = eq.CurrentItem;
            if (equipped.itemData == null || !equipped.itemData.isStackable) continue;

            // Comparaison tolérante
            bool sameType =
                equipped.itemData == source.itemData ||
                equipped.itemData.IsSameType(source.itemData) ||
                equipped.itemData.itemName == source.itemData.itemName ||
                (!string.IsNullOrEmpty(equipped.itemData.prefabName) &&
                 equipped.itemData.prefabName == source.itemData.prefabName);

            if (!sameType) continue;

            int space = equipped.itemData.maxStack - equipped.currentStack;
            if (space <= 0) continue;

            int moved = Mathf.Min(space, source.currentStack);
            if (moved <= 0) continue;

            equipped.currentStack += moved;
            equipped.UpdateStackText();

            source.currentStack -= moved;
            source.UpdateStackText();

            // rafraîchit le visuel du slot d’équipement
            eq.ForceRefreshVisual(equipped);

            Debug.Log($"[EquipMerge] +{moved} dans {eq.name} ({equipped.currentStack}/{equipped.itemData.maxStack})");
            merged = true;

            if (source.currentStack <= 0)
            {
                // pile source finie → on la supprime de l’inventaire
                InventoryManager.Instance.RemoveItem(source);
                break;
            }
        }

        return merged;
    }

    public void DebugPrintEquipment()
    {
        Debug.Log("=== Equipment Debug ===");
        if (equipSlots == null) { Debug.Log("equipSlots = null"); return; }
        for (int i = 0; i < equipSlots.Length; i++)
        {
            var s = equipSlots[i];
            var it = s != null ? s.CurrentItem : null;
            string txt = it == null ? "(vide)" : $"{it.itemData?.itemName} x{it.currentStack}";
            Debug.Log($"Slot[{i}] {(s ? s.name : "<null>")} -> {txt}");
        }
    }

    public ItemData GetEquippedMeleeWeapon()
    {
        foreach (var slot in equipSlots)
        {
            if (slot?.CurrentItem == null) continue;
            var data = slot.CurrentItem.itemData;
            if (data != null && data.isMeleeWeapon)
                return data;
        }
        return null;
    }
}
