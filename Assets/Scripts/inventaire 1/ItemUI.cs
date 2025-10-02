using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Item Data")]
    public Item itemData;      // Données de l'item (nom, description, icône)
    public Image icon;         // Icône de l'item affichée dans l'inventaire

    [Header("Tooltip")]
    private Tooltip tooltip;

    private float hoverTime = 2f;   // Temps avant d'afficher le tooltip
    private float timer = 0f;
    private bool isHovering = false;
    private bool tooltipVisible = false;

    private void Awake()
    {
        // Trouver le Tooltip dans la scène (pas besoin de le lier à chaque prefab)
        tooltip = FindFirstObjectByType<Tooltip>();

        // Si l’itemData est défini dans l’Inspector (ex: spawn auto par InventorySpawner)
        if (itemData != null)
        {
            Setup(itemData);
        }
    }

    /// <summary>
    /// Configure l'UI de l'item à partir de ses données
    /// </summary>
    public void Setup(Item newItemData)
    {
        itemData = newItemData;

        if (icon != null && itemData.icon != null)
        {
            icon.sprite = itemData.icon;
            icon.enabled = true;
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
