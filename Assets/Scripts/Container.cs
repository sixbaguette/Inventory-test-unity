using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Container : MonoBehaviour
{
    [Header("Paramètres du conteneur")]
    public string containerName = "Coffre";
    public int width = 6;
    public int height = 5;

    [Header("Items de départ")]
    public ItemData[] startingItems;

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

        // Si c'est la première ouverture → placer les items de départ
        if (storedItems.Count == 0 && startingItems != null && startingItems.Length > 0)
        {
            foreach (var item in startingItems)
            {
                if (item == null) continue;

                // 🔍 Trouve une position libre pour cet item
                Vector2Int? freePos = inv.FindFreeSpaceFor(item);
                if (freePos.HasValue)
                {
                    inv.AddItemAt(item, freePos.Value.x, freePos.Value.y);
                }
                else
                {
                    Debug.LogWarning($"[Container] Plus de place pour {item.name} dans {containerName}");
                }
            }
        }
        else
        {
            // Sinon → on recharge les items sauvegardés
            foreach (var s in storedItems)
            {
                if (s == null || s.data == null) continue;

                GameObject go = GameObject.Instantiate(inv.itemUIPrefab, inv.itemsLayer);
                var ui = go.GetComponent<ItemUI>();
                ui.Setup(s.data);
                ui.currentStack = Mathf.Max(1, s.stack);
                ui.UpdateStackText();

                ui.itemData.width = s.width;
                ui.itemData.height = s.height;

                inv.PlaceItem(ui, s.x, s.y);
            }
        }
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
                stack = ui.currentStack
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
}
