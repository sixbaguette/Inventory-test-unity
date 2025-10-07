using UnityEngine;
using UnityEngine.UIElements;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public int width = 6;
    public int height = 4;
    public GameObject slotPrefab;
    public Transform slotParent;
    public Canvas overlayCanvas;

    [HideInInspector]
    public Slot[,] slots;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializeGrid();
    }

    public bool AddItem(Item item)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (CanPlaceItem(x, y, item, null))
                {
                    PlaceItem(itemUI: null, x, y);
                    return true;
                }
            }
        }
        return false; // Pas d’espace libre
    }

    private void InitializeGrid()
    {
        slots = new Slot[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject slotGO = Instantiate(slotPrefab, slotParent);
                Slot slot = slotGO.GetComponent<Slot>();
                slot.Setup(x, y);
                slots[x, y] = slot;

                SlotDrop slotDrop = slotGO.GetComponent<SlotDrop>();
                if (slotDrop != null)
                {
                    slotDrop.x = x;
                    slotDrop.y = y;
                }
            }
        }
    }
    
    public bool CanPlaceItem(int startX, int startY, Item item, ItemUI ignoreItemUI = null)
    {
        if (item == null) return false;

        for (int y = 0; y < item.height; y++)
        {
            for (int x = 0; x < item.width; x++)
            {
                int checkX = startX + x;
                int checkY = startY + y;

                if (checkX >= width || checkY >= height) return false;

                Slot slot = slots[checkX, checkY];

                if (slot.HasItem() && slot.GetItem() != ignoreItemUI)
                    return false;
            }
        }

        return true;
    }


    public bool PlaceItem(ItemUI itemUI, int startX, int startY)
    {
        if (itemUI == null || itemUI.itemData == null) return false;

        Item item = itemUI.itemData;
        Slot previousSlot = itemUI.currentSlot;

        if (itemUI.occupiedSlots != null)
        {
            foreach (var slot in itemUI.occupiedSlots)
                slot.ClearItem();
        }

        if (!CanPlaceItem(startX, startY, item, itemUI))
        {
            if (previousSlot != null)
                itemUI.SetOccupiedSlots(previousSlot.x, previousSlot.y, item.width, item.height);

            foreach (var slot in itemUI.occupiedSlots)
                slot.SetItem(itemUI);

            return false;
        }

        itemUI.SetOccupiedSlots(startX, startY, item.width, item.height);

        foreach (var slot in itemUI.occupiedSlots)
            slot.SetItem(itemUI);

        Vector3 minPos = itemUI.occupiedSlots[0].transform.localPosition;
        Vector3 maxPos = minPos;

        foreach (var slot in itemUI.occupiedSlots)
        {
            Vector3 pos = slot.transform.localPosition;
            minPos = Vector3.Min(minPos, pos);
            maxPos = Vector3.Max(maxPos, pos);
        }

        Vector3 center = (minPos + maxPos) / 2f;

        Slot targetSlot = slots[startX, startY];
        itemUI.transform.SetParent(targetSlot.transform.parent, false);
        itemUI.transform.localPosition = center;
        itemUI.currentSlot = targetSlot;
        itemUI.UpdateOutline();

        itemUI.transform.SetParent(overlayCanvas.transform, true);

        return true;
    }
}
