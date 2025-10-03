using UnityEngine;
using UnityEngine.EventSystems;

public class ItemDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector2 originalPosition;

    private Canvas canvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[ItemDrag] Canvas introuvable !");
            }
        }
    }



    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;

        transform.SetParent(canvas.transform, true); // Permet d’être au-dessus des autres éléments
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;
    }


    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        GameObject hovered = eventData.pointerCurrentRaycast.gameObject;
        if (hovered != null && hovered.GetComponent<SlotDrop>() != null)
        {
            transform.SetParent(hovered.transform, false);
            rectTransform.anchoredPosition = Vector2.zero;
        }
        else
        {
            transform.SetParent(originalParent, false);
            rectTransform.anchoredPosition = originalPosition;
        }

        canvasGroup.blocksRaycasts = true;
    }


    private SlotDrop GetSlotUnderPointer(PointerEventData eventData)
    {
        foreach (Slot slot in InventoryManager.Instance.slots)
        {
            SlotDrop slotDrop = slot.GetComponent<SlotDrop>();
            if (slotDrop != null)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(
                        slotDrop.GetComponent<RectTransform>(),
                        eventData.position,
                        eventData.pressEventCamera))
                {
                    return slotDrop;
                }
            }
        }
        return null;
    }

    private void ReturnToOriginal()
    {
        transform.SetParent(originalParent, false);
        rectTransform.anchoredPosition = originalPosition;
    }
}
