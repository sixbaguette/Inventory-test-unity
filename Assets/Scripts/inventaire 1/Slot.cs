using UnityEngine;

public class Slot : MonoBehaviour
{
    public int x, y;
    public ItemUI currentItem;

    public bool IsEmpty => currentItem == null;

    public void Setup(int gridX, int gridY)
    {
        x = gridX;
        y = gridY;
    }
}
