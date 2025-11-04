using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class TrashSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visuel")]
    public Image iconImage;
    public Image highlightOverlay;

    [Header("Couleurs")]
    public Color overlayNormal = new(1f, 0f, 0f, 0f);
    public Color overlayHighlight = new(1f, 0f, 0f, 0.35f);

    private Canvas myCanvas;
    private CanvasGroup group;

    void Awake()
    {
        // assure raycast valide
        myCanvas = GetComponentInParent<Canvas>();
        group = GetComponent<CanvasGroup>();

        if (myCanvas == null)
            myCanvas = gameObject.AddComponent<Canvas>();
        if (myCanvas.GetComponent<GraphicRaycaster>() == null)
            myCanvas.gameObject.AddComponent<GraphicRaycaster>();

        group.interactable = true;
        group.blocksRaycasts = true;
        group.alpha = 1f;

        if (iconImage) iconImage.raycastTarget = true;
        if (highlightOverlay) highlightOverlay.raycastTarget = true;

        if (highlightOverlay)
            highlightOverlay.color = overlayNormal;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!highlightOverlay) return;
        LeanTween.cancel(highlightOverlay.gameObject);
        LeanTween.value(highlightOverlay.gameObject, highlightOverlay.color, overlayHighlight, 0.12f)
            .setOnUpdate(c => highlightOverlay.color = c);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!highlightOverlay) return;
        LeanTween.cancel(highlightOverlay.gameObject);
        LeanTween.value(highlightOverlay.gameObject, highlightOverlay.color, overlayNormal, 0.12f)
            .setOnUpdate(c => highlightOverlay.color = c);
    }

    public void OnDrop(PointerEventData eventData)
    {
        var droppedItem = eventData?.pointerDrag?.GetComponent<ItemUI>();
        if (droppedItem == null) return;

        // Feedback
        if (highlightOverlay)
        {
            LeanTween.cancel(highlightOverlay.gameObject);
            LeanTween.sequence()
                .append(LeanTween.value(highlightOverlay.gameObject, overlayHighlight, overlayNormal, 0.25f)
                    .setOnUpdate(c => highlightOverlay.color = c));
        }

        bool removed = false;

        // 1) Quelle était la source réelle au moment du drag ?
        var drag = droppedItem.GetComponent<ItemDrag>();
        var playerInv = drag != null ? drag.sourcePlayerInv : droppedItem.GetComponentInParent<InventoryManager>();
        var contInv = drag != null ? drag.sourceContainerInv : droppedItem.GetComponentInParent<ContainerInventoryManager>();

        // 2) Supprime logiquement dans la bonne source
        if (playerInv != null)
        {
            playerInv.RemoveItem(droppedItem);
            removed = true;
        }
        else if (contInv != null)
        {
            contInv.RemoveItem(droppedItem);
            var container = contInv.GetComponentInParent<Container>();
            if (container != null) container.SaveFrom(contInv);
            removed = true;
        }
        else
        {
            // 3) Fallback ultra-sûr : libère les slots si on ne trouve pas la source
            if (droppedItem.occupiedSlots != null)
            {
                foreach (var s in droppedItem.occupiedSlots)
                    if (s != null) s.ClearItem();
            }
            droppedItem.currentSlot = null;
            removed = true;
        }

        // 4) Destruction visuelle
        if (removed)
        {
            Destroy(droppedItem.gameObject);
            Debug.Log($"🗑️ {droppedItem.itemData?.itemName} supprimé !");
        }
    }
}
