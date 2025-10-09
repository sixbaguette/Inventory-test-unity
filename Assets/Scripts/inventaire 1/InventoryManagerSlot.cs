using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public int width = 6;
    public int height = 5;
    public GameObject slotPrefab;
    public Transform slotParent;           // parent des slots (GridLayoutGroup)
    public GameObject itemUIPrefab;        // prefab UI (ItemUI) pour AddItem()
    public Canvas overlayCanvas;           // canvas overlay pour drag

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

                slot.Setup(x, y);
                slots[x, y] = slot;

                SlotDrop sd = slotGO.GetComponent<SlotDrop>();
                if (sd != null) { sd.x = x; sd.y = y; }
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

        if (ui.occupiedSlots != null)
        {
            foreach (var s in ui.occupiedSlots)
                s.ClearItem();
        }

        ItemData item = ui.itemData; // <-- ItemData, pas Item
        Destroy(ui.gameObject);
        return item;
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
        if (itemUI == null || itemUI.itemData == null) return false;
        ItemData item = itemUI.itemData;

        // Libère anciens slots
        if (itemUI.occupiedSlots != null)
        {
            foreach (var s in itemUI.occupiedSlots) if (s != null) s.ClearItem();
        }

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

        // Assignation des nouveaux slots
        itemUI.SetOccupiedSlots(startX, startY, item.width, item.height);
        foreach (var s in itemUI.occupiedSlots) if (s != null) s.SetItem(itemUI);

        // Positionnement visuel : on met parent = slot et on ajuste ancres / size afin d'être correctement aligné
        Slot targetSlot = slots[startX, startY];
        RectTransform slotRect = targetSlot.GetComponent<RectTransform>();

        itemUI.transform.SetParent(targetSlot.transform, false);
        itemUI.transform.SetAsLastSibling();

        // Ajuste le rect transform du ItemUI à la taille occupée (en slots)
        float spacingX = 0f, spacingY = 0f;
        var grid = slotParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (grid != null) { spacingX = grid.spacing.x; spacingY = grid.spacing.y; }

        Vector2 desiredSize = new Vector2(
            (item.width * slotRect.sizeDelta.x) + ((item.width - 1) * spacingX),
            (item.height * slotRect.sizeDelta.y) + ((item.height - 1) * spacingY)
        );

        RectTransform itRect = itemUI.rectTransform;
        itRect.anchorMin = new Vector2(0, 1);
        itRect.anchorMax = new Vector2(0, 1);
        itRect.pivot = new Vector2(0, 1);
        itRect.anchoredPosition = Vector2.zero;
        itRect.sizeDelta = desiredSize;

        itemUI.currentSlot = targetSlot;
        itemUI.UpdateOutline();
        itemUI.UpdateSize(); // maintien cohérence

        return true;
    }

    // Ajoute un item dans l'inventaire (instancie le prefab UI et place dans la première case libre)
    public bool AddItem(ItemData data)
    {
        if (data == null) return false;
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

        itemUI.Setup(data);

        if (FindFirstFreePosition(data, out int px, out int py))
        {
            PlaceItem(itemUI, px, py);
            return true;
        }

        // Pas de place : détruit l'UI et signale
        Destroy(go);
        Debug.LogWarning("[InventoryManager] Pas de place pour ajouter " + data.itemName);
        return false;
    }

    // Retire l'ItemUI (libère les slots et détruit l'UI)
    public void RemoveItem(ItemUI itemUI)
    {
        if (itemUI == null) return;

        if (itemUI.occupiedSlots != null)
        {
            foreach (var s in itemUI.occupiedSlots) if (s != null) s.ClearItem();
        }

        Destroy(itemUI.gameObject);
    }
}
