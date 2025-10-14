using UnityEngine;

public enum EquipSlotType
{
    None,
    Primary,
    Secondary,
    Equipement,
    Armor,
    Helmet,
    Legging,
    Backpack
}

public enum WorldStackMode
{
    SingleObjectWithCount,   // A : un seul objet 3D avec une valeur interne
    MultipleObjects          // B : plusieurs objets 3D distincts
}

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
    public EquipSlotType equipSlotType = EquipSlotType.None;

    [Header("Monde (3D)")]
    public GameObject worldPrefab; // <-- le prefab 3D du monde

    [Header("Stacking")]
    public bool isStackable = false;
    [Range(1, 999)]
    public int maxStack = 1;
    public WorldStackMode worldStackMode = WorldStackMode.MultipleObjects;
}
