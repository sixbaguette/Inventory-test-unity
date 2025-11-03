using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Container : MonoBehaviour
{
    [System.Serializable]
    public class ContainerItemEntry
    {
        public ItemData itemData;
        [Min(1)] public int stackCount = 1;
    }

    [Header("Paramètres du conteneur")]
    public string containerName = "Coffre";
    public int width = 6;
    public int height = 5;

    [Header("Items de départ configurables")]
    public List<ContainerItemEntry> startingItems = new List<ContainerItemEntry>();

    // ✅ Inventaire interne persistant
    [SerializeField] private List<StoredItem> storedItems = new List<StoredItem>();

    private bool isOpen = false;

    private void Start()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    public void OpenContainer()
    {
        if (isOpen && ContainerUIController.Instance != null && ContainerUIController.Instance.IsContainerOpen)
        {
            Debug.Log("[Container] Déjà ouvert, on ignore.");
            return;
        }

        Debug.Log($"[Container] Ouverture du conteneur : {containerName}");
        isOpen = true;

        if (ContainerUIController.Instance != null)
            ContainerUIController.Instance.OpenContainer(this);
    }

    public void CloseContainer()
    {
        if (!isOpen) return;

        Debug.Log($"[Container] Fermeture du conteneur : {containerName}");
        isOpen = false;

        if (ContainerUIController.Instance != null)
            ContainerUIController.Instance.CloseContainer();
    }

    /// <summary>
    /// Chargé quand le conteneur est ouvert → on instancie ses items sauvegardés ou initiaux.
    /// </summary>
    public void LoadInto(ContainerInventoryManager inv)
    {
        if (inv == null) return;

        inv.width = width;
        inv.height = height;
        inv.InitializeGrid();

        // 1) Priorité: si on a déjà un contenu persistant -> on le réhydrate et ON S'ARRÊTE LÀ
        if (storedItems.Count > 0)
        {
            foreach (var s in storedItems)
            {
                if (s == null || s.data == null) continue;

                GameObject go = GameObject.Instantiate(inv.itemUIPrefab, inv.itemsLayer);
                var ui = go.GetComponent<ItemUI>();
                ui.Setup(s.data);
                ui.currentStack = Mathf.Max(1, s.stack);
                ui.UpdateStackText();

                ui.currentAmmo = s.currentAmmo;

                // ⚠️ Si le placement échoue, on détruit pour éviter le "ghost" en haut-gauche
                if (!inv.PlaceItem(ui, s.x, s.y))
                {
                    Debug.LogWarning($"[Container] Échec de placement lors de la réhydratation → destroy {ui.itemData?.itemName}");
                    GameObject.Destroy(go);
                }
                else
                {
                    inv.AddToInventoryList(ui);
                }
            }
            return; // ⛔ très important : ne PAS générer/dupliquer après ça
        }

        // 2) Sinon: items de départ manuels (une seule fois)
        if (startingItems != null && startingItems.Count > 0)
        {
            foreach (var entry in startingItems)
            {
                if (entry == null || entry.itemData == null) continue;

                inv.AddItem(entry.itemData);
                var lastItem = inv.items.Count > 0 ? inv.items[^1] : null;
                if (lastItem != null)
                {
                    if (entry.itemData.isStackable)
                        lastItem.SetStack(entry.stackCount);

                    lastItem.currentAmmo = entry.itemData.isGun ? entry.itemData.ammoCapacity : -1;

                    storedItems.Add(new StoredItem
                    {
                        data = entry.itemData,
                        x = lastItem.currentSlot != null ? lastItem.currentSlot.x : 0,
                        y = lastItem.currentSlot != null ? lastItem.currentSlot.y : 0,
                        width = entry.itemData.width,
                        height = entry.itemData.height,
                        stack = lastItem.currentStack,
                        currentAmmo = lastItem.currentAmmo
                    });
                }
            }

            startingItems.Clear();
            return; // ⛔ ne pas poursuivre (sinon tu générerais par-dessus)
        }

        // 3) Enfin: loot table (si configurée) → génère, puis snapshot dans storedItems, et on ne réhydrate PAS (car déjà visible)
        var loot = GetComponent<ContainerLootTable>();
        if (loot != null)
        {
            loot.containerInv = inv;
            loot.EnsureGenerated(); // génère visuellement dans la grille si besoin

            // snapshot dans storedItems (uniquement si c’est encore vide)
            if (storedItems.Count == 0)
            {
                foreach (var itemUI in inv.items)
                {
                    if (itemUI == null || itemUI.itemData == null || itemUI.currentSlot == null)
                        continue;

                    storedItems.Add(new StoredItem
                    {
                        data = itemUI.itemData,
                        x = itemUI.currentSlot.x,
                        y = itemUI.currentSlot.y,
                        width = itemUI.itemData.width,
                        height = itemUI.itemData.height,
                        stack = itemUI.currentStack,
                        currentAmmo = itemUI.itemData.isGun ? itemUI.itemData.ammoCapacity : -1
                    });
                }

                Debug.Log($"[Container] Loot généré depuis {loot.profile?.name} → {storedItems.Count} items enregistrés.");
            }

            return;
        }

        // 4) Aucun contenu configuré → rien à faire
    }

    /// <summary>
    /// Sauvegarde le contenu actuel de l'UI du coffre quand on ferme.
    /// </summary>
    public void SaveFrom(ContainerInventoryManager inv)
    {
        if (inv == null) return;

        storedItems.Clear();

        if (inv.itemsLayer == null) return;

        for (int i = 0; i < inv.itemsLayer.childCount; i++)
        {
            var child = inv.itemsLayer.GetChild(i);
            var ui = child.GetComponent<ItemUI>();
            if (ui == null || ui.itemData == null || ui.currentSlot == null) continue;

            storedItems.Add(new StoredItem
            {
                data = ui.itemData,
                x = ui.currentSlot.x,
                y = ui.currentSlot.y,
                width = ui.itemData.width,
                height = ui.itemData.height,
                stack = ui.currentStack,

                // 🆕 snapshot runtime
                currentAmmo = ui.currentAmmo
            });
        }

        Debug.Log($"[Container] Contenu sauvegardé ({storedItems.Count} items).");
    }

    // ✅ Appelé automatiquement par le contrôleur UI quand il ferme
    public void OnUIClosed()
    {
        isOpen = false;
    }
}

[Serializable]
public class StoredItem
{
    public ItemData data;
    public int x;
    public int y;
    public int width;
    public int height;
    public int stack = 1;

    // 🆕 ÉTATS RUNTIME
    public int currentAmmo = -1;   // -1 = inconnu / non applicable
    // (plus tard tu pourras ajouter durability, modifiers, etc.)
}
