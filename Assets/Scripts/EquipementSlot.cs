using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipementSlot : MonoBehaviour, IDropHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public string slotType; // ex : "Weapon", "Armor"
    public Image iconDisplay;

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
        if (!IsCompatible(item)) return; // ❌ mauvais type d’item

        // ✅ Si le slot est vide
        if (currentItem == null)
        {
            EquipItem(draggedItemUI);
        }
        else
        {
            // ❌ déjà occupé, on pourrait swap ou refuser
            Debug.Log("Slot déjà occupé !");
        }
    }

    private void EquipItem(ItemUI itemUI)
    {
        currentItem = itemUI;
        itemUI.transform.SetParent(transform);
        itemUI.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        if (iconDisplay != null)
            iconDisplay.sprite = itemUI.itemData.icon;
    }

    public void UnequipItem()
    {
        if (currentItem == null) return;

        // Le replacer dans l’inventaire
        InventoryManager.Instance.AddItem(currentItem.itemData);
        currentItem = null;

        if (iconDisplay != null)
            iconDisplay.sprite = null;
    }

    // Pour pouvoir retirer l’équipement
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            currentItem.transform.SetParent(InventoryManager.Instance.slotParent);
            currentItem.transform.SetAsLastSibling();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Suivi de la souris
        if (currentItem != null)
            currentItem.GetComponent<RectTransform>().position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Si on ne l’a pas droppé ailleurs
        if (eventData.pointerCurrentRaycast.gameObject == null)
        {
            UnequipItem();
        }
    }
}
