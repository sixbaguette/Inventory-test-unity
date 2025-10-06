using UnityEngine.EventSystems;
using UnityEngine;

public class SlotDrop : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int x;
    public int y;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        ItemUI draggedItemUI = eventData.pointerDrag.GetComponent<ItemUI>();
        if (draggedItemUI == null || draggedItemUI.itemData == null) return;

        Item item = draggedItemUI.itemData;
        bool canPlace = InventoryManager.Instance.CanPlaceItem(x, y, item, draggedItemUI);

        // Colorier tous les slots occupés
        for (int dx = 0; dx < item.width; dx++)
        {
            for (int dy = 0; dy < item.height; dy++)
            {
                int checkX = x + dx;
                int checkY = y + dy;

                if (checkX >= InventoryManager.Instance.width || checkY >= InventoryManager.Instance.height)
                    continue;

                InventoryManager.Instance.slots[checkX, checkY].Highlight(canPlace ? Color.green : Color.red);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        ItemUI draggedItemUI = eventData.pointerDrag.GetComponent<ItemUI>();
        if (draggedItemUI == null || draggedItemUI.itemData == null) return;

        Item item = draggedItemUI.itemData;

        for (int dx = 0; dx < item.width; dx++)
        {
            for (int dy = 0; dy < item.height; dy++)
            {
                int checkX = x + dx;
                int checkY = y + dy;

                if (checkX >= InventoryManager.Instance.width || checkY >= InventoryManager.Instance.height)
                    continue;

                InventoryManager.Instance.slots[checkX, checkY].ResetHighlight();
            }
        }
    }
}
