using UnityEngine;
using System.Collections.Generic;

public class PlayerEquipHandler : MonoBehaviour
{
    [Header("Références")]
    public Transform handSocket;
    private GameObject currentEquippedObject;
    private ItemData currentEquippedData;

    private List<Rigidbody> disabledRigidbodies = new List<Rigidbody>();
    private List<Collider> disabledColliders = new List<Collider>();

    public void EquipItem(ItemData itemData, ItemUI itemUI = null)
    {
        UnequipAll();

        if (itemData == null || itemData.worldPrefab == null)
            return;

        currentEquippedObject = Instantiate(itemData.worldPrefab, handSocket);
        currentEquippedObject.transform.localPosition = Vector3.zero;
        currentEquippedObject.transform.localRotation = Quaternion.identity;
        currentEquippedData = itemData;

        DisablePhysics(currentEquippedObject);

        GunSystem gun = currentEquippedObject.GetComponent<GunSystem>();
        if (itemData.isGun && gun != null)
        {
            gun.handSocket = handSocket;
            gun.EquipWeapon(itemData, itemUI); // 🆕 on passe le ItemUI ici
            gun.ApplyHandOffset();   // 🆕 ici
            gun.enabled = true;
        }

        Debug.Log($"[EquipHandler] Équipé {itemData.itemName}");
    }
   
    public void UnequipAll()
    {
        if (currentEquippedObject != null)
        {
            // 🆕 remet le handSocket à zéro avant destruction
            GunSystem gun = currentEquippedObject.GetComponent<GunSystem>();
            if (gun != null)
                gun.ResetHandOffset();

            Debug.Log("[EquipHandler] Destruction de " + currentEquippedObject.name);
            Destroy(currentEquippedObject);
            currentEquippedObject = null;
            currentEquippedData = null;
        }

        disabledRigidbodies.Clear();
        disabledColliders.Clear();
    }

    private void DisablePhysics(GameObject go)
    {
        // Récupère tous les rigidbodies du prefab
        var rigidbodies = go.GetComponentsInChildren<Rigidbody>(true);
        var colliders = go.GetComponentsInChildren<Collider>(true);

        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            disabledRigidbodies.Add(rb);
        }

        foreach (var col in colliders)
        {
            col.enabled = false;
            disabledColliders.Add(col);
        }
    }

    // (optionnel) pour plus tard : réactiver quand on drop
    public void ReenablePhysics()
    {
        foreach (var rb in disabledRigidbodies)
        {
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }

        foreach (var col in disabledColliders)
        {
            if (col != null)
                col.enabled = true;
        }
    }

    public GameObject GetEquippedObject()
    {
        return currentEquippedObject;
    }

    public void UnequipIfHolding(ItemData data)
    {
        if (data == null) return;
        if (currentEquippedObject == null) return;

        // Vérifie si c’est bien l’ItemData actuellement équipé
        if (currentEquippedData == data)
        {
            Debug.Log($"[PlayerEquipHandler] Désinstanciation forcée de {data.itemName}");
            UnequipAll();
            return;
        }

        // Si le champ n’est pas sync (vieux état), on teste aussi le contenu du prefab
        GunSystem gun = currentEquippedObject.GetComponent<GunSystem>();
        MeleeWeapon melee = currentEquippedObject.GetComponent<MeleeWeapon>();

        if ((gun != null && gun.weaponData == data) ||
            (melee != null && melee.currentWeapon == data))
        {
            Debug.Log($"[PlayerEquipHandler] Désinstanciation forcée (match interne) de {data.itemName}");
            UnequipAll();
        }
    }
}
