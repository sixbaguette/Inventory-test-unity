using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public GameObject slotPrefab;
    public Transform gridPanel; // Panel avec GridLayoutGroup
    public int width = 6;
    public int height = 5;

    [HideInInspector] public Transform firstSlot;

    void Start()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject slotObj = Instantiate(slotPrefab, gridPanel);
                if (x == 0 && y == 0)
                {
                    firstSlot = slotObj.transform;
                }
            }
        }
    }
}
