using UnityEngine;
using UnityEngine.EventSystems;

public class SlotDrop : MonoBehaviour, IDropHandler
{
    public int x;
    public int y;

    public void Setup(int xCoord, int yCoord)
    {
        x = xCoord;
        y = yCoord;
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Le drag gère déjà le placement
    }
}
