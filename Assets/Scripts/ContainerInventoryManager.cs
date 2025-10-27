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
    private List<ItemUI> items = new List<ItemUI>();

    public void InitializeGrid()
    {
        if (slotParent == null)
        {
            Debug.LogError("[ContainerInventory] slotParent non assigné !");
            return;
        }

        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        slots = new Slot[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject slotGO = Instantiate(slotPrefab, slotParent);
                RectTransform rt = slotGO.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(40, 40);
                rt.localScale = Vector3.one;
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
        ui.SetOccupiedSlots(x, y, ui.itemData.width, ui.itemData.height);
        foreach (var s in ui.occupiedSlots) if (s != null) s.SetItem(ui);

        ui.transform.SetParent(itemsLayer, false);

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
        ui.UpdateOutline();
        ui.UpdateSize();
    }
}
