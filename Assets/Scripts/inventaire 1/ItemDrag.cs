using System.Collections.Generic;
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

        // Recherche le canvas du DragCanvas
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.name == "Canvas" && c.transform.parent != null && c.transform.parent.name == "DragCanvas")
            {
                overlayCanvas = c;
                break;
            }
        }

        // fallback au premier canvas trouvé (au cas où)
        if (overlayCanvas == null)
            overlayCanvas = FindFirstObjectByType<Canvas>();
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        // ✅ Libère le slot équipement si l’item vient d’un slot équipement
        EquipementSlot oldEquipSlot = GetComponentInParent<EquipementSlot>();
        if (oldEquipSlot != null)
        {
            oldEquipSlot.ForceClear(itemUI);
        }

        originalParent = transform.parent;
        transform.SetParent(overlayCanvas.transform, true);
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;

        CanvasGroup cg = itemUI.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = false;

        // Trouver le slot le plus proche de la souris (pour le drag offset)
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
        if (!enabled) return;
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
        if (!enabled) return;

        // Réinitialise les highlights
        foreach (var slot in InventoryManager.Instance.slots)
            slot.ResetHighlight();

        // Réactive les raycasts sur cet item
        canvasGroup.blocksRaycasts = true;

        // --- 1️⃣ Vérifie si on lâche sur un slot d’équipement ---
        EquipementSlot equipSlot = eventData.pointerCurrentRaycast.gameObject
            ? eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<EquipementSlot>()
            : null;

        if (equipSlot != null)
        {
            // Appelle directement la logique de placement d’équipement
            equipSlot.OnDrop(eventData);
            return;
        }

        // --- 2️⃣ Vérifie si on lâche sur un slot de grille ---
        SlotDrop targetSlot = eventData.pointerCurrentRaycast.gameObject
            ? eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<SlotDrop>()
            : null;

        // Si aucun objet sous la souris, on tente un raycast manuel (plus fiable)
        if (targetSlot == null)
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                targetSlot = result.gameObject.GetComponentInParent<SlotDrop>();
                if (targetSlot != null) break;
            }
        }

        bool placed = false;

        // --- 3️⃣ Si on a bien trouvé un slot valide, on tente le placement ---
        if (targetSlot != null && itemUI != null)
        {
            placed = InventoryManager.Instance.PlaceItem(itemUI, targetSlot.x, targetSlot.y);
        }

        // --- 4️⃣ Si aucun placement valide, on remet l’objet à sa place d’origine ---
        if (!placed && itemUI.currentSlot != null)
        {
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
