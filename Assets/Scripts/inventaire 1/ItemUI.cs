using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ItemData itemData;
    public Image icon;
    public Image outline;
    public Slot currentSlot;
    public Slot[] occupiedSlots;

    [HideInInspector] public RectTransform rectTransform { get; private set; }

    // sauvegarde état d'origine (avant equip / resize)
    private Vector2 originalSize;
    private Vector2 originalOutlineSize;
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;
    private Vector2 originalPivot;
    private Vector2 originalAnchoredPos;
    private Transform originalParent;

    private Tooltip tooltip;
    private bool isHovering = false;
    private bool tooltipVisible = false;
    private float hoverTime = 1f;
    private float timer = 0f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        tooltip = FindFirstObjectByType<Tooltip>();
        StoreOriginalState(); // stocke l'état initial
    }

    public void Setup(ItemData newItemData)
    {
        itemData = newItemData;
        if (icon != null && itemData.icon != null)
        {
            icon.sprite = itemData.icon;
            icon.enabled = true;
        }

        UpdateSize();
        UpdateOutline();
        StoreOriginalState();
    }

    public void StoreOriginalState()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        originalSize = rectTransform.sizeDelta;
        if (outline != null) originalOutlineSize = outline.rectTransform.sizeDelta;
        originalAnchorMin = rectTransform.anchorMin;
        originalAnchorMax = rectTransform.anchorMax;
        originalPivot = rectTransform.pivot;
        originalAnchoredPos = rectTransform.anchoredPosition;
        originalParent = transform.parent;
    }

    public void RestoreOriginalState()
    {
        rectTransform.SetParent(originalParent, false);
        rectTransform.anchorMin = originalAnchorMin;
        rectTransform.anchorMax = originalAnchorMax;
        rectTransform.pivot = originalPivot;
        rectTransform.anchoredPosition = originalAnchoredPos;
        rectTransform.sizeDelta = originalSize;
        if (outline != null) outline.rectTransform.sizeDelta = originalOutlineSize;
        UpdateOutline();
    }

    public void UpdateSize()
    {
        // 🔒 Sécurité : empêche les nulls et erreurs d'ordre d'initialisation
        if (itemData == null) return;
        if (InventoryManager.Instance == null) return;
        if (InventoryManager.Instance.slots == null) return;
        if (InventoryManager.Instance.slots.Length == 0) return;

        // ✅ Récupère un slot de référence (0,0)
        Slot firstSlot = InventoryManager.Instance.slots[0, 0];
        if (firstSlot == null) return;

        RectTransform slotRect = firstSlot.GetComponent<RectTransform>();
        if (slotRect == null) return;

        // ✅ Taille d'un slot + spacing
        Vector2 slotSize = slotRect.sizeDelta;
        float spacingX = 0f;
        float spacingY = 0f;

        var grid = InventoryManager.Instance.slotParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (grid != null)
        {
            spacingX = grid.spacing.x;
            spacingY = grid.spacing.y;
        }

        // ✅ Calcul final de la taille de l'item (en tenant compte du spacing)
        Vector2 newSize = new Vector2(
            (itemData.width * slotSize.x) + ((itemData.width - 1) * spacingX),
            (itemData.height * slotSize.y) + ((itemData.height - 1) * spacingY)
        );

        // ✅ Application sur le RectTransform principal
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        rectTransform.sizeDelta = newSize;

        // ✅ Mets aussi à jour le contour (outline) si présent
        if (outline != null)
        {
            outline.rectTransform.sizeDelta = newSize + new Vector2(2, 2);
            outline.rectTransform.anchoredPosition = new Vector2(-1, 1);
        }
    }



    public void UpdateOutline()
    {
        if (outline == null || itemData == null) return;

        RectTransform slotRect = InventoryManager.Instance.slots[0, 0].GetComponent<RectTransform>();
        Vector2 slotSize = slotRect.sizeDelta;

        float spacingX = 0f, spacingY = 0f;
        var grid = InventoryManager.Instance.slotParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (grid != null) { spacingX = grid.spacing.x; spacingY = grid.spacing.y; }

        Vector2 totalSize = new Vector2(
            (itemData.width * slotSize.x) + ((itemData.width - 1) * spacingX),
            (itemData.height * slotSize.y) + ((itemData.height - 1) * spacingY)
        );

        // Centré dans le parent
        outline.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        outline.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        outline.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        outline.rectTransform.anchoredPosition = Vector2.zero;

        // Ajuste la taille + marge
        Vector2 margin = new Vector2(4, 4);
        outline.rectTransform.sizeDelta = totalSize + margin;
    }


    public void SetOccupiedSlots(int startX, int startY, int width, int height)
    {
        occupiedSlots = new Slot[width * height];
        int index = 0;
        for (int yy = 0; yy < height; yy++)
        {
            for (int xx = 0; xx < width; xx++)
            {
                occupiedSlots[index] = InventoryManager.Instance.slots[startX + xx, startY + yy];
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
        if (tooltipVisible && tooltip != null) tooltip.Hide();
        tooltipVisible = false;
    }

    void Update()
    {
        if (isHovering && Input.GetKeyDown(KeyCode.P))
        {
            // Drop cet item particulier
            InventoryManager.Instance.RemoveItem(this);
            Vector3 spawnPos = PlayerController.Instance.transform.position +
                               PlayerController.Instance.transform.forward * 1.5f + Vector3.up * 0.3f;
            Instantiate(itemData.worldPrefab, spawnPos, Quaternion.identity);
        }

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
