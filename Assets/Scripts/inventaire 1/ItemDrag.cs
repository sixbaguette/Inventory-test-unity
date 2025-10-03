using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
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

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
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
