using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ItemUI itemUI;
    private Transform originalParent;
    private Canvas overlayCanvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Slot dragOffsetSlot; // Le slot sous la souris quand on commence le drag
    private Vector2 dragOffset;
    private Vector2 grabOffset; // Offset souris → centre de l’objet

    private void Awake()
    {
        itemUI = GetComponent<ItemUI>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        overlayCanvas = FindFirstObjectByType<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        transform.SetParent(overlayCanvas.transform, true);
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;

        // Trouver le slot le plus proche de la souris
        Slot nearestSlot = null;
        float minDist = float.MaxValue;

        foreach (var slot in itemUI.occupiedSlots)
        {
            float dist = Vector2.Distance(eventData.position, slot.GetComponent<RectTransform>().position);
            if (dist < minDist)
            {
                minDist = dist;
                nearestSlot = slot;
            }
        }

        if (nearestSlot != null)
        {
            dragOffsetSlot = nearestSlot;

            Vector2 localMousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localMousePos);
            dragOffset = rectTransform.localPosition - nearestSlot.GetComponent<RectTransform>().localPosition;
        }
    }


    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        rectTransform.localPosition = localPoint - grabOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        foreach (var slot in InventoryManager.Instance.slots)
            slot.ResetHighlight();

        canvasGroup.blocksRaycasts = true;

        // --- Détection si on lâche sur un slot d’équipement ---
        EquipementSlot equipSlot = null;
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            var go = eventData.pointerCurrentRaycast.gameObject;
            equipSlot = go.GetComponentInParent<EquipementSlot>();
        }

        if (equipSlot != null)
        {
            // Appelle directement la logique de placement d’équipement
            equipSlot.OnDrop(eventData);
            return;
        }

        // --- Sinon, on gère le drop dans la grille ---
        SlotDrop targetSlot = null;
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            var go = eventData.pointerCurrentRaycast.gameObject;
            targetSlot = go.GetComponentInParent<SlotDrop>();
        }

        bool placed = false;

        if (targetSlot != null && itemUI != null && itemUI.currentSlot != null)
        {
            placed = InventoryManager.Instance.PlaceItem(itemUI, targetSlot.x, targetSlot.y);
        }

        if (!placed && itemUI.currentSlot != null)
        {
            // Remet l’item à sa position d’origine si pas placé
            InventoryManager.Instance.PlaceItem(itemUI, itemUI.currentSlot.x, itemUI.currentSlot.y);
        }
    }



    private float slotWidth
    {
        get
        {
            RectTransform slotRect = InventoryManager.Instance.slots[0, 0].GetComponent<RectTransform>();
            return slotRect.sizeDelta.x;
        }
    }

    private float slotHeight
    {
        get
        {
            RectTransform slotRect = InventoryManager.Instance.slots[0, 0].GetComponent<RectTransform>();
            return slotRect.sizeDelta.y;
        }
    }

}
