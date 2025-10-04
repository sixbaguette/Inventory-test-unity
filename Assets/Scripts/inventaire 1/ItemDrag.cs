using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Canvas overlayCanvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Canvas canvas;
    private ItemUI itemUI;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        itemUI = GetComponent<ItemUI>();

        // Trouve le Canvas Overlay
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in canvases)
        {
            if (c.gameObject.name == "ItemOverlayCanvas")
            {
                overlayCanvas = c;
                break;
            }
        }

        if (overlayCanvas == null)
            Debug.LogError("ItemOverlayCanvas introuvable dans la scène !");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;

        transform.SetParent(overlayCanvas.transform, true); // Passe au-dessus
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;

        // Efface les highlights précédents
        foreach (var slot in InventoryManager.Instance.slots)
            slot.ResetHighlight();

        // Vérifie sous la souris
        GameObject hovered = eventData.pointerCurrentRaycast.gameObject;
        SlotDrop targetSlot = null;

        if (hovered != null)
        {
            targetSlot = hovered.GetComponent<SlotDrop>();

            // Si on est sur un ItemUI, on prend son parent SlotDrop
            if (targetSlot == null)
            {
                ItemUI hoveredItemUI = hovered.GetComponentInParent<ItemUI>();
                if (hoveredItemUI != null && hoveredItemUI.currentSlot != null)
                {
                    targetSlot = hoveredItemUI.currentSlot.GetComponent<SlotDrop>();
                }
            }
        }

        if (targetSlot != null)
        {
            Item item = itemUI.itemData;

            int startX = Mathf.Clamp(targetSlot.x, 0, InventoryManager.Instance.width - item.width);
            int startY = Mathf.Clamp(targetSlot.y, 0, InventoryManager.Instance.height - item.height);

            bool canPlace = InventoryManager.Instance.CanPlaceItem(startX, startY, item, itemUI);

            for (int y = 0; y < item.height; y++)
            {
                for (int x = 0; x < item.width; x++)
                {
                    int checkX = startX + x;
                    int checkY = startY + y;

                    if (checkX < InventoryManager.Instance.width && checkY < InventoryManager.Instance.height)
                    {
                        Slot slot = InventoryManager.Instance.slots[checkX, checkY];
                        slot.Highlight(canPlace ? Color.green : Color.red);
                    }
                }
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        foreach (var slot in InventoryManager.Instance.slots)
            slot.ResetHighlight();

        SlotDrop targetSlot = null;

        // Chercher un SlotDrop sous la souris
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            var go = eventData.pointerCurrentRaycast.gameObject;
            targetSlot = go.GetComponentInParent<SlotDrop>();
        }

        bool placed = false;

        if (targetSlot != null)
        {
            placed = InventoryManager.Instance.PlaceItem(itemUI, targetSlot.x, targetSlot.y);
        }

        if (!placed)
        {
            // Si drop invalide → retour à la position initiale
            transform.SetParent(originalParent, false);
            rectTransform.anchoredPosition = Vector2.zero;
        }

        canvasGroup.blocksRaycasts = true;
    }
}
