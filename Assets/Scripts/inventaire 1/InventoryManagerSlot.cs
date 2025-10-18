using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
        if (source.itemData != target.itemData) return false;
        if (!source.itemData.isStackable) return false;

        int spaceLeft = target.itemData.maxStack - target.currentStack;
        if (spaceLeft <= 0) return false;

        int moved = Mathf.Min(spaceLeft, source.currentStack);
        target.currentStack += moved;
        source.currentStack -= moved;

        target.UpdateStackText();
        source.UpdateStackText();

        // ✅ Si le stack source est vide après fusion → on le retire proprement
        if (source.currentStack <= 0)
        {
            // 1️⃣ Libère visuellement ses anciens slots
            if (source.occupiedSlots != null)
            {
                foreach (var s in source.occupiedSlots)
                    if (s != null)
                        s.ClearItem();
            }

            // 2️⃣ Retire l’ItemUI de la liste d’inventaire
            if (inventoryItems.Contains(source))
                inventoryItems.Remove(source);

            // 3️⃣ Supprime le GameObject (UI)
            if (source != null && source.gameObject != null)
                Destroy(source.gameObject);

            Debug.Log($"Stack {source.itemData.itemName} fusionné et supprimé proprement.");
        }

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
        startX = Mathf.Clamp(startX, 0, width - itemUI.itemData.width);
        startY = Mathf.Clamp(startY, 0, height - itemUI.itemData.height);

        if (itemUI == null || itemUI.itemData == null) return false;
        ItemData item = itemUI.itemData;

        // Libère anciens slots
        if (itemUI.occupiedSlots != null)
            foreach (var s in itemUI.occupiedSlots) if (s != null) s.ClearItem();

        // --- libère proprement tous les anciens slots avant de tester ---
        if (itemUI.occupiedSlots != null)
        {
            foreach (var s in itemUI.occupiedSlots)
            {
                if (s != null)
                    s.ClearItem();
            }
        }

        // Vérifie place
        if (!CanPlaceItem(startX, startY, item, itemUI))
        {
            // si impossible → essayer de réassigner ancien emplacement (s'il existait)
            if (itemUI.currentSlot != null)
            {
                itemUI.SetOccupiedSlots(itemUI.currentSlot.x, itemUI.currentSlot.y, item.width, item.height);
                foreach (var s in itemUI.occupiedSlots) if (s != null) s.SetItem(itemUI);
            }
            return false;
        }

        // Vérifie qu'aucun autre item ne se trouve dans la zone cible
        for (int yy = 0; yy < item.height; yy++)
        {
            for (int xx = 0; xx < item.width; xx++)
            {
                int cx = startX + xx;
                int cy = startY + yy;
                if (slots[cx, cy].HasItem() && slots[cx, cy].GetItem() != itemUI)
                {
                    // ⚠️ un autre item occupe cet emplacement !
                    Debug.LogWarning("[InventoryManager] Zone occupée, placement annulé");
                    return false;
                }
            }
        }

        // Assigne les nouveaux slots
        itemUI.SetOccupiedSlots(startX, startY, item.width, item.height);
        foreach (var s in itemUI.occupiedSlots) if (s != null) s.SetItem(itemUI);

        // === POSITIONNEMENT PIXEL-PERFECT DANS ItemsLayer (PAS DANS UN SLOT) ===
        if (itemsLayer == null) itemsLayer = slotParent as RectTransform; // fallback

        // Parent = ItemsLayer
        itemUI.transform.SetParent(itemsLayer, false);

        // Récupère taille cellule + spacing
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

        // ✅ On ajoute le padding gauche / haut
        float xPx = padLeft + startX * (cellW + spacingX);
        float yPx = padTop + startY * (cellH + spacingY);

        itRect.anchoredPosition = new Vector2(xPx, -yPx);

        // Devant visuellement
        itemUI.transform.SetAsLastSibling();

        // Mémorise le "slot d'origine" (utile pour revert)
        itemUI.currentSlot = slots[startX, startY];

        // Mets à jour outline/size (sécure)
        itemUI.UpdateOutline();
        itemUI.UpdateSize();

        // Reset l’orientation visuelle après placement
        if (itemUI.icon != null)
            itemUI.icon.rectTransform.localEulerAngles = Vector3.zero;
        if (itemUI.outline != null)
            itemUI.outline.rectTransform.localEulerAngles = Vector3.zero;

        itemUI.ResetVisualLayout(); // garantit 0° + centrage après pose

        return true;
    }


    // Ajoute un item dans l'inventaire (instancie le prefab UI et place dans la première case libre)
    public bool AddItem(ItemData data, int quantity = 1)
    {
        if (data == null) return false;

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
}
