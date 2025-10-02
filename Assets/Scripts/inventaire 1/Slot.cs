using UnityEngine;

public class Slot : MonoBehaviour
{
    public int x;
    public int y;

    public void Setup(int gridX, int gridY)
    {
        x = gridX;
        y = gridY;
    }
}
