using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Item itemData;
    public Image icon;

    private Tooltip tooltip;

    private float hoverTime = 2f;
    private float timer = 0f;
    private bool isHovering = false;
    private bool tooltipVisible = false;

    private void Awake()
    {
        tooltip = FindFirstObjectByType<Tooltip>();
    }

    public void Setup(Item newItemData)
    {
        itemData = newItemData;
        icon.sprite = itemData.icon;
        icon.enabled = true;
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
