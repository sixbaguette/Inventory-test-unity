using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ItemUI itemUI;

    private Transform originalParent;
    private Canvas overlayCanvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private bool isDragging = false;
    private Vector2Int lastValidPos;
    private Vector2Int lastValidSize;

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

    void Update()
    {
        if (isDragging && Input.GetKeyDown(KeyCode.R))
        {
            RotateDuringDrag();
        }
    }

    private void RotateDuringDrag()
    {
        if (itemUI == null || itemUI.itemData == null) return;

        // swap width/height sur LA COPIE runtime
        int oldW = itemUI.itemData.width;
        int oldH = itemUI.itemData.height;
        itemUI.itemData.width = oldH;
        itemUI.itemData.height = oldW;

        // recalcul visuel — l’icône et l’outline suivent si configurés en stretch (voir ci-dessous)
        itemUI.UpdateSize();
        itemUI.UpdateOutline();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (overlayCanvas == null || itemUI == null) return;

        isDragging = true;

        // détache d’un slot d’équipement si besoin
        var equipSlot = GetComponentInParent<EquipementSlot>();
        if (equipSlot != null) equipSlot.ForceClear(itemUI);

        originalParent = transform.parent;
        transform.SetParent(overlayCanvas.transform, true);
        transform.SetAsLastSibling();

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out startMousePos
        );

        // Sauvegarde la position et la taille actuelles avant de déplacer
        if (itemUI.currentSlot != null)
        {
            lastValidPos = new Vector2Int(itemUI.currentSlot.x, itemUI.currentSlot.y);
            lastValidSize = new Vector2Int(itemUI.itemData.width, itemUI.itemData.height);
        }

        startItemPos = rectTransform.anchoredPosition;
        dragOffset = startItemPos - startMousePos;

        // Recalage visuel
        itemUI.UpdateSize();
        itemUI.UpdateOutline();
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
        isDragging = false;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;

        var inv = InventoryManager.Instance;
        if (inv == null || itemUI == null || itemUI.itemData == null) return;

        // 1️⃣ Si on lâche sur un slot d’équipement → priorité
        EquipementSlot equipSlot = null;
        if (eventData.pointerCurrentRaycast.gameObject != null)
            equipSlot = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<EquipementSlot>();

        if (equipSlot != null && equipSlot.IsCompatible(itemUI.itemData))
        {
            equipSlot.OnDrop(eventData);

            if (inv.slots != null)
            {
                foreach (var s in inv.slots)
                    if (s != null) s.ResetHighlight();
            }
            return;
        }

        // 2️⃣ ✅ Fusion automatique si on lâche sur un autre item identique
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            ItemUI targetUI = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<ItemUI>();
            if (targetUI != null && targetUI != itemUI)
            {
                if (InventoryManager.Instance.TryMergeStacks(itemUI, targetUI))
                {
                    // fusion faite → on sort
                    if (inv.slots != null)
                    {
                        foreach (var s in inv.slots)
                            if (s != null) s.ResetHighlight();
                    }
                    return;
                }
            }
        }

        // 3️⃣ Sinon snap grille par slot le plus proche (en écran)
        if (inv.slots == null || inv.slotParent == null) return;

        foreach (var s in inv.slots)
            if (s != null) s.ResetHighlight();

        var rootCanvas = inv.slotParent.GetComponentInParent<Canvas>();
        Camera cam = rootCanvas != null ? rootCanvas.worldCamera : null;

        Vector2 pointerScreen = eventData.position;
        float bestDist = float.MaxValue;
        int bestX = -1, bestY = -1;

        // Recherche du slot le plus proche de la souris (en coordonnées écran)
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
            // ❌ Placement échoué : on restaure l’état précédent
            Debug.Log("[OnEndDrag] Placement impossible, retour position/rotation initiale");

            // Reviens à la rotation d’origine
            if (itemUI.itemData.width != lastValidSize.x || itemUI.itemData.height != lastValidSize.y)
            {
                int tmp = itemUI.itemData.width;
                itemUI.itemData.width = itemUI.itemData.height;
                itemUI.itemData.height = tmp;
            }

            // Replace à sa position d’origine
            inv.PlaceItem(itemUI, lastValidPos.x, lastValidPos.y);
        }
        else
        {
            // ✅ placement réussi, on met à jour la dernière position connue
            lastValidPos = new Vector2Int(startX, startY);
            lastValidSize = new Vector2Int(itemUI.itemData.width, itemUI.itemData.height);
        }

        canvasGroup.blocksRaycasts = true;
    }

}
