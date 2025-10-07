using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Item itemData;
    public Image icon;
    public Slot currentSlot;
    public Slot[] occupiedSlots;
    public int originSlotX;
    public int originSlotY;
    public Item item;
    public Image outline;


    private Tooltip tooltip;
    private float hoverTime = 2f;
    private float timer = 0f;
    private bool isHovering = false;
    private bool tooltipVisible = false;

    [HideInInspector]
    public RectTransform rectTransform { get; private set; }
    private bool originalSizeStored = false;
    public int originalSiblingIndex { get; private set; }
    [HideInInspector]
    public Vector2 originalSize;
    [HideInInspector]
    public Vector2 originalOutlineSize;
    [HideInInspector]
    public Transform originalParent;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        tooltip = FindFirstObjectByType<Tooltip>();

        if (itemData != null)
            Setup(itemData);

        StoreOriginalState();
    }

    public void Setup(Item newItemData)
    {
        itemData = newItemData;

        if (icon != null && itemData.icon != null)
        {
            icon.sprite = itemData.icon;
            icon.enabled = true;
        }

        UpdateOutline();
        UpdateSize();

        StoreOriginalState();
    }

    public void StoreOriginalState()
    {
        originalSize = rectTransform.sizeDelta;
        if (outline != null)
            originalOutlineSize = outline.rectTransform.sizeDelta;

        originalParent = transform.parent;
    }

    public void RestoreOriginalState()
    {
        rectTransform.sizeDelta = originalSize;
        if (outline != null)
            outline.rectTransform.sizeDelta = originalOutlineSize;

        transform.SetParent(originalParent, false);
    }

    public void ResizeTo(Vector2 newSize)
    {
        rectTransform.sizeDelta = newSize;
        UpdateOutline();
    }

    public void UpdateOutline()
    {
        if (outline == null || itemData == null || InventoryManager.Instance == null)
            return;

        RectTransform slotRect = InventoryManager.Instance.slots[0, 0].GetComponent<RectTransform>();
        Vector2 slotSize = slotRect.sizeDelta;

        float spacingX = 0f, spacingY = 0f;
        var grid = InventoryManager.Instance.slotParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (grid != null)
        {
            spacingX = grid.spacing.x;
            spacingY = grid.spacing.y;
        }

        Vector2 totalSize = new Vector2(
            (itemData.width * slotSize.x) + ((itemData.width - 1) * spacingX),
            (itemData.height * slotSize.y) + ((itemData.height - 1) * spacingY)
        );

        Vector2 margin = new Vector2(2, 2);
        outline.rectTransform.anchorMin = new Vector2(0, 1);
        outline.rectTransform.anchorMax = new Vector2(0, 1);
        outline.rectTransform.pivot = new Vector2(0, 1);
        outline.rectTransform.sizeDelta = totalSize + margin;
        outline.rectTransform.anchoredPosition = new Vector2(-margin.x / 2f, margin.y / 2f);
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

    public void UpdateSize()
    {
        RectTransform slotRect = InventoryManager.Instance.slots[0, 0].GetComponent<RectTransform>();
        Vector2 slotSize = slotRect.sizeDelta;

        float spacingX = 0f, spacingY = 0f;
        var grid = InventoryManager.Instance.slotParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (grid != null)
        {
            spacingX = grid.spacing.x;
            spacingY = grid.spacing.y;
        }

        rectTransform.sizeDelta = new Vector2(
            (itemData.width * slotSize.x) + ((itemData.width - 1) * spacingX),
            (itemData.height * slotSize.y) + ((itemData.height - 1) * spacingY)
        );

        UpdateOutline();
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
