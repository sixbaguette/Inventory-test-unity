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
    private bool isDragging = false;
    private Vector2Int lastValidPos;
    private Vector2Int lastValidSize;

    private Vector2 dragOffset;   // souris → centre de l’objet
    private Vector2 startMousePos;
    private Vector2 startItemPos;
    private int preDragX = -1, preDragY = -1;

    // 🧩 Nouveaux champs pour la gestion multi-inventaires
    public InventoryManager sourcePlayerInv;
    public ContainerInventoryManager sourceContainerInv;

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

        // swap logique
        int w = itemUI.itemData.width;
        int h = itemUI.itemData.height;
        itemUI.itemData.width = h;
        itemUI.itemData.height = w;

        // MAJ visuelle
        itemUI.UpdateSize();
        itemUI.UpdateOutline();
        itemUI.ResetVisualLayout();  // <- clé : remet icon/outline centrés à 0°

        // feedback visuel (optionnel, pas de rotation)
        LeanTween.cancel(itemUI.gameObject);
        itemUI.transform.localScale = Vector3.one;
        LeanTween.scale(itemUI.gameObject, Vector3.one * 1.05f, 0.08f)
                 .setEaseOutBack()
                 .setOnComplete(() => itemUI.transform.localScale = Vector3.one);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (overlayCanvas == null || itemUI == null) return;

        isDragging = true;

        // 🧠 Détermine depuis quel inventaire vient l'item
        sourcePlayerInv = GetComponentInParent<InventoryManager>();
        sourceContainerInv = GetComponentInParent<ContainerInventoryManager>();

        // détache d’un slot d’équipement si besoin
        var equipSlot = GetComponentInParent<EquipementSlot>();
        if (equipSlot != null) equipSlot.ForceClear(itemUI);

        originalParent = transform.parent;
        transform.SetParent(overlayCanvas.transform, true);
        // s'assure que l'item ne bloque pas les raycasts pendant le drag
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
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

        // sauve la dernière case valide
        if (itemUI != null && itemUI.currentSlot != null)
        {
            preDragX = itemUI.currentSlot.x;
            preDragY = itemUI.currentSlot.y;
        }
        else
        {
            preDragX = preDragY = -1;
        }

        // Recalage visuel
        itemUI.UpdateSize();
        itemUI.UpdateOutline();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (overlayCanvas == null || rectTransform == null || itemUI == null || itemUI.itemData == null)
            return;

        // 1️⃣ Suivre la souris dans l’overlay
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out var localMouse))
            return;

        rectTransform.anchoredPosition = localMouse + dragOffset;

        // 2️⃣ Si on survole un slot d’équipement → pas de highlight de grille
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

        // 🧭 Vérifie si la souris est sur la grille du conteneur actif
        var contUI = ContainerUIController.Instance;
        var contInv = contUI != null ? contUI.GetActiveContainerInventory() : null;

        if (contInv != null && contInv.slotParent != null)
        {
            var contRect = contInv.slotParent as RectTransform;
            if (RectTransformUtility.RectangleContainsScreenPoint(
                contRect, eventData.position, eventData.pressEventCamera))
            {
                // reset les highlights du joueur
                var playerInv = InventoryManager.Instance;
                if (playerInv?.slots != null)
                    foreach (var s in playerInv.slots) if (s != null) s.ResetHighlight();

                // 🎯 survol du conteneur → highlight dessus
                HighlightContainerGrid(contInv, eventData);
                return;
            }
        }

        // 🧭 Sinon, highlight sur l’inventaire du joueur
        var inv = InventoryManager.Instance;
        if (inv == null || inv.slots == null || inv.slotParent == null)
            return;

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

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // Réactive les raycasts pour pouvoir recliquer
        canvasGroup.blocksRaycasts = true;

        // --- Récupère les références principales
        var playerInv = InventoryManager.Instance;
        var openUI = ContainerUIController.Instance;
        var openContainer = openUI != null ? openUI.GetActiveContainerInventory() : null;

        bool placed = false;

        // 1️⃣ Si un conteneur est ouvert → essaie d’y placer l’objet
        if (openContainer != null && openContainer.slotParent != null)
        {
            if (TryPlaceInContainerByMath(openContainer, eventData.position))
            {
                // Si l’item venait du joueur → on le détache proprement sans le détruire
                if (sourcePlayerInv != null)
                    sourcePlayerInv.DetachWithoutDestroy(itemUI);

                placed = true;
            }
        }

        // 2️⃣ Sinon, essaie la grille du joueur
        if (!placed && playerInv != null && playerInv.slotParent != null)
        {
            if (TryPlaceInPlayerByMath(playerInv, eventData.position))
            {
                // Si l’item venait du conteneur → on le détache proprement sans le détruire
                if (sourceContainerInv != null)
                    sourceContainerInv.DetachWithoutDestroy(itemUI);

                placed = true;
            }
        }

        // 3️⃣ Si rien n’a fonctionné → retour à la dernière case valide
        if (!placed)
            ReturnToLastValid();

        // 🧹 Reset des highlights sur les deux grilles
        ClearAllHighlights();

        // ✅ Réactive le raycast sur l'ItemUI après le drop
        if (itemUI != null)
        {
            itemUI.EnableRaycastAfterDrop();
            itemUI.transform.SetAsLastSibling(); // <- très important
            var cg = itemUI.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.blocksRaycasts = true;
                cg.interactable = true;
                cg.alpha = 1f;
            }
        }
    }

    private bool TryPlaceInPlayerByMath(InventoryManager inv, Vector2 screenPos)
    {
        var grid = inv.slotParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (grid == null || inv.slots == null) return false;

        RectTransform panel = inv.slotParent as RectTransform;
        if (panel == null) return false;

        // Conversion écran -> local dans le GridPanel
        Vector2 local;
        var rootCanvas = panel.GetComponentInParent<Canvas>();
        Camera cam = rootCanvas != null ? rootCanvas.worldCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(panel, screenPos, cam, out local))
            return false;

        // Le GridLayout est généralement ancré top-left : il faut un repère avec (0,0) en haut-gauche
        // Avec un RectTransform ancré à gauche/haut, local.y est négatif vers le bas :
        // On convertit en y positif vers le bas :
        Vector2 size = panel.rect.size;
        float xFromLeft = local.x + (size.x * panel.pivot.x);
        float yFromTop = (size.y * (1f - panel.pivot.y)) - local.y;

        float cellW = grid.cellSize.x;
        float cellH = grid.cellSize.y;
        float spacingX = grid.spacing.x;
        float spacingY = grid.spacing.y;
        float padL = grid.padding.left;
        float padT = grid.padding.top;

        // Enlève padding
        float px = xFromLeft - padL;
        float py = yFromTop - padT;

        if (px < 0f || py < 0f) { ReturnToLastValid(); return true; }

        int cellX = Mathf.FloorToInt(px / (cellW + spacingX));
        int cellY = Mathf.FloorToInt(py / (cellH + spacingY));

        // Clamp pour item multicases
        int startX = Mathf.Clamp(cellX, 0, inv.width - itemUI.itemData.width);
        int startY = Mathf.Clamp(cellY, 0, inv.height - itemUI.itemData.height);

        // Depuis où vient l’item ?
        if (sourceContainerInv != null) // Conteneur -> Joueur
        {
            if (inv.CanPlaceItem(startX, startY, itemUI.itemData, itemUI))
            {
                bool placed = inv.PlaceItem(itemUI, startX, startY);
                if (placed)
                {
                    sourceContainerInv.RemoveItem(itemUI);
                    return true;
                }
            }
            ReturnToLastValid();
            return true;
        }
        else // Déplacement interne inventaire joueur
        {
            if (inv.CanPlaceItem(startX, startY, itemUI.itemData, itemUI))
            {
                bool placed = inv.PlaceItem(itemUI, startX, startY);
                if (!placed) ReturnToLastValid();
                return true;
            }
            ReturnToLastValid();
            return true;
        }
    }

    private bool TryPlaceInContainerByMath(ContainerInventoryManager cont, Vector2 screenPos)
    {
        var grid = cont.slotParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (grid == null || cont.slots == null) return false;

        RectTransform panel = cont.slotParent as RectTransform;
        if (panel == null) return false;

        // Conversion écran -> local dans le GridPanel du conteneur
        Vector2 local;
        var rootCanvas = panel.GetComponentInParent<Canvas>();
        Camera cam = rootCanvas != null ? rootCanvas.worldCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(panel, screenPos, cam, out local))
            return false;

        Vector2 size = panel.rect.size;
        float xFromLeft = local.x + (size.x * panel.pivot.x);
        float yFromTop = (size.y * (1f - panel.pivot.y)) - local.y;

        float cellW = grid.cellSize.x;
        float cellH = grid.cellSize.y;
        float spacingX = grid.spacing.x;
        float spacingY = grid.spacing.y;
        float padL = grid.padding.left;
        float padT = grid.padding.top;

        float px = xFromLeft - padL;
        float py = yFromTop - padT;

        if (px < 0f || py < 0f) return false;

        int cellX = Mathf.FloorToInt(px / (cellW + spacingX));
        int cellY = Mathf.FloorToInt(py / (cellH + spacingY));

        int startX = Mathf.Clamp(cellX, 0, cont.width - itemUI.itemData.width);
        int startY = Mathf.Clamp(cellY, 0, cont.height - itemUI.itemData.height);

        if (sourcePlayerInv != null) // Joueur -> Conteneur
        {
            // IMPORTANT : on ne recrée pas un nouvel ItemUI, on déplace l’existant
            if (cont.CanPlaceItem(startX, startY, itemUI.itemData))
            {
                cont.PlaceItem(itemUI, startX, startY);
                sourcePlayerInv.RemoveItem(itemUI);
                return true;
            }
            ReturnToLastValid();
            return true;
        }
        else // Déplacement interne conteneur
        {
            if (cont.CanPlaceItem(startX, startY, itemUI.itemData))
            {
                cont.PlaceItem(itemUI, startX, startY);
                return true;
            }
            ReturnToLastValid();
            return true;
        }
    }

    private void ReturnToLastValid()
    {
        if (itemUI.currentSlot != null)
        {
            if (sourcePlayerInv != null)
                sourcePlayerInv.PlaceItem(itemUI, itemUI.currentSlot.x, itemUI.currentSlot.y);
            else if (sourceContainerInv != null)
                sourceContainerInv.PlaceItem(itemUI, itemUI.currentSlot.x, itemUI.currentSlot.y);
        }
    }

    // 🧩 appelée par le conteneur après placement
    public void SetSourceContainer(ContainerInventoryManager cont)
    {
        sourcePlayerInv = null;
        sourceContainerInv = cont;
    }

    private void HighlightContainerGrid(ContainerInventoryManager cont, PointerEventData eventData)
    {
        var grid = cont.slotParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (grid == null) return;

        RectTransform panel = cont.slotParent as RectTransform;
        if (panel == null) return;

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panel,
            eventData.position,
            eventData.pressEventCamera,
            out local);

        Vector2 size = panel.rect.size;
        float xFromLeft = local.x + (size.x * panel.pivot.x);
        float yFromTop = (size.y * (1f - panel.pivot.y)) - local.y;

        float cellW = grid.cellSize.x;
        float cellH = grid.cellSize.y;
        float spacingX = grid.spacing.x;
        float spacingY = grid.spacing.y;
        float padL = grid.padding.left;
        float padT = grid.padding.top;

        float px = xFromLeft - padL;
        float py = yFromTop - padT;
        if (px < 0f || py < 0f) return;

        int cellX = Mathf.FloorToInt(px / (cellW + spacingX));
        int cellY = Mathf.FloorToInt(py / (cellH + spacingY));

        int startX = Mathf.Clamp(cellX, 0, cont.width - itemUI.itemData.width);
        int startY = Mathf.Clamp(cellY, 0, cont.height - itemUI.itemData.height);

        bool canPlace = cont.CanPlaceItem(startX, startY, itemUI.itemData);

        foreach (var s in cont.slots)
            if (s != null) s.ResetHighlight();

        for (int dx = 0; dx < itemUI.itemData.width; dx++)
        {
            for (int dy = 0; dy < itemUI.itemData.height; dy++)
            {
                int cx = startX + dx;
                int cy = startY + dy;
                if (cx < 0 || cy < 0 || cx >= cont.width || cy >= cont.height) continue;
                cont.slots[cx, cy].Highlight(canPlace ? Color.green : Color.red);
            }
        }
    }

    private void ClearAllHighlights()
    {
        var inv = InventoryManager.Instance;
        if (inv?.slots != null)
        {
            foreach (var s in inv.slots)
                s?.ResetHighlight();
        }

        var cont = ContainerUIController.Instance?.GetActiveContainerInventory();
        if (cont?.slots != null)
        {
            foreach (var s in cont.slots)
                s?.ResetHighlight();
        }
    }
}
