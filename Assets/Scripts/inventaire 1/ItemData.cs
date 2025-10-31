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

    [Header("Heal")]
    public bool isHealingItem;        // ✅ active le mode soin
    public float healAmount = 25f;    // combien de HP sont rendus
    public float useTime = 3f;        // durée d’utilisation en secondes

    [Header("Armor Settings")]
    public bool isArmor;
    public float armorValue = 0f;  // puissance de réduction
    public EquipSlotType armorSlotType; // où se met l’armure : Helmet, Armor, Legging, etc.

    [Header("Melee Settings")]
    public bool isMeleeWeapon = false;
    public float meleeDamage = 25f;
    public float attackRange = 2f;
    public float attackSpeed = 1f; // coups par seconde

    public bool IsSameType(ItemData other)
    {
        if (other == null)
        {
            Debug.Log("  ⚠️ other == null");
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            Debug.Log($"  ✅ Référence identique pour {itemName}");
            return true;
        }

        if (!string.IsNullOrEmpty(prefabName) && !string.IsNullOrEmpty(other.prefabName))
        {
            bool same = prefabName.Trim().ToLower() == other.prefabName.Trim().ToLower();
            Debug.Log($"  🧩 Compare prefabName : {prefabName} vs {other.prefabName} → {(same ? "OK" : "NO")}");
            if (same) return true;
        }

        if (isAmmo && other.isAmmo)
        {
            bool sameType = ammoType == other.ammoType;
            Debug.Log($"  🔫 Compare ammoType : {ammoType} vs {other.ammoType} → {(sameType ? "OK" : "NO")}");
            if (sameType) return true;
        }

        if (!string.IsNullOrEmpty(itemName) && !string.IsNullOrEmpty(other.itemName))
        {
            bool sameName = string.Equals(itemName, other.itemName, System.StringComparison.OrdinalIgnoreCase);
            Debug.Log($"  🏷️ Compare itemName : {itemName} vs {other.itemName} → {(sameName ? "OK" : "NO")}");
            if (sameName) return true;
        }

        Debug.Log("  ❌ Aucun critère n’a matché !");
        return false;
    }
}
