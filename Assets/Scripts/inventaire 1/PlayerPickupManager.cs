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

        // Raycast droit devant le joueur
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            WorldItem wi = hit.collider.GetComponentInParent<WorldItem>();
            if (wi != null && wi.itemData != null)
            {
                // Utilise le vrai stackCount du WorldItem touché
                int amount = wi.itemData.isStackable ? Mathf.Max(1, wi.stackCount) : 1;

                // === Arme ou objet classique ===
                if (wi.itemData.isGun)
                {
                    // Ajoute normalement
                    bool added = InventoryManager.Instance.AddOrStackItem(wi.itemData, amount);

                    if (added)
                    {
                        // Récupère l’ItemUI nouvellement créé
                        ItemUI newUI = InventoryManager.Instance.GetLastItem();
                        if (newUI != null)
                        {
                            newUI.currentAmmo = wi.currentAmmo; // conserve le chargeur
                            Debug.Log($"[Pickup] Arme '{wi.itemData.itemName}' ramassée avec {wi.currentAmmo} balles");
                        }

                        wi.OnPickedUp();
                    }
                    else
                    {
                        Debug.Log("[Pickup] Inventaire plein ou ajout impossible : " + wi.itemData.itemName);
                    }
                }
                else
                {
                    // comportement normal (stackables, objets simples)
                    bool added = InventoryManager.Instance.AddOrStackItem(wi.itemData, amount);
                    Debug.Log($"[Pickup] Ajout de {amount}x {wi.itemData.itemName} → {(added ? "OK" : "ÉCHEC")}");

                    if (added)
                        wi.OnPickedUp();
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
                // On drop 1 seul item du stack
                GameObject go = Instantiate(data.worldPrefab, dropPos, Quaternion.identity);
                WorldItem wi = go.GetComponent<WorldItem>();
                if (wi != null)
                {
                    wi.itemData = data;
                    wi.SetStackCount(1); // très important : stackCount = 1
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

        GameObject droppedObj = Instantiate(data.worldPrefab, dropPos, Quaternion.identity);
        WorldItem droppedWorldItem = droppedObj.GetComponent<WorldItem>();
        if (droppedWorldItem != null)
        {
            droppedWorldItem.itemData = data;

            if (data.isGun)
            {
                droppedWorldItem.currentAmmo = itemToDrop.currentAmmo; // conserve les balles restantes
                Debug.Log($"[Drop] Arme '{data.itemName}' dropée avec {droppedWorldItem.currentAmmo} balles");
            }
        }
        inv.RemoveItem(itemToDrop);

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
                // on drop UN seul exemplaire
                GameObject go = Instantiate(data.worldPrefab, dropPos, Quaternion.identity);
                WorldItem wi = go.GetComponent<WorldItem>();
                if (wi != null)
                {
                    wi.itemData = data;
                    wi.SetStackCount(1); // CRUCIAL : l’objet au sol vaut 1
                }

                itemToDrop.currentStack--;
                itemToDrop.UpdateStackText();

                Debug.Log($"[Drop] 1x {data.itemName} (reste {itemToDrop.currentStack})");
            }
            else
            {
                // dernier exemplaire du stack → drop 1 et retire l’UI
                GameObject go = Instantiate(data.worldPrefab, dropPos, Quaternion.identity);
                WorldItem wi = go.GetComponent<WorldItem>();
                if (wi != null)
                {
                    wi.itemData = data;
                    wi.SetStackCount(1); // ici aussi : 1, pas la valeur du prefab
                }

                inv.RemoveItem(itemToDrop);
                Debug.Log($"[Drop] Dernier exemplaire de {data.itemName}");
            }

            return;
        }

        GameObject droppedObj = Instantiate(data.worldPrefab, dropPos, Quaternion.identity);
        WorldItem droppedWorldItem = droppedObj.GetComponent<WorldItem>();
        if (droppedWorldItem != null)
        {
            droppedWorldItem.itemData = data;

            if (data.isGun)
            {
                droppedWorldItem.currentAmmo = itemToDrop.currentAmmo; // on copie le chargeur actuel
                Debug.Log($"[DropSpecific] Arme '{data.itemName}' dropée avec {droppedWorldItem.currentAmmo} balles");
            }
        }

        // === Retrait de l'item de l'inventaire approprié ===
        if (InventoryManager.Instance != null &&
            itemToDrop.transform.IsChildOf(InventoryManager.Instance.itemsLayer))
        {
            // L'item est bien dans l'inventaire du joueur
            InventoryManager.Instance.RemoveItem(itemToDrop);
        }
        else
        {
            // Sinon, c’est un item d’un container
            var containerInv = itemToDrop.GetComponentInParent<ContainerInventoryManager>();
            if (containerInv != null)
            {
                containerInv.RemoveItem(itemToDrop);

                // Persistance immédiate
                var container = containerInv.GetComponentInParent<Container>();
                if (container != null)
                    container.SaveFrom(containerInv);
            }
            else
            {
                // Sécurité si l’objet n’est dans aucun inventaire
                GameObject.Destroy(itemToDrop.gameObject);
                Debug.LogWarning($"[DropSpecificItem] Item {itemToDrop.itemData?.itemName} supprimé manuellement (hors inventaire).");
            }
        }
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
            // A : un seul objet 3D avec stackCount interne
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
            // B : plusieurs objets 3D distincts
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

        // === Retrait de l'item de l'inventaire approprié ===
        if (InventoryManager.Instance != null &&
            itemToDrop.transform.IsChildOf(InventoryManager.Instance.itemsLayer))
        {
            // L'item vient de l'inventaire joueur
            InventoryManager.Instance.RemoveItem(itemToDrop);
        }
        else
        {
            // Item venant d’un container
            var containerInv = itemToDrop.GetComponentInParent<ContainerInventoryManager>();
            if (containerInv != null)
            {
                containerInv.RemoveItem(itemToDrop);

                // Persistance immédiate pour le coffre
                var container = containerInv.GetComponentInParent<Container>();
                if (container != null)
                    container.SaveFrom(containerInv);
            }
            else
            {
                // Sécurité : détruit l’objet si on ne sait pas d’où il vient
                GameObject.Destroy(itemToDrop.gameObject);
                Debug.LogWarning($"[DropEntireStack] Item {itemToDrop.itemData?.itemName} supprimé manuellement (hors inventaire).");
            }
        }

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
