using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    public int x;
    public int y;

    private ItemUI currentItem;
    private Image slotImage;

    private void Awake()
    {
        slotImage = GetComponent<Image>();
    }

    public void Setup(int posX, int posY)
    {
        x = posX;
        y = posY;
    }

    public void SetItem(ItemUI item)
    {
        currentItem = item;
    }

    public void ClearItem()
    {
        currentItem = null;
    }

    public bool HasItem()
    {
        return currentItem != null;
    }

    public ItemUI GetItem()
    {
        return currentItem;
    }

    // --- NOUVEAU ---
    public void Highlight(Color color)
    {
        if (slotImage != null)
            slotImage.color = color;
    }

    public void ResetHighlight()
    {
        if (slotImage != null)
            slotImage.color = Color.gray; // couleur par défaut
    }
}
