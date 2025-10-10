using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ItemUI itemUI;

    private Transform originalParent;
    private Canvas overlayCanvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Vector2 dragOffset;   // souris → centre de l’objet
    private Vector2 startMousePos;
    private Vector2 startItemPos;

    private void Awake()
    {
        itemUI = GetComponent<ItemUI>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        overlayCanvas = InventoryManager.Instance.overlayCanvas; // canvas de drag
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (overlayCanvas == null || itemUI == null) return;

        // ✅ si l’item vient d’un slot d’équipement, on le détache proprement
        // ✅ si l’item vient d’un slot d’équipement, on le détache proprement
        var equipSlot = GetComponentInParent<EquipementSlot>();
        if (equipSlot != null)
        {
            equipSlot.ForceClear(itemUI);
        }

        originalParent = transform.parent;
        transform.SetParent(overlayCanvas.transform, true);
        transform.SetAsLastSibling();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;

        // ✅ calcule offset souris → item
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out startMousePos
        );

        startItemPos = rectTransform.anchoredPosition;
        dragOffset = startItemPos - startMousePos;
    }


    public void OnDrag(PointerEventData eventData)
    {
        if (overlayCanvas == null || rectTransform == null || itemUI == null || itemUI.itemData == null)
            return;

        // 1) Suivre la souris dans l’overlay
        Vector2 localMouse;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localMouse))
            return;

        rectTransform.anchoredPosition = localMouse + dragOffset;

        // 2) Si on survole un slot d’équipement → pas de highlight de grille
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            var overEquip = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<EquipementSlot>();
            if (overEquip != null)
            {
                var clearInv = InventoryManager.Instance;
                if (clearInv?.slots != null)
                    foreach (var s in clearInv.slots) if (s != null) s.ResetHighlight();
                return;
            }
        }

        // 3) Highlight grille par "slot le plus proche" en coordonnées écran
        var inv = InventoryManager.Instance;
        if (inv == null || inv.slots == null || inv.slotParent == null) return;

        foreach (var s in inv.slots)
            if (s != null) s.ResetHighlight();

        // Canvas racine de la grille pour conversion World->Screen
        var rootCanvas = inv.slotParent.GetComponentInParent<Canvas>();
        Camera cam = rootCanvas != null ? rootCanvas.worldCamera : null;

        // Trouver le slot dont le centre écran est le plus proche du pointeur
        Vector2 pointerScreen = eventData.position;
        float bestDist = float.MaxValue;
        int bestX = -1, bestY = -1;

        for (int y = 0; y < inv.height; y++)
        {
            for (int x = 0; x < inv.width; x++)
            {
                var rt = inv.slots[x, y].GetComponent<RectTransform>();
                // centre du slot en écran
                Vector3 centerWorld = rt.TransformPoint(rt.rect.center);
                Vector2 centerScreen = RectTransformUtility.WorldToScreenPoint(cam, centerWorld);

                float d = Vector2.Distance(pointerScreen, centerScreen);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestX = x;
                    bestY = y;
                }
            }
        }

        if (bestX < 0 || bestY < 0) return;

        // Ajuster pour que l’item multi-cases tienne dans la grille (top-left de l’item)
        int startX = Mathf.Clamp(bestX, 0, inv.width - itemUI.itemData.width);
        int startY = Mathf.Clamp(bestY, 0, inv.height - itemUI.itemData.height);

        bool canPlace = inv.CanPlaceItem(startX, startY, itemUI.itemData, itemUI);

        for (int dx = 0; dx < itemUI.itemData.width; dx++)
        {
            for (int dy = 0; dy < itemUI.itemData.height; dy++)
            {
                int cx = startX + dx;
                int cy = startY + dy;
                if (cx < 0 || cy < 0 || cx >= inv.width || cy >= inv.height) continue;
                inv.slots[cx, cy].Highlight(canPlace ? Color.green : Color.red);
            }
        }
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;

        var inv = InventoryManager.Instance;
        if (inv == null || itemUI == null || itemUI.itemData == null) return;

        // 1) Si on lâche sur un slot d’équipement → priorité
        EquipementSlot equipSlot = null;
        if (eventData.pointerCurrentRaycast.gameObject != null)
            equipSlot = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<EquipementSlot>();

        if (equipSlot != null && equipSlot.IsCompatible(itemUI.itemData))
        {
            equipSlot.OnDrop(eventData);
            if (inv.slots != null)
                foreach (var s in inv.slots) if (s != null) s.ResetHighlight();
            return;
        }

        // 2) Sinon snap grille par slot le plus proche (en écran)
        if (inv.slots == null || inv.slotParent == null) return;

        foreach (var s in inv.slots)
            if (s != null) s.ResetHighlight();

        var rootCanvas = inv.slotParent.GetComponentInParent<Canvas>();
        Camera cam = rootCanvas != null ? rootCanvas.worldCamera : null;

        Vector2 pointerScreen = eventData.position;
        float bestDist = float.MaxValue;
        int bestX = -1, bestY = -1;

        for (int y = 0; y < inv.height; y++)
        {
            for (int x = 0; x < inv.width; x++)
            {
                var rt = inv.slots[x, y].GetComponent<RectTransform>();
                Vector3 centerWorld = rt.TransformPoint(rt.rect.center);
                Vector2 centerScreen = RectTransformUtility.WorldToScreenPoint(cam, centerWorld);

                float d = Vector2.Distance(pointerScreen, centerScreen);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestX = x;
                    bestY = y;
                }
            }
        }

        if (bestX < 0 || bestY < 0)
        {
            // pas de slot trouvé → on remet à la dernière position valide si connue
            if (itemUI.currentSlot != null)
                inv.PlaceItem(itemUI, itemUI.currentSlot.x, itemUI.currentSlot.y);
            return;
        }

        // Ajuste pour que l’item tienne dans la grille
        int startX = Mathf.Clamp(bestX, 0, inv.width - itemUI.itemData.width);
        int startY = Mathf.Clamp(bestY, 0, inv.height - itemUI.itemData.height);

        // Option : petit rayon max pour éviter les snaps trop loin (ex: 120 px)
        // if (bestDist > 120f) { if (itemUI.currentSlot != null) inv.PlaceItem(itemUI, itemUI.currentSlot.x, itemUI.currentSlot.y); return; }

        bool placed = inv.PlaceItem(itemUI, startX, startY);
        if (!placed)
        {
            // Essaie un mini recentrage autour du slot visé (utile près des bords/collisions)
            for (int ox = -itemUI.itemData.width; ox <= 0 && !placed; ox++)
            {
                for (int oy = -itemUI.itemData.height; oy <= 0 && !placed; oy++)
                {
                    int altX = Mathf.Clamp(bestX + ox, 0, inv.width - itemUI.itemData.width);
                    int altY = Mathf.Clamp(bestY + oy, 0, inv.height - itemUI.itemData.height);
                    if (inv.CanPlaceItem(altX, altY, itemUI.itemData, itemUI))
                        placed = inv.PlaceItem(itemUI, altX, altY);
                }
            }

            if (!placed && itemUI.currentSlot != null)
                inv.PlaceItem(itemUI, itemUI.currentSlot.x, itemUI.currentSlot.y);
        }

        canvasGroup.blocksRaycasts = true;
    }
}
