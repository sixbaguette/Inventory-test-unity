using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static ItemUI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public int width = 6;
    public int height = 5;
    public GameObject slotPrefab;
    public Transform slotParent;           // parent des slots (GridLayoutGroup)
    public GameObject itemUIPrefab;        // prefab UI (ItemUI) pour AddItem()
    public Canvas overlayCanvas;           // canvas overlay pour drag
    public RectTransform itemsLayer; // assigné dans l’inspecteur

    private List<ItemUI> inventoryItems = new List<ItemUI>();

    [HideInInspector]
    public Slot[,] slots;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 🧭 S’assure que le Canvas overlay peut recevoir les clics
        if (overlayCanvas != null)
        {
            var gr = overlayCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (gr == null) overlayCanvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // S’assure que la grille est prête
        InitializeGrid();
    }


    public void InitializeGrid()
    {
        if (slotParent == null)
        {
            Debug.LogError("[InventoryManager] slotParent non assigné !");
            return;
        }

        // 🔄 Détruit les anciens slots si réinitialisation
        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        slots = new Slot[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject slotGO = Instantiate(slotPrefab, slotParent);
                Slot slot = slotGO.GetComponent<Slot>();
                if (slot == null)
                {
                    Debug.LogError("[InventoryManager] SlotPrefab n’a pas de script Slot !");
                    continue;
                }

                // ✅ Le Slot connaît déjà sa position
                slot.Setup(x, y);
                slots[x, y] = slot;
            }
        }

        Debug.Log($"[InventoryManager] Grille initialisée ({width}x{height})");
    }



    public bool CanPlaceItem(int startX, int startY, ItemData item, ItemUI ignoreItemUI = null)
    {
        if (item == null) return false;
        if (startX < 0 || startY < 0) return false;

        for (int yy = 0; yy < item.height; yy++)
        {
            for (int xx = 0; xx < item.width; xx++)
            {
                int checkX = startX + xx;
                int checkY = startY + yy;

                if (checkX < 0 || checkY < 0 || checkX >= width || checkY >= height) return false;

                Slot slot = slots[checkX, checkY];
                if (slot == null) return false;

                if (slot.HasItem() && slot.GetItem() != ignoreItemUI) return false;
            }
        }

        return true;
    }

    // Supprime le dernier item ajouté et renvoie son ScriptableObject
    // Supprime le dernier item ajouté et renvoie son ScriptableObject
    public ItemData RemoveLastItem()
    {
        if (inventoryItems == null || inventoryItems.Count == 0)
            return null;

        ItemUI ui = inventoryItems[inventoryItems.Count - 1];
        inventoryItems.RemoveAt(inventoryItems.Count - 1);

        if (ui == null) return null;

        ItemData item = ui.itemData;

        if (ui.occupiedSlots != null)
        {
            foreach (var s in ui.occupiedSlots)
                if (s != null)
                    s.ClearItem();
        }

        if (ui != null)
            Destroy(ui.gameObject);

        return item;
    }


    public ItemUI GetLastItem()
    {
        if (inventoryItems == null || inventoryItems.Count == 0)
            return null;

        return inventoryItems[inventoryItems.Count - 1];
    }

    public bool TryMergeStacks(ItemUI source, ItemUI target)
    {
        if (source == null || target == null) return false;
        if (source.itemData == null || target.itemData == null) return false;
        Debug.Log($"[TryMergeStacks] Source={source.itemData.itemName} ({source.itemData.prefabName}) / Target={target.itemData.itemName} ({target.itemData.prefabName})");
        if (!source.itemData.isStackable || !target.itemData.isStackable)
        {
            Debug.Log("  ❌ Un des deux n'est pas stackable.");
            return false;
        }

        if (!source.itemData.IsSameType(target.itemData))
        {
            Debug.Log("  ❌ IsSameType retourne FALSE !");
            return false;
        }

        int spaceLeft = target.itemData.maxStack - target.currentStack;
        if (spaceLeft <= 0) return false;

        int moved = Mathf.Min(spaceLeft, source.currentStack);
        if (moved <= 0) return false;

        target.currentStack += moved;
        target.UpdateStackText();

        source.currentStack -= moved;
        source.UpdateStackText();

        if (source.currentStack <= 0)
        {
            // 🔺 Supprimer la source selon son propriétaire réel
            switch (source.Owner)
            {
                case ItemUI.ItemOwner.Player:
                    RemoveItem(source); // enlève des slots + détruit l’UI
                    break;
                case ItemUI.ItemOwner.Container:
                    var cont = source.GetComponentInParent<ContainerInventoryManager>();
                    if (cont != null) cont.RemoveItem(source);
                    else if (source != null && source.gameObject != null) Destroy(source.gameObject);
                    break;
                default:
                    if (source != null && source.gameObject != null) Destroy(source.gameObject);
                    break;
            }
        }

        // petit polish visuel
        target.UpdateOutline();
        return true;
    }

    public bool FindFirstFreePosition(ItemData item, out int outX, out int outY)
    {
        outX = outY = -1;
        for (int y = 0; y <= height - item.height; y++)
        {
            for (int x = 0; x <= width - item.width; x++)
            {
                if (CanPlaceItem(x, y, item))
                {
                    outX = x; outY = y;
                    return true;
                }
            }
        }
        return false;
    }

    // Place un ItemUI (UI existant) dans la grille à startX,startY
    public bool PlaceItem(ItemUI itemUI, int startX, int startY)
    {
        if (itemUI == null || itemUI.itemData == null)
            return false;

        ItemData item = itemUI.itemData;

        // 🧩 Clamp la position pour éviter les dépassements
        startX = Mathf.Clamp(startX, 0, width - item.width);
        startY = Mathf.Clamp(startY, 0, height - item.height);

        // 🔄 Libère les anciens slots de l'item s’il en avait
        if (itemUI.occupiedSlots != null)
        {
            foreach (var s in itemUI.occupiedSlots)
                if (s != null) s.ClearItem();
        }

        // 🚫 Vérifie la place dispo avant d’essayer de poser
        if (!CanPlaceItem(startX, startY, item, itemUI))
        {
            // Essaye de revenir à l'ancien emplacement
            if (itemUI.currentSlot != null)
            {
                itemUI.SetOccupiedSlots(itemUI.currentSlot.x, itemUI.currentSlot.y, item.width, item.height);
                foreach (var s in itemUI.occupiedSlots)
                    if (s != null) s.SetItem(itemUI);
            }
            return false;
        }

        // ⚠️ Double sécurité : vérifie qu'aucun autre item ne bloque
        for (int yy = 0; yy < item.height; yy++)
        {
            for (int xx = 0; xx < item.width; xx++)
            {
                int cx = startX + xx;
                int cy = startY + yy;

                if (cx < 0 || cy < 0 || cx >= width || cy >= height)
                    continue;

                var slot = slots[cx, cy];
                if (slot.HasItem() && slot.GetItem() != itemUI)
                {
                    Debug.LogWarning("[ContainerInventory] Zone occupée, placement annulé.");
                    return false;
                }
            }
        }

        // ✅ Assigne les nouveaux slots
        itemUI.SetOccupiedSlots(startX, startY, item.width, item.height);
        foreach (var s in itemUI.occupiedSlots)
            if (s != null) s.SetItem(itemUI);

        // 🎯 Positionne l'item dans le layer visuel (ItemsLayer ou fallback)
        if (itemsLayer == null)
            itemsLayer = slotParent as RectTransform;

        itemUI.transform.SetParent(itemsLayer, false);

        // 📏 Récupère infos du grid pour calculer la position
        var grid = slotParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        float spacingX = grid ? grid.spacing.x : 0f;
        float spacingY = grid ? grid.spacing.y : 0f;
        float padLeft = grid ? grid.padding.left : 0f;
        float padTop = grid ? grid.padding.top : 0f;

        RectTransform cellRect = slots[0, 0].GetComponent<RectTransform>();
        float cellW = cellRect.sizeDelta.x;
        float cellH = cellRect.sizeDelta.y;

        Vector2 desiredSize = new Vector2(
            (item.width * cellW) + ((item.width - 1) * spacingX),
            (item.height * cellH) + ((item.height - 1) * spacingY)
        );

        RectTransform itRect = itemUI.rectTransform;
        itRect.anchorMin = new Vector2(0, 1);
        itRect.anchorMax = new Vector2(0, 1);
        itRect.pivot = new Vector2(0, 1);
        itRect.sizeDelta = desiredSize;

        // ✅ Calcule position pixel parfaite (haut-gauche = origine)
        float xPx = padLeft + startX * (cellW + spacingX);
        float yPx = padTop + startY * (cellH + spacingY);
        itRect.anchoredPosition = new Vector2(xPx, -yPx);

        // 🔝 Met visuellement l'item au-dessus
        itemUI.transform.SetAsLastSibling();

        // 🔖 Mémorise le premier slot (origine logique)
        itemUI.currentSlot = slots[startX, startY];

        // 🎨 Met à jour le visuel
        itemUI.UpdateSize();
        itemUI.UpdateOutline();
        itemUI.ResetVisualLayout();
        itemUI.EnableRaycastAfterDrop(); // <--- ajoute ça

        // 🔧 Corrige rotation éventuelle
        if (itemUI.icon != null)
            itemUI.icon.rectTransform.localEulerAngles = Vector3.zero;
        if (itemUI.outline != null)
            itemUI.outline.rectTransform.localEulerAngles = Vector3.zero;

        // ✅ Assure la réactivation du CanvasGroup pour le redrag
        var cg = itemUI.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = itemUI.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
        cg.interactable = true;
        cg.alpha = 1f;

        // ✅ Synchronise la source du drag
        var drag = itemUI.GetComponent<ItemDrag>();
        if (drag != null)
        {
            drag.itemUI = itemUI;
            drag.sourcePlayerInv = this;
            drag.sourceContainerInv = null;
        }

        // ✅ Réactive le raycast complet après placement
        itemUI.EnableRaycastAfterDrop();
        itemUI.transform.SetAsLastSibling();
        itemUI.EnsureCanvasRaycastable();
        itemUI.DisableExtraCanvasIfInInventory();
        StripLocalCanvas(itemUI); // 🧹 supprime les Canvas temporaires (drag)

        itemUI.Owner = ItemOwner.Player;
        return true;
    }


    // Ajoute un item dans l'inventaire (instancie le prefab UI et place dans la première case libre)
    public bool AddItem(ItemData data, int quantity = 1)
    {
        if (data == null) return false;

        // 🧹 Nettoyage de sécurité : retire toute référence d'ItemUI détruit
        for (int i = inventoryItems.Count - 1; i >= 0; i--)
        {
            if (inventoryItems[i] == null)
                inventoryItems.RemoveAt(i);
        }

        // === Si stackable : essaie de compléter un stack existant ===
        if (data.isStackable)
        {
            foreach (ItemUI ui in inventoryItems)
            {
                if (ui.itemData == data && ui.currentStack < data.maxStack)
                {
                    quantity = ui.AddToStack(quantity);
                    if (quantity <= 0)
                        return true; // tout ajouté, on sort
                }
            }
        }

        // === Sinon ou s’il reste du "reste" ===
        while (quantity > 0)
        {
            if (itemUIPrefab == null)
            {
                Debug.LogError("[InventoryManager] itemUIPrefab non assigné !");
                return false;
            }

            GameObject go = Instantiate(itemUIPrefab, slotParent.parent.Find("ItemsParent"));
            ItemUI itemUI = go.GetComponent<ItemUI>();
            if (itemUI == null)
            {
                Debug.LogError("[InventoryManager] itemUIPrefab n'a pas de ItemUI !");
                Destroy(go);
                return false;
            }

            // Crée une copie unique de l’ItemData pour chaque ItemUI
            //ItemData runtimeCopy = ScriptableObject.Instantiate(data);
            //itemUI.Setup(runtimeCopy);
            itemUI.Setup(data);

            inventoryItems.Add(itemUI);

            if (FindFirstFreePosition(data, out int px, out int py))
            {
                PlaceItem(itemUI, px, py);
            }
            else
            {
                Debug.LogWarning("[InventoryManager] Pas de place pour ajouter " + data.itemName);
                Destroy(go);
                return false;
            }

            // Si stackable, remplir le stack au max
            if (data.isStackable)
            {
                int toAdd = Mathf.Min(quantity, data.maxStack);
                itemUI.currentStack = toAdd;
                itemUI.UpdateStackText();
                quantity -= toAdd;
            }
            else
            {
                quantity--; // item unique
            }
        }

        return true;
    }

    // Retire l'ItemUI (libère les slots et détruit l'UI)
    public void RemoveItem(ItemUI itemUI)
    {
        if (itemUI == null) return;

        // 🔹 Nettoie les highlights résiduels
        if (slots != null)
        {
            foreach (var s in slots)
                s?.ResetHighlight();
        }

        // ✅ sécurité supplémentaire : évite les suppressions multiples
        if (!inventoryItems.Contains(itemUI))
            return;

        inventoryItems.Remove(itemUI);

        if (itemUI.occupiedSlots != null)
        {
            foreach (var s in itemUI.occupiedSlots)
                if (s != null)
                    s.ClearItem();
        }

        if (itemUI != null && itemUI.gameObject != null)
            Destroy(itemUI.gameObject);

        Debug.Log($"Item supprimé. Il reste {inventoryItems.Count} items dans l’inventaire.");
    }

    public void AddToInventoryList(ItemUI itemUI)
    {
        if (itemUI == null) return;
        if (!inventoryItems.Contains(itemUI))
            inventoryItems.Add(itemUI);
    }

    public void QuickTransfer(ItemUI itemUI)
    {
        if (itemUI == null || itemUI.itemData == null)
            return;

        ItemData data = itemUI.itemData;

        // 🧭 Vérifie si l'item est actuellement dans un slot d'équipement
        bool isInEquipment = EquipementManager.Instance.equipSlots != null &&
                             System.Array.Exists(EquipementManager.Instance.equipSlots,
                                 s => s != null && s.CurrentItem == itemUI);

        // 1️⃣ Si l’objet est déjà équipé → le renvoyer dans l’inventaire
        if (isInEquipment)
        {
            EquipementManager.Instance.UnequipItem(itemUI);

            if (InventoryAudioManager.Instance != null)
            {
                InventoryAudioManager.Instance.Play("close_inventory"); // ou un son de "range / holster"
            }

            Debug.Log($"[QuickTransfer] {data.itemName} retiré de l'équipement via Shift + Clic.");
            return;
        }

        // 2️⃣ Sinon, si l’objet est dans l’inventaire et équipable → on tente de l’équiper
        if (data.isEquipable)
        {
            bool equipped = EquipementManager.Instance.TryEquipItem(itemUI);
            if (equipped)
            {
                if (InventoryAudioManager.Instance != null)
                {
                    if (data.equipSlotType == EquipSlotType.Primary || data.equipSlotType == EquipSlotType.Secondary)
                        InventoryAudioManager.Instance.Play("equip_weapon");
                    else
                        InventoryAudioManager.Instance.Play("equip_armor");
                }

                Debug.Log($"[QuickTransfer] {data.itemName} équipé automatiquement via Shift + Clic.");
                return;
            }
        }

        // 3️⃣ Plus tard : gérer le transfert vers coffre ou autre conteneur
        Debug.Log($"[QuickTransfer] Aucun slot compatible libre pour {data.itemName}.");
    }

    public bool TryAutoPlace(ItemUI item)
    {
        if (item == null || item.itemData == null) return false;

        // 1) Essai orientation actuelle
        if (FindFirstFreePosition(item.itemData, out int x, out int y))
        {
            // Visuel propre (au cas où)
            item.UpdateSize();
            item.UpdateOutline();
            item.ResetVisualLayout();
            return PlaceItem(item, x, y);
        }

        // 2) Essai orientation pivotée (⚠️ sans appeler RotateItem)
        int oldW = item.itemData.width;
        int oldH = item.itemData.height;

        item.itemData.width = oldH;
        item.itemData.height = oldW;

        bool ok = FindFirstFreePosition(item.itemData, out x, out y);
        if (ok)
        {
            // On garde cette orientation (données déjà échangées)
            // ➜ MAJ visuelle SANS ré-échanger les données
            item.UpdateSize();
            item.UpdateOutline();
            item.ResetVisualLayout();

            return PlaceItem(item, x, y);
        }

        // 3) Rien trouvé → revert et échec
        item.itemData.width = oldW;
        item.itemData.height = oldH;
        item.UpdateSize();
        item.UpdateOutline();
        item.ResetVisualLayout();
        return false;
    }

    // 🔫 Consomme des balles compatibles dans l'inventaire
    public int ConsumeAmmo(BulletType type, int needed)
    {
        int collected = 0;

        // copie la liste car on peut modifier pendant l’itération
        var itemsCopy = new List<ItemUI>(inventoryItems);

        foreach (var item in itemsCopy)
        {
            if (item == null || item.itemData == null)
                continue;

            if (item.itemData.isAmmo && item.itemData.ammoType == type)
            {
                int take = Mathf.Min(item.currentStack, needed - collected);
                collected += take;
                item.currentStack -= take;
                item.UpdateStackText();

                if (item.currentStack <= 0)
                    RemoveItem(item);

                if (collected >= needed)
                    break;
            }
        }

        Debug.Log($"[InventoryManager] → {collected} balles de type {type} consommées");
        return collected;
    }

    // Détache l'item de l'inventaire joueur sans détruire l'UI
    public void DetachWithoutDestroy(ItemUI ui)
    {
        if (ui == null) return;

        // Enlève de la liste
        if (inventoryItems.Contains(ui))
            inventoryItems.Remove(ui);

        // Libère les slots occupés
        if (ui.occupiedSlots != null)
        {
            foreach (var s in ui.occupiedSlots)
                if (s != null) s.ClearItem();
        }

        // Nettoie les refs d’emplacement
        ui.occupiedSlots = null;
        ui.currentSlot = null;
        ui.Owner = ItemOwner.None;
    }

    // =======================================================
    // 🔧 Supprime tout Canvas/GraphicRaycaster local sur un item
    // =======================================================
    private void StripLocalCanvas(Component root)
    {
        if (root == null) return;
        var c = root.GetComponent<Canvas>();
        if (c != null) Destroy(c);
        var gr = root.GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (gr != null) Destroy(gr);
    }

    // Shift+clic depuis l'inventaire joueur -> conteneur OU1 (ouvert)
    public bool ShiftClickTransferToOpenContainer(ItemUI ui)
    {
        var cont = ContainerUIController.Instance?.GetActiveContainerInventory();
        // 🧠 Sécurité : assure que les deux listes sont à jour
        AddToInventoryList(ui);
        cont?.AddIfMissing(ui);
        if (ui == null || cont == null)
        {
            Debug.LogWarning("[ShiftClick] Aucun conteneur ouvert.");
            return false;
        }

        // 1️⃣ Fusion prioritaire dans le conteneur (avant tout placement)
        foreach (var other in cont.items.ToArray())
        {
            if (other == null || other.itemData == null) continue;
            if (!other.itemData.isStackable) continue;
            if (!other.itemData.IsSameType(ui.itemData)) continue;

            int space = other.itemData.maxStack - other.currentStack;
            if (space <= 0) continue;

            int moved = Mathf.Min(space, ui.currentStack);
            if (moved <= 0) continue;

            other.currentStack += moved;
            other.UpdateStackText();

            ui.currentStack -= moved;
            ui.UpdateStackText();

            Debug.Log($"[ShiftClick] Fusion {moved} -> conteneur ({other.itemData.itemName})");

            if (ui.currentStack <= 0)
            {
                RemoveItem(ui);
                return true;
            }
        }

        // ✅ Nouveau : fusion post-placement (si même item déjà là)
        if (ui.currentStack > 0)
        {
            foreach (var other in cont.items.ToArray())
            {
                if (other == null || other == ui) continue;
                InventoryManager.Instance.TryMergeStacks(ui, other);
                if (ui.currentStack <= 0) return true;
            }
        }

        // 2️⃣ Trouve une position libre dans la grille du conteneur
        var pos = cont.FindFreeSpaceFor(ui.itemData);
        if (!pos.HasValue)
        {
            Debug.Log("[ShiftClick] Pas d’espace libre dans le conteneur.");
            return false;
        }

        // ⚙️ 3️⃣ Déplace visuellement et logiquement l’item
        // - on le détache proprement de l’inventaire
        DetachWithoutDestroy(ui);

        // - on change le parent du RectTransform
        ui.transform.SetParent(cont.itemsLayer != null ? cont.itemsLayer : cont.slotParent, false);

        // - on place à la position libre
        bool placed = cont.PlaceItem(ui, pos.Value.x, pos.Value.y);
        if (!placed)
        {
            TryAutoPlace(ui);
            AddToInventoryList(ui);
            return false;
        }

        // - on ajoute à la liste interne du conteneur
        cont.AddIfMissing(ui);

        // - on assure l’interaction visuelle
        ui.EnableRaycastAfterDrop();
        ui.DisableExtraCanvasIfInInventory();
        ui.transform.SetAsLastSibling();

        Debug.Log($"[ShiftClick] {ui.itemData.itemName} déplacé vers conteneur ({pos.Value.x},{pos.Value.y})");
        return true;
    }

    public List<ItemUI> GetAllItemUIs()
    {
        var result = new List<ItemUI>();

        // 1) La liste logique (la source de vérité)
        foreach (var ui in inventoryItems)
            if (ui != null) result.Add(ui);

        // 2) Sécurité : balaye le itemsLayer au cas où
        if (itemsLayer != null)
        {
            for (int i = 0; i < itemsLayer.childCount; i++)
            {
                var ui = itemsLayer.GetChild(i).GetComponent<ItemUI>();
                if (ui != null && !result.Contains(ui))
                    result.Add(ui);
            }
        }

        return result;
    }
}