﻿using UnityEngine;
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
    }

    private void TryPickup()
    {
        if (inventoryManager == null) return;

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[Pickup] Aucune caméra trouvée !");
            return;
        }

        // 🔫 Raycast droit devant le joueur
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            WorldItem wi = hit.collider.GetComponentInParent<WorldItem>();
            if (wi != null && wi.itemData != null)
            {
                // ✅ Utilise le vrai stackCount du WorldItem touché
                int amount = wi.itemData.isStackable ? Mathf.Max(1, wi.stackCount) : 1;

                bool added = InventoryManager.Instance.AddItem(wi.itemData, amount);
                Debug.Log($"[Pickup] Ajout de {amount}x {wi.itemData.itemName} → {(added ? "OK" : "ÉCHEC")}");

                if (added)
                {
                    wi.OnPickedUp(); // détruit l’objet ramassé
                }
                else
                {
                    Debug.Log("[Pickup] Inventaire plein ou ajout impossible : " + wi.itemData.itemName);
                }
            }
            else
            {
                Debug.Log("[Pickup] Aucun WorldItem trouvé sur l’objet visé.");
            }
        }
        else
        {
            Debug.Log("[Pickup] Aucun objet ramassable devant toi.");
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

        ItemUI targetItem = GetItemUnderMouse();
        ItemUI itemToDrop = targetItem ?? inv.GetLastItem();

        if (itemToDrop == null || itemToDrop.itemData == null)
        {
            isDropping = false;
            return;
        }

        Vector3 dropPos = dropPoint != null
            ? dropPoint.position
            : transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;

        ItemData data = itemToDrop.itemData;

        // === CAS 1 : stackable ===
        if (data.isStackable)
        {
            if (itemToDrop.currentStack > 1)
            {
                // 🟡 On drop 1 seul item du stack
                GameObject go = Instantiate(data.worldPrefab, dropPos, Quaternion.identity);
                WorldItem wi = go.GetComponent<WorldItem>();
                if (wi != null)
                {
                    wi.itemData = data;
                    wi.SetStackCount(1); // ✅ très important : stackCount = 1
                }

                itemToDrop.currentStack--;
                itemToDrop.UpdateStackText();

                Debug.Log($"[Drop] 1x {data.itemName} (reste {itemToDrop.currentStack})");
            }
            else
            {
                // Drop complet du stack restant
                GameObject go = Instantiate(data.worldPrefab, dropPos, Quaternion.identity);
                WorldItem wi = go.GetComponent<WorldItem>();
                if (wi != null)
                {
                    wi.itemData = data;
                    wi.SetStackCount(itemToDrop.currentStack); // il ne reste qu’un
                }

                inv.RemoveItem(itemToDrop);
                Debug.Log($"[Drop] Stack complet de {data.itemName}");
            }

            isDropping = false;
            return;
        }

        // === CAS 2 : non stackable ===
        Instantiate(data.worldPrefab, dropPos, Quaternion.identity);
        inv.RemoveItem(itemToDrop);
        Debug.Log($"[Drop] Item unique {data.itemName}");

        isDropping = false;
    }

    public void DropSpecificItem(ItemUI itemToDrop)
    {
        if (itemToDrop == null || itemToDrop.itemData == null) return;

        var inv = InventoryManager.Instance;
        if (inv == null) return;

        Vector3 dropPos = dropPoint != null
            ? dropPoint.position
            : transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;

        ItemData data = itemToDrop.itemData;

        // === Item stackable ===
        if (data.isStackable)
        {
            if (itemToDrop.currentStack > 1)
            {
                // 🔹 on drop UN seul exemplaire
                GameObject go = Instantiate(data.worldPrefab, dropPos, Quaternion.identity);
                WorldItem wi = go.GetComponent<WorldItem>();
                if (wi != null)
                {
                    wi.itemData = data;
                    wi.SetStackCount(1); // ⬅️ CRUCIAL : l’objet au sol vaut 1
                }

                itemToDrop.currentStack--;
                itemToDrop.UpdateStackText();

                Debug.Log($"[Drop] 1x {data.itemName} (reste {itemToDrop.currentStack})");
            }
            else
            {
                // 🔹 dernier exemplaire du stack → drop 1 et retire l’UI
                GameObject go = Instantiate(data.worldPrefab, dropPos, Quaternion.identity);
                WorldItem wi = go.GetComponent<WorldItem>();
                if (wi != null)
                {
                    wi.itemData = data;
                    wi.SetStackCount(1); // ⬅️ ici aussi : 1, pas la valeur du prefab
                }

                inv.RemoveItem(itemToDrop);
                Debug.Log($"[Drop] Dernier exemplaire de {data.itemName}");
            }

            return;
        }

        // === Item non stackable ===
        Instantiate(data.worldPrefab, dropPos, Quaternion.identity);
        inv.RemoveItem(itemToDrop);
        Debug.Log($"[Drop] Item unique {data.itemName}");
    }

    public void DropEntireStack(ItemUI itemToDrop)
    {
        if (itemToDrop == null || itemToDrop.itemData == null) return;

        var inv = InventoryManager.Instance;
        if (inv == null) return;

        Vector3 dropPos = dropPoint != null
            ? dropPoint.position
            : transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;

        ItemData data = itemToDrop.itemData;

        if (data.worldStackMode == WorldStackMode.SingleObjectWithCount)
        {
            // ✅ A : un seul objet 3D avec stackCount interne
            GameObject go = Instantiate(data.worldPrefab, dropPos, Quaternion.identity);
            WorldItem wi = go.GetComponent<WorldItem>();
            if (wi != null)
            {
                wi.itemData = data;
                wi.SetStackCount(itemToDrop.currentStack);
            }
        }
        else
        {
            // ✅ B : plusieurs objets 3D distincts
            for (int i = 0; i < itemToDrop.currentStack; i++)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-0.05f, 0.05f),
                    0f,
                    Random.Range(-0.05f, 0.05f)
                );
                Instantiate(data.worldPrefab, dropPos + offset, Quaternion.identity);
            }
        }

        inv.RemoveItem(itemToDrop);
        Debug.Log($"[DropStack] {itemToDrop.currentStack}x {data.itemName} droppés !");
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
