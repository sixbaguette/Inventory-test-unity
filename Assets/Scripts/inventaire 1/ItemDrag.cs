using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.UI.Image;

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

    private InventoryManager originPlayerInv;
    private ContainerInventoryManager originContainerInv;
    private int originX = -1, originY = -1;
    private Vector2 originAnchoredPos;
    private Transform originParent;

    private enum GridType { None, Player, Container }

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
        if (itemUI == null) return;

        // 1️⃣ Sécurise overlayCanvas
        if (overlayCanvas == null)
            overlayCanvas = InventoryManager.Instance != null ? InventoryManager.Instance.overlayCanvas : null;

        if (overlayCanvas == null)
        {
            foreach (var c in GameObject.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c.isActiveAndEnabled && c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    overlayCanvas = c;
                    break;
                }
            }
        }

        if (overlayCanvas == null)
        {
            Debug.LogError("[ItemDrag] Aucun overlayCanvas trouvé ! Drag annulé.");
            return;
        }

        isDragging = true;

        // 2️⃣ Détermine la grille source (joueur ou conteneur)
        sourcePlayerInv = GetComponentInParent<InventoryManager>();
        sourceContainerInv = GetComponentInParent<ContainerInventoryManager>();

        // 🔹 On sauvegarde aussi les vraies origines pour le retour
        originPlayerInv = sourcePlayerInv;
        originContainerInv = sourceContainerInv;

        // 3️⃣ Libère un éventuel slot d’équipement
        var equipSlot = GetComponentInParent<EquipementSlot>();
        if (equipSlot != null)
            equipSlot.ForceClear(itemUI);

        // 4️⃣ Place l’item sous l’overlay (devant tout)
        originalParent = transform.parent;
        originParent = transform.parent; // 👈 pour ReturnToLastValid()
        transform.SetParent(overlayCanvas.transform, false);

        var topCanvas = overlayCanvas.GetComponent<Canvas>();
        if (topCanvas != null)
        {
            topCanvas.overrideSorting = true;
            topCanvas.sortingOrder = 999;
        }
        transform.SetAsLastSibling();

        // 5️⃣ Rend l’item non bloquant pendant le drag
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = true;

        // Tous les enfants graphiques ne bloquent pas non plus
        var graphics = GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphics)
            g.raycastTarget = false;

        // 6️⃣ Calcul de l’offset curseur
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out startMousePos
        );

        startItemPos = rectTransform.anchoredPosition;
        dragOffset = startItemPos - startMousePos;

        // 7️⃣ Sauvegarde des coordonnées et position d’origine
        if (itemUI.currentSlot != null)
        {
            lastValidPos = new Vector2Int(itemUI.currentSlot.x, itemUI.currentSlot.y);
            lastValidSize = new Vector2Int(itemUI.itemData.width, itemUI.itemData.height);

            preDragX = itemUI.currentSlot.x;
            preDragY = itemUI.currentSlot.y;

            originX = preDragX;
            originY = preDragY;
        }
        else
        {
            preDragX = preDragY = -1;
            originX = originY = -1;
        }

        originAnchoredPos = itemUI.rectTransform.anchoredPosition; // 👈 position exacte avant drag

        // 8️⃣ Mise à jour visuelle
        itemUI.UpdateSize();
        itemUI.UpdateOutline();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (overlayCanvas == null || rectTransform == null || itemUI == null || itemUI.itemData == null)
            return;

        // 1️⃣ Suivre la souris
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localMouse))
        {
            rectTransform.anchoredPosition = localMouse + dragOffset;
        }

        // 2️⃣ Récupère les deux grilles
        var inv = InventoryManager.Instance;
        var cont = ContainerUIController.Instance?.GetActiveContainerInventory();
        if (inv == null || inv.slots == null) return;

        // 3️⃣ Reset tous les highlights
        ClearAllHighlights();

        // 4️⃣ Détermine le slot le plus proche de la souris
        float bestDist = float.MaxValue;
        bool closestIsPlayer = false;
        int bestX = -1, bestY = -1;

        // Fonction locale pour tester un inventaire
        void TestGrid(Slot[,] grid, int width, int height, Transform slotParent, ref float bestDistRef, ref bool isPlayerRef, ref int bx, ref int by, bool player)
        {
            if (grid == null || slotParent == null) return;
            var rootCanvas = slotParent.GetComponentInParent<Canvas>();
            Camera cam = rootCanvas != null ? rootCanvas.worldCamera : null;
            Vector2 pointerScreen = eventData.position;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var rt = grid[x, y].GetComponent<RectTransform>();
                    Vector3 worldCenter = rt.TransformPoint(rt.rect.center);
                    Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);
                    float dist = Vector2.Distance(pointerScreen, screenCenter);
                    if (dist < bestDistRef)
                    {
                        bestDistRef = dist;
                        isPlayerRef = player;
                        bx = x;
                        by = y;
                    }
                }
            }
        }

        // Vérifie les deux grilles
        TestGrid(inv.slots, inv.width, inv.height, inv.slotParent, ref bestDist, ref closestIsPlayer, ref bestX, ref bestY, true);
        if (cont != null)
            TestGrid(cont.slots, cont.width, cont.height, cont.slotParent, ref bestDist, ref closestIsPlayer, ref bestX, ref bestY, false);

        // 5️⃣ Si aucun slot détecté → pas de highlight
        if (bestX < 0 || bestY < 0)
            return;

        // 6️⃣ Applique le highlight à la bonne grille
        if (closestIsPlayer)
        {
            DoHighlightForPlayer(inv, eventData.position);
        }
        else if (cont != null)
        {
            DoHighlightForContainer(cont, eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        canvasGroup.alpha = 1f;

        var playerInv = InventoryManager.Instance;
        var openContainer = ContainerUIController.Instance?.GetActiveContainerInventory();

        bool placed = false;

        // === 1️⃣ Priorité : conteneur si ouvert ===
        if (openContainer != null && openContainer.slotParent != null)
        {
            if (TryPlaceInContainerByMath(openContainer, eventData.position))
            {
                // Si la source était l'inventaire joueur → on le détache proprement
                if (sourcePlayerInv != null)
                {
                    sourcePlayerInv.DetachWithoutDestroy(itemUI);
                    sourcePlayerInv = null;
                }

                // Marque simplement la nouvelle source (pas besoin d'ajouter à une liste privée)
                sourceContainerInv = openContainer;
                sourcePlayerInv = null;

                placed = true;
            }
        }

        // === 2️⃣ Sinon tente le joueur ===
        if (!placed && playerInv != null && playerInv.slotParent != null)
        {
            if (TryPlaceInPlayerByMath(playerInv, eventData.position))
            {
                // Si la source était un conteneur → on le retire logiquement du container
                if (sourceContainerInv != null)
                {
                    sourceContainerInv.DetachWithoutDestroy(itemUI);
                    sourceContainerInv = null;
                }

                // 🧠 Important : ajoute l’item à la liste de l’inventaire du joueur
                playerInv.AddToInventoryList(itemUI);
                sourcePlayerInv = playerInv;

                placed = true;
            }
        }

        // === 3️⃣ Si pas placé nulle part → retour ===
        if (!placed)
            ReturnToLastValid();

        // === 4️⃣ Réactive les raycasts graphiques ===
        var graphics = GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphics)
            g.raycastTarget = true;

        // === 5️⃣ Restaure l'ordre du Canvas ===
        var topCanvas = overlayCanvas.GetComponent<Canvas>();
        if (topCanvas != null)
            topCanvas.overrideSorting = false;

        // === 6️⃣ Sécurité visuelle / interaction ===
        itemUI.transform.SetAsLastSibling();
        itemUI.EnableRaycastAfterDrop();
        itemUI.DisableExtraCanvasIfInInventory();

        // === 7️⃣ Stack prioritaire avec l'ItemUI sous la souris ===
        if (!TryMergeStackUnderPointer(eventData, itemUI))
        {
            // Fallback : on peut garder ton scan global si tu veux tenter d'autres fusions
            TryMergeStacksCrossInventories(InventoryManager.Instance,
                                           ContainerUIController.Instance?.GetActiveContainerInventory(),
                                           itemUI);
        }
        // ✅ Toujours nettoyer les highlights même si le stack s'est fait ou échoué
        ClearAllHighlights();
    }

    private void TryMergeStacksCrossInventories(InventoryManager playerInv, ContainerInventoryManager containerInv, ItemUI draggedItem)
    {
        if (draggedItem == null || draggedItem.itemData == null)
            return;

        // 🔸 Vérifie d’abord dans l’inventaire joueur
        foreach (var item in playerInv.GetComponentsInChildren<ItemUI>(true))
        {
            if (item == draggedItem) continue;
            if (playerInv.TryMergeStacks(draggedItem, item))
                return; // fusion réussie
        }

        // 🔸 Puis dans le conteneur s’il est ouvert
        if (containerInv != null)
        {
            foreach (var item in containerInv.GetComponentsInChildren<ItemUI>(true))
            {
                if (item == draggedItem) continue;
                if (playerInv.TryMergeStacks(draggedItem, item))
                    return;
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
        // 1) Essaye la vraie grille d’origine + coords sauvegardées
        if (originPlayerInv != null && originX >= 0 && originY >= 0)
        {
            if (originPlayerInv.PlaceItem(itemUI, originX, originY))
                return;
        }
        if (originContainerInv != null && originX >= 0 && originY >= 0)
        {
            if (originContainerInv.PlaceItem(itemUI, originX, originY))
                return;
        }

        // 2) Sinon, si l’item a un currentSlot valide, repose-le là
        if (itemUI.currentSlot != null)
        {
            var s = itemUI.currentSlot;
            if (originPlayerInv != null && ReferenceEquals(s, originPlayerInv.slots[s.x, s.y]))
            {
                if (originPlayerInv.PlaceItem(itemUI, s.x, s.y)) return;
            }
            if (originContainerInv != null && ReferenceEquals(s, originContainerInv.slots[s.x, s.y]))
            {
                if (originContainerInv.PlaceItem(itemUI, s.x, s.y)) return;
            }
        }

        // 3) Fallback total : reparent à son parent d’avant le drag et remets la position visuelle
        if (originParent != null)
            itemUI.transform.SetParent(originParent, false);

        itemUI.rectTransform.anchoredPosition = originAnchoredPos;

        // Et garantit qu’on peut re-cliquer
        var cg = itemUI.GetComponent<CanvasGroup>() ?? itemUI.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true; cg.interactable = true; cg.alpha = 1f;
        itemUI.EnableRaycastAfterDrop();
    }

    // 🧩 appelée par le conteneur après placement
    public void SetSourceContainer(ContainerInventoryManager cont)
    {
        sourcePlayerInv = null;
        sourceContainerInv = cont;
    }

    private void ClearAllHighlights()
    {
        var inv = InventoryManager.Instance;
        if (inv?.slots != null) foreach (var s in inv.slots) s?.ResetHighlight();

        var cont = ContainerUIController.Instance?.GetActiveContainerInventory();
        if (cont?.slots != null) foreach (var s in cont.slots) s?.ResetHighlight();
    }

    // ======== calc + reset + paint highlight ========
    private void DoHighlightForPlayer(InventoryManager inv, Vector2 screenPos)
    {
        if (inv == null || inv.slots == null || inv.slotParent == null) return;

        foreach (var s in inv.slots) if (s != null) s.ResetHighlight();

        var grid = inv.slotParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        var panel = inv.slotParent as RectTransform;
        if (grid == null || panel == null) return;

        Vector2 local;
        // 🧠 On récupère automatiquement la bonne caméra du canvas parent
        var rootCanvas = panel.GetComponentInParent<Canvas>();
        Camera cam = rootCanvas && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(panel, screenPos, cam, out local)) return;

        Vector2 size = panel.rect.size;
        float xFromLeft = local.x + (size.x * panel.pivot.x);
        float yFromTop = (size.y * (1f - panel.pivot.y)) - local.y;

        float cellW = grid.cellSize.x, cellH = grid.cellSize.y;
        float spX = grid.spacing.x, spY = grid.spacing.y;
        float padL = grid.padding.left, padT = grid.padding.top;

        float px = xFromLeft - padL, py = yFromTop - padT;
        if (px < 0f || py < 0f) return;

        int cellX = Mathf.FloorToInt(px / (cellW + spX));
        int cellY = Mathf.FloorToInt(py / (cellH + spY));

        int startX = Mathf.Clamp(cellX, 0, inv.width - itemUI.itemData.width);
        int startY = Mathf.Clamp(cellY, 0, inv.height - itemUI.itemData.height);

        bool canPlace = inv.CanPlaceItem(startX, startY, itemUI.itemData, itemUI);

        for (int dx = 0; dx < itemUI.itemData.width; dx++)
            for (int dy = 0; dy < itemUI.itemData.height; dy++)
            {
                int cx = startX + dx, cy = startY + dy;
                if (cx < 0 || cy < 0 || cx >= inv.width || cy >= inv.height) continue;
                inv.slots[cx, cy].Highlight(canPlace ? Color.green : Color.red);
            }
    }

    private void DoHighlightForContainer(ContainerInventoryManager cont, Vector2 screenPos)
    {
        if (cont == null || cont.slots == null || cont.slotParent == null) return;

        foreach (var s in cont.slots) if (s != null) s.ResetHighlight();

        var grid = cont.slotParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        var panel = cont.slotParent as RectTransform;
        if (grid == null || panel == null) return;

        Vector2 local;
        var rootCanvas = panel.GetComponentInParent<Canvas>();
        Camera cam = rootCanvas && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(panel, screenPos, cam, out local)) return;

        Vector2 size = panel.rect.size;
        float xFromLeft = local.x + (size.x * panel.pivot.x);
        float yFromTop = (size.y * (1f - panel.pivot.y)) - local.y;

        float cellW = grid.cellSize.x, cellH = grid.cellSize.y;
        float spX = grid.spacing.x, spY = grid.spacing.y;
        float padL = grid.padding.left, padT = grid.padding.top;

        float px = xFromLeft - padL, py = yFromTop - padT;
        if (px < 0f || py < 0f) return;

        int cellX = Mathf.FloorToInt(px / (cellW + spX));
        int cellY = Mathf.FloorToInt(py / (cellH + spY));

        int startX = Mathf.Clamp(cellX, 0, cont.width - itemUI.itemData.width);
        int startY = Mathf.Clamp(cellY, 0, cont.height - itemUI.itemData.height);

        bool canPlace = cont.CanPlaceItem(startX, startY, itemUI.itemData, itemUI);

        for (int dx = 0; dx < itemUI.itemData.width; dx++)
            for (int dy = 0; dy < itemUI.itemData.height; dy++)
            {
                int cx = startX + dx, cy = startY + dy;
                if (cx < 0 || cy < 0 || cx >= cont.width || cy >= cont.height) continue;
                cont.slots[cx, cy].Highlight(canPlace ? Color.green : Color.red);
            }
    }

    // Qui "possède" cet ItemUI ? (joueur / conteneur / aucun)
    private enum Owner { None, Player, Container }

    private Owner GetOwner(ItemUI ui, out InventoryManager player, out ContainerInventoryManager cont)
    {
        player = ui ? ui.GetComponentInParent<InventoryManager>() : null;
        cont = ui ? ui.GetComponentInParent<ContainerInventoryManager>() : null;
        if (player != null) return Owner.Player;
        if (cont != null) return Owner.Container;
        return Owner.None;
    }

    // Retire un item de son inventaire propriétaire
    private void RemoveFromOwner(ItemUI ui)
    {
        if (ui == null) return;
        var owner = GetOwner(ui, out var player, out var cont);
        switch (owner)
        {
            case Owner.Player:
                if (player != null) player.RemoveItem(ui);
                break;
            case Owner.Container:
                if (cont != null) cont.RemoveItem(ui);
                break;
        }
    }

    // S’assure que l’item figure dans la liste logique de son propriétaire actuel
    private void EnsureRegisteredInOwnerList(ItemUI ui)
    {
        if (ui == null) return;
        var owner = GetOwner(ui, out var player, out var cont);
        if (owner == Owner.Player && player != null)
        {
            player.AddToInventoryList(ui);
        }
        else if (owner == Owner.Container && cont != null)
        {
            cont.AddIfMissing(ui); // 👈 on ajoute cette petite méthode côté conteneur plus bas
        }
    }

    // Essaie de merger avec l’ItemUI directement SOUS la souris (prioritaire)
    private bool TryMergeStackUnderPointer(PointerEventData eventData, ItemUI dragged)
    {
        if (dragged == null || dragged.itemData == null || !dragged.itemData.isStackable) return false;
        if (UnityEngine.EventSystems.EventSystem.current == null) return false;

        var pd = new PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = eventData.position };
        var results = new List<RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pd, results);

        foreach (var r in results)
        {
            var target = r.gameObject.GetComponentInParent<ItemUI>();
            if (target == null || target == dragged) continue;
            if (target.itemData != dragged.itemData) continue; // même SO
            if (!target.itemData.isStackable) continue;
            if (target.currentStack >= target.itemData.maxStack) continue;

            // On fusionne
            int space = target.itemData.maxStack - target.currentStack;
            int moved = Mathf.Min(space, dragged.currentStack);

            target.currentStack += moved;
            target.UpdateStackText();

            dragged.currentStack -= moved;
            dragged.UpdateStackText();

            if (dragged.currentStack <= 0)
            {
                // ✅ retire de son inventaire ET détruit le GameObject
                RemoveFromOwner(dragged);
                if (dragged != null && dragged.gameObject != null)
                    Object.Destroy(dragged.gameObject);
            }
            else
            {
                EnsureRegisteredInOwnerList(dragged);
            }

            // Juste pour être propre visuellement
            target.UpdateOutline();
            dragged.UpdateOutline();

            return true;
        }
        return false;
    }
}
