using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipementSlot : MonoBehaviour, IDropHandler
{
    [Header("Type d’équipement accepté")]
    public Image iconDisplay;
    public EquipSlotType slotType = EquipSlotType.None;  // plus de string ici

    private ItemUI currentItem;
    public ItemUI CurrentItem => currentItem;

    public bool IsCompatible(ItemData item)
    {
        if (item == null) return false;
        if (!item.isEquipable) return false;

        // si le slot accepte tout
        if (slotType == EquipSlotType.None) return true;

        // comparaison enum <-> enum (pas de string)
        return item.equipSlotType == slotType;
    }

    public void OnDrop(PointerEventData eventData)
    {
        var droppedUI = eventData.pointerDrag?.GetComponent<ItemUI>();
        if (droppedUI == null) return;

        if (!IsCompatible(droppedUI.itemData))
            return;

        // Si un item existe déjà → on le renvoie dans l’inventaire
        if (currentItem != null)
        {
            UnequipItem();
        }

        // Nettoie les slots de la grille que l’objet occupait
        if (droppedUI.occupiedSlots != null)
        {
            foreach (var s in droppedUI.occupiedSlots)
                if (s != null) s.ClearItem();
        }
        droppedUI.currentSlot = null;
        droppedUI.occupiedSlots = null;

        // Équipe proprement
        EquipItem(droppedUI);
        // À la fin de UnequipItem()
        if (MiniTooltipUI.Instance != null) MiniTooltipUI.Instance.HideInstant();
    }

    public void EquipItem(ItemUI itemUI)
    {
        if (itemUI == null) return;

        itemUI.StoreOriginalState();
        currentItem = itemUI;

        // === 1️⃣ Vérifie et convertit la position si l'item change de Canvas ===
        Canvas slotCanvas = GetComponentInParent<Canvas>();
        Canvas itemCanvas = itemUI.GetComponentInParent<Canvas>();

        if (slotCanvas != null && itemCanvas != slotCanvas)
        {
            // Conversion de la position écran → locale dans le nouveau canvas
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(itemCanvas.worldCamera, itemUI.rectTransform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                slotCanvas.transform as RectTransform,
                screenPos,
                slotCanvas.worldCamera,
                out var localPos);

            itemUI.rectTransform.position = slotCanvas.transform.TransformPoint(localPos);
            itemUI.transform.SetParent(slotCanvas.transform, true);
        }

        // === 2️⃣ Place ensuite l'item dans le slot ===
        itemUI.transform.SetParent(transform, false);

        // === 3️⃣ Recalibrage du RectTransform ===
        RectTransform it = itemUI.rectTransform;
        RectTransform sr = GetComponent<RectTransform>();

        it.localScale = Vector3.one;
        it.anchorMin = it.anchorMax = it.pivot = new Vector2(0.5f, 0.5f);
        it.anchoredPosition3D = Vector3.zero;
        it.localPosition = Vector3.zero;
        it.sizeDelta = sr.sizeDelta;

        // === 4️⃣ Met à jour visuel ===
        if (itemUI.icon != null)
        {
            itemUI.icon.enabled = true;
            itemUI.icon.rectTransform.sizeDelta = sr.sizeDelta;
        }

        if (itemUI.outline != null)
        {
            var outlineRT = itemUI.outline.rectTransform;
            outlineRT.anchorMin = outlineRT.anchorMax = outlineRT.pivot = new Vector2(0.5f, 0.5f);
            outlineRT.anchoredPosition = Vector2.zero;

            // 🧩 ajuste pile à la taille du slot
            outlineRT.sizeDelta = sr.sizeDelta;

            // Optionnel : léger dépassement visuel (contour visible sans dépasser)
            // outlineRT.sizeDelta += new Vector2(2, 2);

            itemUI.outline.enabled = true;
        }

        if (iconDisplay != null)
            iconDisplay.enabled = false;

        // === 5️⃣ Devant visuellement ===
        itemUI.transform.SetAsLastSibling();
        itemUI.ResetVisualLayout();
    }

    public void UnequipItem()
    {
        if (currentItem == null) return;

        var item = currentItem;
        currentItem = null;

        var inv = InventoryManager.Instance;
        if (inv == null || inv.itemsLayer == null)
        {
            Debug.LogWarning("[Unequip] InventoryManager/itemsLayer manquant");
            return;
        }

        // Reparent vers la couche ItemsLayer du canvas d’inventaire
        Canvas targetCanvas = inv.itemsLayer.GetComponentInParent<Canvas>();
        Canvas itemCanvas = item.GetComponentInParent<Canvas>();

        if (targetCanvas != null && itemCanvas != targetCanvas)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
                itemCanvas ? itemCanvas.worldCamera : null,
                item.rectTransform.position
            );
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetCanvas.transform as RectTransform,
                screenPos,
                targetCanvas.worldCamera,
                out var localPos
            );

            item.transform.SetParent(inv.itemsLayer.transform, false);
            item.rectTransform.localPosition = localPos;
        }
        else
        {
            item.transform.SetParent(inv.itemsLayer.transform, false);
        }

        // Sécurité UI
        var cg = item.GetComponent<CanvasGroup>();
        if (cg) cg.blocksRaycasts = true;

        // Nettoyages visuels
        item.UpdateSize();
        item.UpdateOutline();
        item.ResetVisualLayout();

        // Placement (avec rotation auto si nécessaire)
        if (!inv.TryAutoPlace(item))
        {
            // Si aucune place -> fallback : restore ou autre logique (drop, message, etc.)
            item.RestoreOriginalState();
            Debug.Log("[Unequip] Aucune place disponible (toutes orientations).");
        }

        if (iconDisplay != null) iconDisplay.enabled = true;
        // À la fin de UnequipItem()
        if (MiniTooltipUI.Instance != null) MiniTooltipUI.Instance.HideInstant();
    }

    public void ForceClear(ItemUI itemUI)
    {
        if (currentItem == itemUI)
        {
            currentItem = null;
            if (iconDisplay != null)
                iconDisplay.enabled = true;

            // ✅ remet dans la hiérarchie d’inventaire correcte (ItemsLayer, pas OverlayCanvas)
            if (InventoryManager.Instance != null && InventoryManager.Instance.itemsLayer != null)
            {
                itemUI.transform.SetParent(InventoryManager.Instance.itemsLayer.transform, true);
                itemUI.UpdateSize();
                itemUI.UpdateOutline();
            }
        }
        itemUI.ResetVisualLayout();
    }

    public void DropEquippedItem()
    {
        if (currentItem == null)
        {
            Debug.LogWarning("[EquipementSlot] Aucun item à drop.");
            return;
        }

        // 1) Cache la ref puis libère le slot
        ItemUI itemToDrop = currentItem;
        currentItem = null;
        if (iconDisplay != null) iconDisplay.enabled = true;

        // 🔥 FORCER la désinstanciation SI cet item est en main
        var hotbar = FindFirstObjectByType<HotbarManager>();
        if (hotbar != null && hotbar.playerEquipHandler != null)
        {
            hotbar.playerEquipHandler.UnequipIfHolding(itemToDrop.itemData);
        }
        else
        {
            // fallback si jamais pas de handler
            var peh = FindFirstObjectByType<PlayerEquipHandler>();
            if (peh != null) peh.UnequipIfHolding(itemToDrop.itemData);
        }

        // 3) Reparent vers l’inventaire + enregistre-le (comme tu fais déjà)
        var inv = InventoryManager.Instance;
        if (inv != null && inv.itemsLayer != null)
        {
            if (itemToDrop.occupiedSlots != null)
            {
                foreach (var s in itemToDrop.occupiedSlots) s?.ClearItem();
            }
            itemToDrop.currentSlot = null;
            itemToDrop.occupiedSlots = null;

            itemToDrop.transform.SetParent(inv.itemsLayer, false);
            inv.AddToInventoryList(itemToDrop);
        }

        // 4) Drop via système habituel
        var ppm = FindFirstObjectByType<PlayerPickupManager>();
        if (ppm != null)
        {
            ppm.DropSpecificItem(itemToDrop);
        }
        else
        {
            Debug.LogWarning("[EquipementSlot] Aucun PlayerPickupManager trouvé, suppression directe.");
            InventoryManager.Instance.RemoveItem(itemToDrop);
        }

        Debug.Log($"[EquipementSlot] {itemToDrop.itemData.itemName} droppé depuis un slot d’équipement.");
    }

    /// <summary>
    /// Vide le slot sans replacer l’item dans l’inventaire (utile pour drop stack)
    /// </summary>
    public void ForceClearSlot()
    {
        if (currentItem == null) return;

        ItemUI itemToDestroy = currentItem;
        currentItem = null;

        if (iconDisplay != null) iconDisplay.enabled = true;

        // 🔥 s'assure que le visuel correspondant disparaît
        var peh = FindFirstObjectByType<PlayerEquipHandler>();
        if (peh != null) peh.UnequipIfHolding(itemToDestroy.itemData);

        if (itemToDestroy != null)
            Destroy(itemToDestroy.gameObject);

        Debug.Log("[EquipementSlot] Slot vidé sans retour inventaire.");
    }

    /// <summary>
    /// Réactualise visuellement l’objet déjà équipé sans le déséquiper.
    /// Utile après un drop partiel de stack.
    /// </summary>
    public void ForceRefreshVisual(ItemUI item)
    {
        if (item == null) return;

        currentItem = item;
        if (iconDisplay != null)
            iconDisplay.enabled = false;

        item.transform.SetParent(transform, false);
        item.rectTransform.localScale = Vector3.one;
        item.rectTransform.anchoredPosition3D = Vector3.zero;

        item.UpdateStackText();
        item.UpdateOutline();
        item.ResetVisualLayout();

        // ✨ Feedback visuel léger (flash jaune rapide)
        Image icon = item.icon;
        if (icon != null)
        {
            var c = icon.color;
            icon.color = Color.yellow;
            LeanTween.value(icon.gameObject, (float v) =>
            {
                icon.color = Color.Lerp(Color.yellow, c, v);
            }, 0f, 1f, 0.25f).setEaseOutCubic();
        }
    }

    public void BeginDragDetach(ItemUI itemUI)
    {
        if (currentItem == itemUI)
        {
            currentItem = null;
            if (iconDisplay != null) iconDisplay.enabled = true; // show the placeholder icon again
                                                                 // IMPORTANT: do NOT reparent itemUI here.
        }
    }
}
