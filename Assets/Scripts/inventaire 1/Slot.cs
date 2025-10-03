using UnityEngine;

public class Slot : MonoBehaviour
{
    public int x;
    public int y;

    private ItemUI currentItem;

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
}
