// PlayerPickupManager.cs
using UnityEngine;

public class PlayerPickupManager : MonoBehaviour
{
    [Header("Touche")]
    public KeyCode pickupKey = KeyCode.F;
    public KeyCode dropKey = KeyCode.P;
    public float pickupRange = 2f;

    private InventoryManager inventoryManager;
    private Transform playerTransform;

    private void Awake()
    {
        inventoryManager = FindFirstObjectByType<InventoryManager>();
        playerTransform = this.transform;
    }

    private void Update()
    {
        if (Input.GetKeyDown(pickupKey))
            TryPickup();

        if (Input.GetKeyDown(dropKey))
            TryDrop();
    }

    private void TryPickup()
    {
        if (inventoryManager == null) return;

        // Cherche tous les WorldItem actifs (FindObjectsByType rapide si pas besoin de tri)
        WorldItem[] worldItems = FindObjectsByType<WorldItem>(FindObjectsSortMode.None);
        WorldItem nearest = null;
        float best = float.MaxValue;

        foreach (var wi in worldItems)
        {
            if (!wi.gameObject.activeInHierarchy) continue;
            float d = Vector3.Distance(playerTransform.position, wi.transform.position);
            if (d <= pickupRange && d < best)
            {
                best = d;
                nearest = wi;
            }
        }

        if (nearest != null)
        {
            bool added = inventoryManager.AddItem(nearest.itemData);
            if (added)
            {
                // Desactive l'objet dans le monde (ou Destruct)
                nearest.OnPickedUp();
            }
            else
            {
                Debug.Log("[Pickup] Impossible d'ajouter item (inventaire plein ?) : " + nearest.itemData.itemName);
            }
        }
    }

    private void TryDrop()
    {
        if (inventoryManager == null) return;

        ItemData removed = inventoryManager.RemoveLastItem();
        if (removed == null) return;

        // Spawn devant le joueur
        if (removed.worldPrefab != null)
        {
            Vector3 spawnPos = playerTransform.position + playerTransform.forward * 1.5f + Vector3.up * 0.3f;
            Instantiate(removed.worldPrefab, spawnPos, Quaternion.identity);
        }
    }
}
