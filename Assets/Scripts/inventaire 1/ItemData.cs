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

public enum BulletType
{
    Cal9mm,
    Cal556,
    Cal762
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

    [Header("Gun Settings")]
    public bool isGun;
    public float damage;
    public float fireRate;
    public float reloadTime;
    public float bulletSpeed;
    public int ammoCapacity;
    public float aimSpeed;
    public BulletType bulletType;          // ✅ utilise ton enum BulletType

    [Header("Ammo Settings")]
    public bool isAmmo;            // Si cet item est une balle
    public BulletType ammoType;    // Type de balle qu’il représente

    [Header("Gun References")]
    public GameObject bulletPrefab;        // ✅ le prefab de ta balle
    public AudioClip fireSound;            // ✅ son du tir
    public AudioClip reloadSound;          // ✅ son du rechargement
    public AudioClip emptyClickSound;
    public ParticleSystem muzzleFlash;     // ✅ effet visuel de tir

    public bool IsSameType(ItemData other)
    {
        if (other == null) return false;
        if (ReferenceEquals(this, other)) return true;

        // 1) clé forte si tu utilises prefabName
        if (!string.IsNullOrEmpty(prefabName) && !string.IsNullOrEmpty(other.prefabName))
            return prefabName == other.prefabName;

        // 2) munitions : même type de balle => même pile
        if (isAmmo && other.isAmmo)
            return ammoType == other.ammoType;

        // 3) fallback sûr par nom exact (si tu gardes des noms stricts)
        if (!string.IsNullOrEmpty(itemName) && !string.IsNullOrEmpty(other.itemName))
            return string.Equals(itemName, other.itemName, System.StringComparison.Ordinal);

        return false;
    }
}
