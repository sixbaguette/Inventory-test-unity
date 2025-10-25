using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public ItemData data;
    public int quantity = 1;

    // ⚙️ État dynamique (ex : munitions actuelles)
    public int currentAmmo;

    public InventoryItem(ItemData data, int quantity = 1)
    {
        this.data = data;
        this.quantity = quantity;

        if (data.isGun)
            currentAmmo = data.ammoCapacity;
    }
}
