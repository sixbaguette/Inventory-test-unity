using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Item itemData;
    public Image icon;
    public Slot currentSlot;
    public Slot[] occupiedSlots;

    private Tooltip tooltip;

    private float hoverTime = 2f;
    private float timer = 0f;
    private bool isHovering = false;
    private bool tooltipVisible = false;

    [HideInInspector]
    public RectTransform rectTransform { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // Trouver le Tooltip dans la scène
        tooltip = FindFirstObjectByType<Tooltip>();

        if (itemData != null)
        {
            Setup(itemData);
        }
    }


    public void Setup(Item newItemData)
    {
        itemData = newItemData;
        if (icon != null && itemData.icon != null)
        {
            icon.sprite = itemData.icon;
            icon.enabled = true;
        }
    }

    public void SetOccupiedSlots(int startX, int startY, int width, int height)
    {
        occupiedSlots = new Slot[width * height];

        int index = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                occupiedSlots[index] = InventoryManager.Instance.slots[startX + x, startY + y];
                index++;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        timer = 0f;
        tooltipVisible = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (tooltipVisible && tooltip != null)
            tooltip.Hide();
        tooltipVisible = false;
    }

    private void Update()
    {
        if (tooltip == null || itemData == null) return;
        if (isHovering && !tooltipVisible)
        {
            timer += Time.deltaTime;
            if (timer >= hoverTime)
            {
                tooltip.Show(itemData);
                tooltipVisible = true;
            }
        }
    }
}
