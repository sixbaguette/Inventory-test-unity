using UnityEngine.EventSystems;
using UnityEngine;

public class SlotDrop : MonoBehaviour, IDropHandler
{
    public int x;
    public int y;

    public void OnDrop(PointerEventData eventData)
    {
        ItemDrag draggedItem = eventData.pointerDrag?.GetComponent<ItemDrag>();
        if (draggedItem != null)
        {
            InventoryManager.Instance.PlaceItem(draggedItem.GetComponent<ItemUI>(), x, y);
        }
    }
}
