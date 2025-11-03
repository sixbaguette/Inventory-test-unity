using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TrashSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visuel")]
    [Tooltip("L’image principale (ton icône de poubelle).")]
    public Image iconImage;

    [Tooltip("Fond coloré qui s’affiche en survol.")]
    public Image highlightOverlay;

    [Header("Couleurs")]
    public Color overlayNormal = new Color(1f, 0f, 0f, 0f);      // invisible
    public Color overlayHighlight = new Color(1f, 0f, 0f, 0.35f); // rouge semi-transparent

    private void Start()
    {
        if (highlightOverlay != null)
            highlightOverlay.color = overlayNormal;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlightOverlay != null)
        {
            LeanTween.cancel(highlightOverlay.gameObject);
            LeanTween.value(highlightOverlay.gameObject, highlightOverlay.color, overlayHighlight, 0.15f)
                .setOnUpdate((Color c) => highlightOverlay.color = c);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlightOverlay != null)
        {
            LeanTween.cancel(highlightOverlay.gameObject);
            LeanTween.value(highlightOverlay.gameObject, highlightOverlay.color, overlayNormal, 0.15f)
                .setOnUpdate((Color c) => highlightOverlay.color = c);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        ItemUI droppedItem = eventData.pointerDrag?.GetComponent<ItemUI>();
        if (droppedItem == null)
            return;

        // Feedback rapide
        if (highlightOverlay != null)
        {
            LeanTween.cancel(highlightOverlay.gameObject);
            LeanTween.sequence()
                .append(LeanTween.value(highlightOverlay.gameObject, overlayHighlight, overlayNormal, 0.25f)
                    .setOnUpdate((Color c) => highlightOverlay.color = c));
        }

        // Supprime visuellement
        Destroy(droppedItem.gameObject);

        // Supprime logiquement (joueur ou container)
        if (InventoryManager.Instance != null &&
            droppedItem.transform.IsChildOf(InventoryManager.Instance.itemsLayer))
        {
            InventoryManager.Instance.RemoveItem(droppedItem);
        }
        else
        {
            var containerInv = droppedItem.GetComponentInParent<ContainerInventoryManager>();
            if (containerInv != null)
            {
                containerInv.RemoveItem(droppedItem);
                var container = containerInv.GetComponentInParent<Container>();
                if (container != null)
                    container.SaveFrom(containerInv);
            }
        }

        Debug.Log($"🗑️ Item {droppedItem.itemData?.itemName} supprimé définitivement !");
    }
}
