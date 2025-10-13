using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("Infos générales")]
    public string itemName;
    public string prefabName; // nom du prefab dans Resources/WorldItems
    [TextArea] public string description;

    [Header("Inventaire (2D)")]
    public Sprite icon;
    public int width = 1;
    public int height = 1;
    public bool isEquipable = false;
    public string equipmentType;

    [Header("Monde (3D)")]
    public GameObject worldPrefab; // <-- le prefab 3D du monde

    [Header("Stacking")]
    public bool isStackable = false;
    [Range(1, 999)]
    public int maxStack = 1;
}
