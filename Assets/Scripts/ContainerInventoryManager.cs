using System.Collections.Generic;
using UnityEngine;

public class ContainerInventoryManager : MonoBehaviour
{
    [Header("Grille du conteneur")]
    public int width = 6;
    public int height = 5;
    public GameObject slotPrefab;
    public Transform slotParent;
    public GameObject itemUIPrefab;
    public RectTransform itemsLayer;

    [HideInInspector] public Slot[,] slots;
    private readonly List<ItemUI> items = new();

    public void InitializeGrid()
    {
        if (slotParent == null)
        {
            Debug.LogError("[ContainerInventory] slotParent non assigné !");
            return;
        }

        // 🔄 Efface ancienne grille
        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        if (itemsLayer != null)
        {
            foreach (Transform child in itemsLayer)
                Destroy(child.gameObject);
        }

        slots = new Slot[width, height];

        // 🧩 Assure un fond visuel
        var parentImg = slotParent.GetComponent<UnityEngine.UI.Image>();
        if (parentImg == null)
            parentImg = slotParent.gameObject.AddComponent<UnityEngine.UI.Image>();
        parentImg.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        parentImg.raycastTarget = false;

        // 🧩 Setup du layout
        var grid = slotParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (grid == null)
            grid = slotParent.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
        grid.cellSize = new Vector2(40, 40);
        grid.spacing = new Vector2(1, 1);
        grid.padding = new RectOffset(400, 0, 30, 0);
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = width;

        // 🔧 Crée les slots
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject slotGO = Instantiate(slotPrefab, slotParent);
                slotGO.name = $"Slot_{x}_{y}";
                var slot = slotGO.GetComponent<Slot>();
                slot.Setup(x, y);
                slots[x, y] = slot;

                var img = slotGO.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                    img.color = Color.gray;
            }
        }

        Debug.Log($"[ContainerInventory] Grille {width}x{height} initialisée ({slotParent.childCount} slots)");
    }

    // ----------------------------------------------------------------------

    public bool AddItem(ItemData data)
    {
        if (data == null) return false;

        // 🔍 Trouve une place libre
        Vector2Int? freePos = FindFreeSpaceFor(data);
        if (!freePos.HasValue)
        {
            Debug.LogWarning("[ContainerInventory] Pas de place libre !");
            return false;
        }

        AddItemAt(data, freePos.Value.x, freePos.Value.y);
        return true;
    }

    public Vector2Int? FindFreeSpaceFor(ItemData item)
    {
        for (int y = 0; y <= height - item.height; y++)
        {
            for (int x = 0; x <= width - item.width; x++)
            {
                if (CanPlaceItem(x, y, item))
                    return new Vector2Int(x, y);
            }
        }
        return null;
    }

    // ContainerInventoryManager.cs
    public bool CanPlaceItem(int startX, int startY, ItemData item, ItemUI ignore = null)
    {
        if (item == null) return false;
        for (int yy = 0; yy < item.height; yy++)
            for (int xx = 0; xx < item.width; xx++)
            {
                int cx = startX + xx;
                int cy = startY + yy;
                if (cx < 0 || cy < 0 || cx >= width || cy >= height) return false;

                var slot = slots[cx, cy];
                if (slot.HasItem() && slot.GetItem() != ignore)
                    return false;
            }
        return true;
    }

    public bool PlaceItem(ItemUI ui, int startX, int startY)
    {
        if (ui == null || ui.itemData == null) return false;

        // ✅ Libère les anciens slots occupés par CET item
        if (ui.occupiedSlots != null)
        {
            foreach (var s in ui.occupiedSlots)
                if (s != null) s.ClearItem();
        }

        // ✅ Teste en ignorant l’item lui-même (déplacement interne)
        if (!CanPlaceItem(startX, startY, ui.itemData, ui))
            return false;

        // ➜ Assigne le tableau occupiedSlots
        int w = ui.itemData.width, h = ui.itemData.height;
        ui.occupiedSlots = new Slot[w * h];
        int idx = 0;

        for (int dx = 0; dx < w; dx++)
            for (int dy = 0; dy < h; dy++)
            {
                var s = slots[startX + dx, startY + dy];
                s.SetItem(ui);
                ui.occupiedSlots[idx++] = s;
            }

        ui.currentSlot = slots[startX, startY];

        // Parent & positionnement visuel (comme tu fais déjà)
        ui.transform.SetParent(itemsLayer != null ? itemsLayer : slotParent, false);
        var grid = slotParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        float cellW = grid.cellSize.x, cellH = grid.cellSize.y;
        float spX = grid.spacing.x, spY = grid.spacing.y;
        float padL = grid.padding.left, padT = grid.padding.top;

        float posX = padL + startX * (cellW + spX);
        float posY = padT + startY * (cellH + spY);

        var rt = ui.rectTransform;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(posX, -posY);

        // Interactivité OK
        var cg = ui.GetComponent<CanvasGroup>() ?? ui.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true; cg.interactable = true; cg.alpha = 1f;

        // Drag context
        var drag = ui.GetComponent<ItemDrag>();
        if (drag != null)
        {
            drag.itemUI = ui;
            drag.sourcePlayerInv = null;
            drag.sourceContainerInv = this;
        }

        ui.UpdateSize();
        ui.UpdateOutline();
        ui.EnableRaycastAfterDrop();
        ui.transform.SetAsLastSibling();

        if (!items.Contains(ui)) items.Add(ui);
        return true;
    }

    public void AddItemAt(ItemData itemData, int x, int y)
    {
        if (itemData == null) return;

        GameObject itemObj = Instantiate(itemUIPrefab, itemsLayer);
        ItemUI ui = itemObj.GetComponent<ItemUI>();
        ui.Setup(itemData);
        PlaceItem(ui, x, y);
    }

    public void RemoveItem(ItemUI ui)
    {
        if (ui == null) return;
        if (items.Contains(ui))
            items.Remove(ui);

        if (ui.occupiedSlots != null)
        {
            foreach (var s in ui.occupiedSlots)
                s.ClearItem();
        }

        Destroy(ui.gameObject);
    }

    public void DetachWithoutDestroy(ItemUI ui)
    {
        if (ui == null) return;

        if (items.Contains(ui))
            items.Remove(ui);

        if (ui.occupiedSlots != null)
        {
            foreach (var s in ui.occupiedSlots)
                s.ClearItem();
        }

        ui.occupiedSlots = null;
        ui.currentSlot = null;
    }
}
