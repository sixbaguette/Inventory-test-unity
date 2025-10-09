using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipementSlot : MonoBehaviour, IDropHandler
{
    public string slotType; // ex "Weapon", "Armor", or empty = accept any
    public Image iconDisplay;

    private ItemUI currentItem;
    public ItemUI CurrentItem => currentItem;

    public bool IsCompatible(ItemData item)
    {
        if (item == null) return false;
        if (string.IsNullOrEmpty(slotType)) return true;
        return item.equipmentType == slotType;
    }

    public void OnDrop(PointerEventData eventData)
    {
        var droppedUI = eventData.pointerDrag?.GetComponent<ItemUI>();
        if (droppedUI == null) return;

        if (!IsCompatible(droppedUI.itemData)) return;
        if (currentItem != null) { Debug.Log("Slot déjà occupé"); return; }

        EquipItem(droppedUI);
    }

    private void EquipItem(ItemUI itemUI)
    {
        if (itemUI == null) return;

        // Clear inventory slots occupied by this item (it is removed from inventory)
        if (itemUI.occupiedSlots != null)
        {
            foreach (var s in itemUI.occupiedSlots) if (s != null) s.ClearItem();
        }

        itemUI.StoreOriginalState();

        currentItem = itemUI;

        // parent -> this slot
        itemUI.transform.SetParent(transform, false);

        // center & size
        RectTransform it = itemUI.rectTransform;
        RectTransform sr = GetComponent<RectTransform>();
        it.anchorMin = new Vector2(0.5f, 0.5f);
        it.anchorMax = new Vector2(0.5f, 0.5f);
        it.pivot = new Vector2(0.5f, 0.5f);
        it.anchoredPosition = Vector2.zero;
        it.sizeDelta = sr.sizeDelta;

        if (itemUI.outline != null)
        {
            var or = itemUI.outline.rectTransform;
            or.anchorMin = new Vector2(0.5f, 0.5f);
            or.anchorMax = new Vector2(0.5f, 0.5f);
            or.pivot = new Vector2(0.5f, 0.5f);
            or.anchoredPosition = Vector2.zero;
            or.sizeDelta = sr.sizeDelta;
        }

        if (iconDisplay != null) iconDisplay.enabled = false;
    }

    public void UnequipItem()
    {
        if (currentItem == null) return;

        // essaye de remettre dans l'inventaire au premier emplacement libre
        if (InventoryManager.Instance.FindFirstFreePosition(currentItem.itemData, out int x, out int y))
        {
            // PlaceItem remet parent et taille correctement
            InventoryManager.Instance.PlaceItem(currentItem, x, y);
        }
        else
        {
            // pas de place : restaure à l'état original (parent + size)
            currentItem.RestoreOriginalState();
        }

        currentItem = null;

        if (iconDisplay != null) iconDisplay.enabled = true;
    }

    // utilitaire appelé depuis ItemDrag si on commence un drag depuis ce slot
    public void ForceClear(ItemUI itemUI)
    {
        if (currentItem == itemUI) UnequipItem();
    }
}
