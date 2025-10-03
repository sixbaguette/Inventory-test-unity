using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public GameObject slotPrefab;
    public Transform gridPanel;
    public int width = 6;
    public int height = 5;

    [HideInInspector] public Slot[,] slots;

    public static float slotSize = 100f; // Taille d’un slot en pixels

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreateGrid();
    }

    public void CreateGrid()
    {
        slots = new Slot[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject slotObj = Instantiate(slotPrefab, gridPanel);
                Slot slot = slotObj.GetComponent<Slot>();
                slot.Setup(x, y);
                slots[x, y] = slot;
            }
        }

        Debug.Log($"[InventoryManager] Grille créée {width}x{height} (total {width * height} slots)");
    }

    public bool CanPlaceItem(int startX, int startY, Item item)
    {
        if (startX + item.width > width || startY + item.height > height)
            return false;

        for (int y = 0; y < item.height; y++)
        {
            for (int x = 0; x < item.width; x++)
            {
                if (!slots[startX + x, startY + y].IsEmpty)
                    return false;
            }
        }

        return true;
    }

    public bool PlaceItem(ItemUI itemUI, int startX, int startY)
    {
        if (!CanPlaceItem(startX, startY, itemUI.itemData))
            return false;

        for (int y = 0; y < itemUI.itemData.height; y++)
        {
            for (int x = 0; x < itemUI.itemData.width; x++)
            {
                slots[startX + x, startY + y].currentItem = itemUI;
            }
        }

        RectTransform rt = itemUI.GetComponent<RectTransform>();
        rt.SetParent(slots[startX, startY].transform, false);
        rt.anchoredPosition = Vector2.zero;

        return true;
    }
}
