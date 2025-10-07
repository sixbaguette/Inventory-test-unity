using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipementSlot : MonoBehaviour, IDropHandler
{
    public string slotType; // ex: "Weapon", "Armor"
    public Image iconDisplay;

    private ItemUI currentItem;
    public ItemUI CurrentItem => currentItem;

    public bool IsCompatible(Item item)
    {
        // Si tu veux autoriser tout type d’item, remplace par : return item != null;
        return item != null && (string.IsNullOrEmpty(slotType) || item.equipmentType == slotType);
    }

    public void OnDrop(PointerEventData eventData)
    {
        var draggedItemUI = eventData.pointerDrag?.GetComponent<ItemUI>();
        if (draggedItemUI == null) return;

        var item = draggedItemUI.itemData;
        if (!IsCompatible(item)) return;

        // Si un item est déjà présent, on empêche le drop
        if (currentItem != null)
        {
            Debug.Log("⚠️ Slot déjà occupé !");
            return;
        }

        EquipItem(draggedItemUI);
    }

    private void EquipItem(ItemUI itemUI)
    {
        if (itemUI == null) return;

        // Sauvegarde l’état d’origine (taille, parent, ancrage, etc.)
        itemUI.StoreOriginalState();

        currentItem = itemUI;

        // On met le parent = ce slot
        itemUI.transform.SetParent(transform, false);

        // On force le centrage et la taille du slot
        RectTransform itemRect = itemUI.rectTransform;
        RectTransform slotRect = GetComponent<RectTransform>();

        itemRect.anchorMin = new Vector2(0.5f, 0.5f);
        itemRect.anchorMax = new Vector2(0.5f, 0.5f);
        itemRect.pivot = new Vector2(0.5f, 0.5f);
        itemRect.anchoredPosition = Vector2.zero;
        itemRect.sizeDelta = slotRect.sizeDelta;

        // Redimensionne et centre l’outline
        if (itemUI.outline != null)
        {
            RectTransform outlineRect = itemUI.outline.rectTransform;
            outlineRect.anchorMin = new Vector2(0.5f, 0.5f);
            outlineRect.anchorMax = new Vector2(0.5f, 0.5f);
            outlineRect.pivot = new Vector2(0.5f, 0.5f);
            outlineRect.anchoredPosition = Vector2.zero;
            outlineRect.sizeDelta = slotRect.sizeDelta;
        }

        // Optionnel : si tu veux cacher l’icône du slot quand quelque chose est équipé
        if (iconDisplay != null)
            iconDisplay.enabled = false;
    }

    public void UnequipItem()
    {
        if (currentItem == null) return;

        // ✅ Restaure taille + position + parent d’origine
        currentItem.RestoreOriginalState();

        // ✅ Nettoie la référence (sinon “Slot déjà occupé” persiste)
        currentItem = null;

        // ✅ Réactive l’icône du slot si besoin
        if (iconDisplay != null)
            iconDisplay.enabled = true;
    }

    // Appelée par ton ItemDrag quand on retire un objet du slot équipement
    public void ForceClear(ItemUI itemUI)
    {
        if (currentItem == itemUI)
        {
            UnequipItem();
        }
    }
}
