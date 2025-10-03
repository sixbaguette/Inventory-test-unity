using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public int width = 6;
    public int height = 4;
    public GameObject slotPrefab;
    public Transform slotParent;

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

    private void InitializeGrid()
    {
        slots = new Slot[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject slotGO = Instantiate(slotPrefab, slotParent); // <-- attache au slotParent
                Slot slot = slotGO.GetComponent<Slot>();
                slot.Setup(x, y); // <-- définit la position du slot
                slots[x, y] = slot;

                SlotDrop slotDrop = slotGO.GetComponent<SlotDrop>();
                if (slotDrop != null)
                {
                    slotDrop.x = x; // <-- indispensable pour le drag & drop
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

                // Si le slot est occupé mais c'est l'item qu'on déplace → on ignore
                if (slot.HasItem() && slot.GetItem() != ignoreItemUI)
                {
                    return false;
                }
            }
        }

        return true;
    }


    public bool PlaceItem(ItemUI itemUI, int startX, int startY)
    {
        Item item = itemUI.itemData;

        // Libère les anciens slots **avant** de tester
        if (itemUI.currentSlot != null)
        {
            foreach (var slot in itemUI.occupiedSlots)
                slot.ClearItem();
        }

        if (!CanPlaceItem(startX, startY, item))
        {
            // Re-assigner les anciens slots car on ne peut pas placer
            itemUI.SetOccupiedSlots(itemUI.currentSlot.x, itemUI.currentSlot.y, item.width, item.height);
            foreach (var slot in itemUI.occupiedSlots)
                slot.SetItem(itemUI);

            return false;
        }

        // Réattribue les nouveaux slots
        itemUI.SetOccupiedSlots(startX, startY, item.width, item.height);
        foreach (var slot in itemUI.occupiedSlots)
            slot.SetItem(itemUI);

        // Positionne précisément l’item
        Slot targetSlot = slots[startX, startY];
        itemUI.transform.SetParent(targetSlot.transform, false);
        itemUI.transform.localPosition = Vector3.zero;

        itemUI.currentSlot = targetSlot;

        return true;
    }

}
