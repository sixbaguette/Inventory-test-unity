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
    }

    private void EquipItem(ItemUI itemUI)
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
            itemUI.outline.enabled = true;
            itemUI.outline.rectTransform.sizeDelta = sr.sizeDelta + new Vector2(4, 4);
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

        // ✅ Replace dans la grille
        if (InventoryManager.Instance.FindFirstFreePosition(item.itemData, out int x, out int y))
        {
            InventoryManager.Instance.PlaceItem(item, x, y);
        }
        else
        {
            item.RestoreOriginalState();
        }

        if (iconDisplay != null)
            iconDisplay.enabled = true;
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
}
