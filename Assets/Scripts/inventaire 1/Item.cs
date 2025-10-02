using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    // Pour plus tard (multi-slot modulable)
    public int width = 1;
    public int height = 1;
}
