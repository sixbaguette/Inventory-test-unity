using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

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
    private List<ItemUI> items = new List<ItemUI>();

    public void InitializeGrid()
    {
        if (slotParent == null)
        {
            Debug.LogError("[ContainerInventory] slotParent non assigné !");
            return;
        }

        // 🔄 Efface l’ancienne grille
        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        slots = new Slot[width, height];

        // 🧩 S'assure que le parent a une taille et un fond visible
        var parentRect = slotParent.GetComponent<RectTransform>();
        if (parentRect != null)
        {
            // taille automatique si pas définie
            float w = width * 42f;
            float h = height * 42f;
            if (parentRect.sizeDelta.x < w || parentRect.sizeDelta.y < h)
                parentRect.sizeDelta = new Vector2(w, h);
        }

        var parentImg = slotParent.GetComponent<UnityEngine.UI.Image>();
        if (parentImg == null)
            parentImg = slotParent.gameObject.AddComponent<UnityEngine.UI.Image>();

        parentImg.color = new Color(1, 1, 1, 0.15f); // léger gris, visible
        parentImg.raycastTarget = false;             // ne bloque pas les clics
        parentImg.maskable = false;

        // 🧩 S'assure qu'il y a un GridLayoutGroup bien réglé
        var grid = slotParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (grid == null)
            grid = slotParent.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();

        grid.cellSize = new Vector2(40, 40);
        grid.spacing = new Vector2(1, 1);
        grid.padding = new RectOffset(400, 0, 25, 0);
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.startAxis = UnityEngine.UI.GridLayoutGroup.Axis.Horizontal;
        grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = width;

        // 🔧 Génère les slots
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject slotGO = Instantiate(slotPrefab, slotParent);
                slotGO.name = $"Slot_{x}_{y}";

                RectTransform rt = slotGO.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(40, 40);
                rt.localScale = Vector3.one;

                // 🎨 Rends les slots visibles
                var img = slotGO.GetComponent<UnityEngine.UI.Image>();
                if (img == null)
                    img = slotGO.AddComponent<UnityEngine.UI.Image>();

                img.color = new Color(1, 1, 1, 0.25f); // visible, semi-transparent
                img.raycastTarget = true;

                Slot slot = slotGO.GetComponent<Slot>();
                slot.Setup(x, y);
                slots[x, y] = slot;
            }
        }

        Debug.Log($"[ContainerInventory] Grille {width}x{height} initialisée ({slotParent.childCount} slots)");
    }

    public bool AddItem(ItemData data)
    {
        if (data == null) return false;
        GameObject go = Instantiate(itemUIPrefab, itemsLayer);
        ItemUI ui = go.GetComponent<ItemUI>();
        ui.Setup(data);
        items.Add(ui);

        if (FindFreePos(data, out int x, out int y))
        {
            PlaceItem(ui, x, y);
            return true;
        }

        Destroy(go);
        return false;
    }

    public bool FindFreePos(ItemData item, out int outX, out int outY)
    {
        for (int y = 0; y <= height - item.height; y++)
            for (int x = 0; x <= width - item.width; x++)
                if (CanPlaceItem(x, y, item))
                { outX = x; outY = y; return true; }
        outX = outY = -1;
        return false;
    }

    public bool CanPlaceItem(int startX, int startY, ItemData item)
    {
        if (item == null) return false;
        for (int yy = 0; yy < item.height; yy++)
            for (int xx = 0; xx < item.width; xx++)
            {
                int cx = startX + xx;
                int cy = startY + yy;
                if (cx >= width || cy >= height) return false;
                if (slots[cx, cy].HasItem()) return false;
            }
        return true;
    }

    public void PlaceItem(ItemUI ui, int x, int y)
    {
        if (ui == null || ui.itemData == null)
            return;

        ui.SetOccupiedSlots(x, y, ui.itemData.width, ui.itemData.height);
        foreach (var s in ui.occupiedSlots)
            if (s != null) s.SetItem(ui);

        // ✅ Parent correct (itemsLayer)
        ui.transform.SetParent(itemsLayer != null ? itemsLayer : slotParent, false);
        ui.transform.SetAsLastSibling();

        // 📏 Positionnement exact
        var grid = slotParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        float spacingX = grid ? grid.spacing.x : 0f;
        float spacingY = grid ? grid.spacing.y : 0f;
        float padLeft = grid ? grid.padding.left : 0f;
        float padTop = grid ? grid.padding.top : 0f;
        float cellW = 40f, cellH = 40f;

        ui.rectTransform.anchorMin = new Vector2(0, 1);
        ui.rectTransform.anchorMax = new Vector2(0, 1);
        ui.rectTransform.pivot = new Vector2(0, 1);

        float xPos = padLeft + x * (cellW + spacingX);
        float yPos = padTop + y * (cellH + spacingY);
        ui.rectTransform.anchoredPosition = new Vector2(xPos, -yPos);

        // ✅ Restaure l’interactivité du CanvasGroup
        var cg = ui.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = ui.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
        cg.interactable = true;
        cg.alpha = 1f;

        // ✅ Met à jour l’origine du drag
        var drag = ui.GetComponent<ItemDrag>();
        if (drag != null)
        {
            drag.itemUI = ui;
            drag.sourcePlayerInv = null;
            drag.sourceContainerInv = this;
        }

        // 🎨 Mise à jour visuelle
        ui.UpdateOutline();
        ui.UpdateSize();
        // ✅ Réactive le raycast complet après placement
        ui.EnableRaycastAfterDrop();
        ui.transform.SetAsLastSibling();
        ui.EnsureCanvasRaycastable();
    }

    public void RemoveItem(ItemUI ui)
    {
        if (ui == null) return;
        if (items.Contains(ui))
            items.Remove(ui);

        if (ui.occupiedSlots != null)
        {
            foreach (var s in ui.occupiedSlots)
                if (s != null) s.ClearItem();
        }

        Destroy(ui.gameObject);
    }

    // Détache l'item du conteneur sans détruire l'UI
    public void DetachWithoutDestroy(ItemUI ui)
    {
        if (ui == null) return;

        if (items.Contains(ui))
            items.Remove(ui);

        if (ui.occupiedSlots != null)
        {
            foreach (var s in ui.occupiedSlots)
                if (s != null) s.ClearItem();
        }

        ui.occupiedSlots = null;
        ui.currentSlot = null;
    }
}
