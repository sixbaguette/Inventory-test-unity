using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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

        // 🔍 1️⃣ Détecter si la souris survole un ItemUI
        ItemUI targetItem = GetItemUnderMouse();

        // Sinon fallback sur le dernier
        ItemUI itemToDrop = targetItem ?? inv.GetLastItem();

        if (itemToDrop == null || itemToDrop.itemData == null)
        {
            isDropping = false;
            return;
        }

        // Position de drop devant le joueur
        Vector3 dropPos = dropPoint != null
            ? dropPoint.position
            : transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;

        // === CAS 1 : stackable ===
        if (itemToDrop.itemData.isStackable)
        {
            if (itemToDrop.currentStack > 1)
            {
                // Drop 1 seul item du stack
                Instantiate(itemToDrop.itemData.worldPrefab, dropPos, Quaternion.identity);

                itemToDrop.currentStack--;
                itemToDrop.UpdateStackText();

                Debug.Log($"[Drop] 1x {itemToDrop.itemData.itemName} (reste {itemToDrop.currentStack})");
            }
            else
            {
                // Drop complet
                Instantiate(itemToDrop.itemData.worldPrefab, dropPos, Quaternion.identity);
                inv.RemoveItem(itemToDrop);
                Debug.Log($"[Drop] Stack complet de {itemToDrop.itemData.itemName}");
            }

            isDropping = false;
            return;
        }

        // === CAS 2 : non stackable ===
        Instantiate(itemToDrop.itemData.worldPrefab, dropPos, Quaternion.identity);
        inv.RemoveItem(itemToDrop);
        Debug.Log($"[Drop] Item unique {itemToDrop.itemData.itemName}");

        isDropping = false;
    }

    /// <summary>
    /// Retourne l'ItemUI actuellement sous le curseur souris (ou null)
    /// </summary>
    private ItemUI GetItemUnderMouse()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var r in results)
        {
            ItemUI item = r.gameObject.GetComponentInParent<ItemUI>();
            if (item != null)
                return item;
        }

        return null;
    }
}
