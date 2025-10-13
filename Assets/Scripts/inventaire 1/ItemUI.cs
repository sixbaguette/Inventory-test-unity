using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public ItemData itemData;
    public Image icon;
    public Image outline;
    public Slot currentSlot;
    public Slot[] occupiedSlots;
    public int currentStack = 1; // quantité actuelle
    public TextMeshProUGUI stackText; // texte affiché sur l’icône

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
        currentStack = 1;
        if (icon != null && itemData.icon != null)
        {
            icon.sprite = itemData.icon;
            icon.enabled = true;
        }

        EnsureChildLayout();
        UpdateSize();
        UpdateOutline();
        StoreOriginalState();
        UpdateStackText();
    }


    public void UpdateStackText()
    {
        if (stackText == null)
        {
            // essaie de le trouver automatiquement
            stackText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (stackText != null)
        {
            if (itemData.isStackable && currentStack > 1)
            {
                stackText.text = currentStack.ToString();
                stackText.enabled = true;
            }
            else
            {
                stackText.text = "";
                stackText.enabled = false;
            }
        }
    }

    public int AddToStack(int amount)
    {
        if (!itemData.isStackable) return amount; // non stackable → on ignore
        int spaceLeft = itemData.maxStack - currentStack;
        int added = Mathf.Min(amount, spaceLeft);
        currentStack += added;
        UpdateStackText();
        return amount - added; // retourne le "reste"
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
        if (itemData == null) return;
        if (InventoryManager.Instance == null) return;
        if (InventoryManager.Instance.slots == null) return;
        if (InventoryManager.Instance.slots.Length == 0) return;

        Slot firstSlot = InventoryManager.Instance.slots[0, 0];
        if (firstSlot == null) return;

        RectTransform slotRect = firstSlot.GetComponent<RectTransform>();
        if (slotRect == null) return;

        Vector2 slotSize = slotRect.sizeDelta;
        float spacingX = 0f;
        float spacingY = 0f;

        var grid = InventoryManager.Instance.slotParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (grid != null)
        {
            spacingX = grid.spacing.x;
            spacingY = grid.spacing.y;
        }

        // taille finale de l'item dans la grille
        Vector2 newSize = new Vector2(
            (itemData.width * slotSize.x) + ((itemData.width - 1) * spacingX),
            (itemData.height * slotSize.y) + ((itemData.height - 1) * spacingY)
        );

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        // ✅ 1) taille du conteneur ItemUI
        rectTransform.sizeDelta = newSize;
        EnsureChildLayout();
        // ✅ 2) FAIT SUIVRE L’ICÔNE exactement à la taille de l’ItemUI
        if (icon != null)
        {
            var irt = icon.rectTransform;
            irt.anchorMin = new Vector2(0.5f, 0.5f);
            irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = Vector2.zero;
            irt.sizeDelta = newSize;              // <- point clé
                                                  // (optionnel) si tu utilises PreserveAspect, laisse coché, sinon décoche
        }
    }

    public void EnsureChildLayout()
    {
        if (icon != null)
        {
            var rt = icon.rectTransform;
            rt.anchorMin = Vector2.zero;   // stretch
            rt.anchorMax = Vector2.one;    // stretch
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localEulerAngles = Vector3.zero;
            icon.preserveAspect = false;   // IMPORTANT pour remplir le parent
        }

        if (outline != null)
        {
            var rt = outline.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localEulerAngles = Vector3.zero;
        }
    }

    public void RotateItem()
    {
        if (itemData == null || InventoryManager.Instance == null)
            return;

        // Sauvegarde dimensions
        int oldWidth = itemData.width;
        int oldHeight = itemData.height;

        // Échange largeur / hauteur
        itemData.width = oldHeight;
        itemData.height = oldWidth;

        // Vérifie si l’item peut être replacé à la même position
        if (currentSlot != null)
        {
            int startX = currentSlot.x;
            int startY = currentSlot.y;

            bool canPlace = InventoryManager.Instance.CanPlaceItem(startX, startY, itemData, this);
            if (!canPlace)
            {
                // revert si rotation impossible
                itemData.width = oldWidth;
                itemData.height = oldHeight;
                Debug.Log($"[Rotate] Rotation impossible : pas assez de place pour {itemData.itemName}");
                return;
            }

            // Repositionne avec nouvelle taille
            InventoryManager.Instance.PlaceItem(this, startX, startY);
        }

        // Actualise visuel
        UpdateSize();
        UpdateOutline();

        Debug.Log($"[Rotate] {itemData.itemName} tourné de 90° ({itemData.width}x{itemData.height})");
    }

    public void UpdateOutline()
    {
        if (outline == null || itemData == null) return;

        // On part de la taille réelle de l’ItemUI (celle déjà arrondie par UpdateSize)
        Vector2 itemSize = rectTransform.sizeDelta;

        // ⚙️ Nettoyage des arrondis Unity
        itemSize.x = Mathf.Round(itemSize.x);
        itemSize.y = Mathf.Round(itemSize.y);

        RectTransform ort = outline.rectTransform;

        // ✅ ancrage cohérent avec la grille (haut-gauche)
        ort.anchorMin = new Vector2(0, 1);
        ort.anchorMax = new Vector2(0, 1);
        ort.pivot = new Vector2(0, 1);

        // ✅ toujours parfaitement superposé à l’item
        ort.anchoredPosition = Vector2.zero;

        // ✅ marge fixe de 2px (modifie si tu veux exact)
        float margin = 2f;
        ort.sizeDelta = itemSize + new Vector2(margin, margin);
    }


    public void SetOccupiedSlots(int startX, int startY, int width, int height)
    {
        var inv = InventoryManager.Instance;
        if (inv == null || inv.slots == null) return;

        // 🔒 bornes de sécurité
        startX = Mathf.Clamp(startX, 0, inv.width - width);
        startY = Mathf.Clamp(startY, 0, inv.height - height);

        occupiedSlots = new Slot[width * height];
        int index = 0;

        for (int yy = 0; yy < height; yy++)
        {
            for (int xx = 0; xx < width; xx++)
            {
                int sx = startX + xx;
                int sy = startY + yy;

                // évite les dépassements
                if (sx < 0 || sy < 0 || sx >= inv.width || sy >= inv.height)
                {
                    Debug.LogWarning($"[SetOccupiedSlots] dépassement ignoré ({sx},{sy})");
                    continue;
                }

                Slot slot = inv.slots[sx, sy];
                occupiedSlots[index++] = slot;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (itemData.isStackable && currentStack > 1)
            {
                SplitStackWindow.Instance?.Open(this);
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

    private ItemUI GetItemUnderMouse()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
            return null;

        var pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);

        foreach (var r in results)
        {
            var item = r.gameObject.GetComponentInParent<ItemUI>();
            if (item != null)
                return item;
        }

        return null;
    }
    private void HandleRotationInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ItemUI hoveredItem = GetItemUnderMouse();
            if (hoveredItem != null)
            {
                hoveredItem.RotateItem();
            }
        }
    }

    void Update()
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

        // 🔁 Rotation avec touche R
        if (isHovering && Input.GetKeyDown(KeyCode.R))
        {
            RotateItem();
        }
        HandleRotationInput();
    }
}
