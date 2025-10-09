// Item.cs (ScriptableObject)
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName;
    public string description;
    public Sprite icon;
    public int width = 1;
    public int height = 1;
    public bool isEquipable = false;
    public string equipmentType;

    [Header("World")]
    public GameObject worldPrefab; // <-- assigne ici le prefab 3D (ex: cube)
}
