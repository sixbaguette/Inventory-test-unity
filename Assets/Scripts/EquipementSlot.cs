using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipementSlot : MonoBehaviour, IDropHandler
{
    public string slotType; // ex : "Weapon", "Armor"
    public Image iconDisplay;

    public ItemUI CurrentItem => currentItem;

    private ItemUI currentItem;

    public bool IsCompatible(Item item)
    {
        return item != null && item.equipmentType == slotType;
    }

    public void OnDrop(PointerEventData eventData)
    {
        var draggedItemUI = eventData.pointerDrag?.GetComponent<ItemUI>();
        if (draggedItemUI == null) return;

        var item = draggedItemUI.itemData;
        if (!IsCompatible(item)) return;

        EquipItem(draggedItemUI);
    }

    private void EquipItem(ItemUI itemUI)
    {
        currentItem = itemUI;

        itemUI.StoreOriginalState(); // 🔹 Stocke l’état initial

        itemUI.transform.SetParent(transform, false);
        itemUI.rectTransform.anchoredPosition = Vector2.zero;

        if (iconDisplay != null)
            iconDisplay.sprite = itemUI.itemData.icon;

        itemUI.UpdateSize(); // Redimensionne
        itemUI.UpdateOutline(); // Met à jour outline
    }



    public void UnequipItem()
    {
        if (currentItem == null) return;

        currentItem.RestoreOriginalState(); // 🔹 Restore l’état original

        InventoryManager.Instance.AddItem(currentItem.itemData);
        currentItem = null;

        if (iconDisplay != null)
            iconDisplay.sprite = null;
    }
}
