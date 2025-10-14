using UnityEngine;
using System.Collections.Generic;

public class PlayerEquipHandler : MonoBehaviour
{
    [Header("Références")]
    public Transform handSocket;
    private GameObject currentEquippedObject;

    private List<Rigidbody> disabledRigidbodies = new List<Rigidbody>();
    private List<Collider> disabledColliders = new List<Collider>();

    public void EquipItem(ItemData itemData)
    {
        UnequipAll();

        if (itemData == null || itemData.worldPrefab == null)
            return;

        // Instancie le prefab
        currentEquippedObject = Instantiate(itemData.worldPrefab, handSocket);
        currentEquippedObject.transform.localPosition = Vector3.zero;
        currentEquippedObject.transform.localRotation = Quaternion.identity;

        // 🔹 Désactive physique et collisions
        DisablePhysics(currentEquippedObject);

        Debug.Log($"[EquipHandler] Équipé {itemData.itemName}");
    }

    public void UnequipAll()
    {
        if (currentEquippedObject != null)
        {
            Debug.Log("[EquipHandler] Destruction de " + currentEquippedObject.name);
            Destroy(currentEquippedObject);
            currentEquippedObject = null;
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
}
