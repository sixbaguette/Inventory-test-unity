// PlayerPickupManager.cs
using UnityEngine;

public class PlayerPickupManager : MonoBehaviour
{
    [Header("Touche")]
    public KeyCode pickupKey = KeyCode.F;
    public KeyCode dropKey = KeyCode.P;
    public float pickupRange = 2f;

    private bool isDropping = false;
    private InventoryManager inventoryManager;
    private Transform playerTransform;
    [SerializeField] private Transform dropPoint;


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
        if (isDropping) return;
        isDropping = true;

        var inv = InventoryManager.Instance;
        if (inv == null)
        {
            isDropping = false;
            return;
        }

        ItemUI lastItem = inv.GetLastItem();
        if (lastItem == null || lastItem.itemData == null)
        {
            isDropping = false;
            return;
        }

        Vector3 dropPos = dropPoint != null
            ? dropPoint.position
            : transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;

        // === CAS 1 : stackable ===
        if (lastItem.itemData.isStackable)
        {
            if (lastItem.currentStack > 1)
            {
                // drop UN seul item du stack
                Instantiate(lastItem.itemData.worldPrefab, dropPos, Quaternion.identity);

                lastItem.currentStack--;
                lastItem.UpdateStackText();

                Debug.Log($"Drop 1 {lastItem.itemData.itemName}. Reste : {lastItem.currentStack}");
            }
            else
            {
                // dernier item du stack → drop et retire de l’inventaire
                Instantiate(lastItem.itemData.worldPrefab, dropPos, Quaternion.identity);
                inv.RemoveItem(lastItem);
                Debug.Log($"Drop complet de {lastItem.itemData.itemName}");
            }

            isDropping = false;
            return;
        }

        // === CAS 2 : non stackable ===
        Instantiate(lastItem.itemData.worldPrefab, dropPos, Quaternion.identity);
        inv.RemoveItem(lastItem);

        Debug.Log($"Drop item unique {lastItem.itemData.itemName}");
        isDropping = false;
    }
}
