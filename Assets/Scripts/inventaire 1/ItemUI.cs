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

    [Header("Hover visuals")]
    public Image hoverBackground;

    public Color hoverColor = new Color(1f, 0.9f, 0.3f, 0.35f); // jaune
    public Color normalColor = new Color(1f, 0.9f, 0.3f, 0f);

    public float hoverScale = 1.05f;
    public float hoverFadeTime = 0.1f;

    [HideInInspector] public bool isBeingDragged = false;

    public int currentAmmo = -1; // -1 = pas encore initialisé

    private UnityEngine.UI.Image hitbox; // image transparente raycastable

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        tooltip = FindFirstObjectByType<Tooltip>();
        StoreOriginalState(); // stocke l'état initial
        if (hoverBackground != null)
            hoverBackground.color = normalColor;
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
        ResetVisualLayout();
        StoreOriginalState();
        UpdateStackText();
        EnsureRaycastable();
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
            ResetVisualLayout();
        }
        if (hoverBackground != null)
        {
            hoverBackground.rectTransform.sizeDelta = rectTransform.sizeDelta;
            hoverBackground.rectTransform.anchoredPosition = Vector2.zero;
        }
        EnsureRaycastable();
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
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localEulerAngles = Vector3.zero;

            outline.raycastTarget = false; // ✅ ici aussi
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

        RectTransform ort = outline.rectTransform;

        // 🧩 Étire l'outline sur tout le parent ItemUI
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.pivot = new Vector2(0.5f, 0.5f);
        ort.anchoredPosition = Vector2.zero;
        ort.localEulerAngles = Vector3.zero;

        // 🟡 Bordure uniforme sur les 4 côtés
        float border = 2f;
        ort.offsetMin = new Vector2(-border, -border);
        ort.offsetMax = new Vector2(border, border);

        // ✨ Correction fine du subpixel
        var rt = rectTransform;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(Mathf.Round(rt.anchoredPosition.x), Mathf.Round(rt.anchoredPosition.y));

        // 🚫 Empêche l’outline de bloquer les clics
        outline.raycastTarget = false;
    }

    public void ResetVisualLayout()
    {
        // parent ItemUI (déjà positionné par InventoryManager)
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        // ICON : stretch plein parent, rotation 0
        if (icon != null)
        {
            var rt = icon.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localEulerAngles = Vector3.zero;
            icon.preserveAspect = false; // IMPORTANT sinon elle ne remplit pas
        }

        // OUTLINE : stretch plein parent (ou taille via UpdateOutline), rotation 0
        if (outline != null)
        {
            var ort = outline.rectTransform;
            // NE PAS réécrire anchorMin/Max/pivot ici !
            ort.anchoredPosition = Vector2.zero;
            ort.localEulerAngles = Vector3.zero;
        }

        EnsureRaycastable();
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
        // ✅ Quick transfer (Shift + clic gauche)
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                // 🔍 Vérifie si un conteneur est ouvert
                var containerInv = ContainerUIController.Instance?.GetActiveContainerInventory();
                bool containerOpen = (containerInv != null && containerInv.gameObject.activeInHierarchy);

                // =============================
                // 🟢 Cas 1 : conteneur ouvert → transfert entre inventaires
                // =============================
                if (containerOpen)
                {
                    bool isInPlayerInventory = GetComponentInParent<InventoryManager>() != null;
                    bool isInContainer = GetComponentInParent<ContainerInventoryManager>() != null;

                    if (isInPlayerInventory)
                    {
                        // joueur → conteneur
                        if (containerInv.TryAutoPlace(this))
                        {
                            InventoryManager.Instance.RemoveItem(this);
                            containerInv.AddToInventoryList(this);
                            Debug.Log($"[ShiftClick] Transféré {itemData.itemName} → conteneur");
                        }
                        else
                        {
                            Debug.Log("[ShiftClick] Pas d’espace dans le conteneur.");
                        }
                    }
                    else if (isInContainer)
                    {
                        // conteneur → joueur
                        if (InventoryManager.Instance.TryAutoPlace(this))
                        {
                            containerInv.RemoveItem(this);
                            InventoryManager.Instance.AddToInventoryList(this);
                            Debug.Log($"[ShiftClick] Transféré {itemData.itemName} → inventaire joueur");
                        }
                        else
                        {
                            Debug.Log("[ShiftClick] Inventaire plein.");
                        }
                    }

                    return;
                }

                // =============================
                // 🟣 Cas 2 : pas de conteneur ouvert → comportement d’équipement normal
                // =============================
                if (itemData.isEquipable && EquipementManager.Instance != null)
                {
                    EquipementManager.Instance.TryEquipItem(this);
                    return;
                }

                // =============================
                // 🟠 Cas 3 : fallback (inventaire vers inventaire interne)
                // =============================
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.QuickTransfer(this);
                    return;
                }
            }
        }

        // 🎯 Menu contextuel (clic droit)
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (ContextMenuUI.Instance != null)
                ContextMenuUI.Instance.Show(this, eventData.position);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isBeingDragged) return; // ne rien faire pendant drag

        // === FOND JAUNE ===
        if (hoverBackground != null)
        {
            hoverBackground.rectTransform.sizeDelta = rectTransform.sizeDelta;
            hoverBackground.rectTransform.anchoredPosition = Vector2.zero;
            hoverBackground.transform.SetAsFirstSibling();

            LeanTween.cancel(hoverBackground.gameObject);
            hoverBackground.color = normalColor;

            LeanTween.value(hoverBackground.gameObject, 0f, 1f, hoverFadeTime)
                .setOnUpdate((float val) =>
                {
                    Color c = hoverColor;
                    c.a = hoverColor.a * val;
                    hoverBackground.color = c;
                });
            LeanTween.scale(hoverBackground.rectTransform, Vector3.one * hoverScale, hoverFadeTime).setEaseOutCubic();
        }

        // === OUTLINE ===
        if (outline != null)
        {
            LeanTween.cancel(outline.gameObject);
            LeanTween.scale(outline.rectTransform, Vector3.one * hoverScale, hoverFadeTime).setEaseOutCubic();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isBeingDragged) return;

        if (hoverBackground != null)
        {
            LeanTween.cancel(hoverBackground.gameObject);
            LeanTween.value(hoverBackground.gameObject, hoverBackground.color.a, 0f, hoverFadeTime)
                .setOnUpdate((float val) =>
                {
                    Color c = hoverColor;
                    c.a = val;
                    hoverBackground.color = c;
                })
                .setOnComplete(() => { hoverBackground.color = normalColor; });
            LeanTween.scale(hoverBackground.rectTransform, Vector3.one, hoverFadeTime).setEaseInCubic();
        }

        if (outline != null)
        {
            LeanTween.cancel(outline.gameObject);
            LeanTween.scale(outline.rectTransform, Vector3.one, hoverFadeTime).setEaseInCubic();
        }
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

    public void ForceHideTooltip()
    {
        if (tooltip != null)
        {
            tooltip.HideAll();
        }
        tooltipVisible = false;
        isHovering = false;
        timer = 0f;
    }

    void Update()
    {
        if (isHovering && !tooltipVisible)
        {
            timer += Time.deltaTime;
            if (timer >= hoverTime)
            {
                // ⬇️ nouveau garde-fou
                if (tooltip != null && tooltip.CanShowHover())
                {
                    tooltip.Show(itemData);
                    tooltipVisible = true;
                }
            }
        }

        // 🔁 Rotation avec touche R
        if (isHovering && Input.GetKeyDown(KeyCode.R))
        {
            RotateItem();
        }
        HandleRotationInput();
    }

    private void EnsureRaycastable()
    {
        // 1) Racine : un Image ultra transparent pour capter les clics/drags
        if (hitbox == null)
            hitbox = GetComponent<UnityEngine.UI.Image>();
        if (hitbox == null)
            hitbox = gameObject.AddComponent<UnityEngine.UI.Image>();

        hitbox.raycastTarget = true;
        hitbox.maskable = false;
        if (hitbox.color.a < 0.001f)
            hitbox.color = new Color(1f, 1f, 1f, 0.001f); // quasi invisible mais “cliquable”

        // 2) L’icône aussi raycastable (au cas où)
        if (icon != null)
        {
            icon.raycastTarget = true;
            icon.maskable = false;
        }

        // 3) CanvasGroup : interactif et laisse passer les events
        var cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        cg.interactable = true;
        cg.blocksRaycasts = true;
        cg.alpha = 1f;
    }

    // Appelle ça à des moments clés :
    public void EnableRaycastAfterDrop()
    {
        EnsureRaycastable();
    }

    public void EnsureCanvasRaycastable()
    {
        // ⚙️ On s’assure juste que l’item est interactif dans son Canvas parent
        var cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
        cg.interactable = true;
        cg.alpha = 1f;

        // ⚙️ Hitbox cliquable (image quasi invisible)
        var img = GetComponent<UnityEngine.UI.Image>() ?? gameObject.AddComponent<UnityEngine.UI.Image>();
        if (img.color.a < 0.01f)
            img.color = new Color(1, 1, 1, 0.01f);
        img.raycastTarget = true;

        // ⚙️ Supprime tout Canvas local s’il existe (sécurité supplémentaire)
        var c = GetComponent<Canvas>();
        if (c != null) Destroy(c);

        var gr = GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (gr != null) Destroy(gr);
    }

    public void DisableExtraCanvasIfInInventory()
    {
        // ❌ Si l’item est dans une grille (joueur ou conteneur), pas de Canvas local
        var parentInv = GetComponentInParent<InventoryManager>();
        var parentCont = GetComponentInParent<ContainerInventoryManager>();

        if (parentInv != null || parentCont != null)
        {
            var localCanvas = GetComponent<Canvas>();
            if (localCanvas != null) Destroy(localCanvas);

            var gr = GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (gr != null) Destroy(gr);
        }
    }

    public void SetStack(int amount)
    {
        if (!itemData.isStackable) return;

        currentStack = Mathf.Clamp(amount, 1, itemData.maxStack);
        UpdateStackText();
    }
}
