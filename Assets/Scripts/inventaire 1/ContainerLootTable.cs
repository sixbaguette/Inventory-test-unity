using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ContainerLootTable : MonoBehaviour
{
    [Header("Profil de loot")]
    public LootProfile profile;

    [Header("Intégration")]
    [Tooltip("Référence vers le Container (script qui gère persistance). Optionnel mais conseillé.")]
    public Container container; // ton script existant

    [Tooltip("Inventaire UI de ce conteneur (où déposer les items générés).")]
    public ContainerInventoryManager containerInv;

    [Header("Génération")]
    [Tooltip("Générer au premier Open si le conteneur est vide.")]
    public bool generateOnOpenIfEmpty = true;

    [Tooltip("Forcer une régénération à chaque ouverture (pour debug).")]
    public bool regenerateEveryOpen = false;

    [Tooltip("Nombre max d’essais pour placer chaque item (évite les boucles si plein).")]
    public int placeAttemptsPerItem = 20;

    private bool generatedOnce = false;
    private System.Random rng;

    void Awake()
    {
        if (container == null) container = GetComponent<Container>();
        if (containerInv == null) containerInv = GetComponentInChildren<ContainerInventoryManager>();
    }

    /// <summary>
    /// À appeler quand on ouvre le conteneur (par ex. depuis Container.LoadInto ou ContainerUIController.OpenContainer)
    /// </summary>
    public void EnsureGenerated()
    {
        if (profile == null || containerInv == null) return;

        if (profile.fixedSeed != 0)
            rng = new System.Random(profile.fixedSeed + GetInstanceID());
        else
            rng = new System.Random();

        // si déjà des items visibles, ne rien faire (évite les doubles si mal appelé)
        if (!IsContainerEmpty() && !regenerateEveryOpen)
            return;

        if (regenerateEveryOpen)
        {
            GenerateNow(clearFirst: true);
            generatedOnce = true;
            return;
        }

        if (!generatedOnce)
        {
            if (generateOnOpenIfEmpty && IsContainerEmpty())
                GenerateNow(clearFirst: false);

            generatedOnce = true;
        }
    }


    public void GenerateNow(bool clearFirst)
    {
        if (containerInv == null) return;

        if (clearFirst) ClearContainerVisualOnly();

        int totalSpawned = 0;
        foreach (var cat in profile.categories)
        {
            if (cat == null || cat.entries == null || cat.entries.Count == 0) continue;

            int rolls = Mathf.Clamp(rng.Next(cat.minRolls, cat.maxRolls + 1), 0, 1000);
            for (int i = 0; i < rolls; i++)
            {
                if (profile.maxTotalItems >= 0 && totalSpawned >= profile.maxTotalItems)
                    break;

                var entry = PickWeighted(cat.entries);
                if (entry == null || entry.itemData == null) continue;

                int stack = 1;
                if (entry.itemData.isStackable)
                {
                    int min = Mathf.Max(1, entry.minStack);
                    int max = Mathf.Max(min, entry.maxStack);
                    stack = rng.Next(min, max + 1);
                }

                bool spawned = TrySpawnIntoContainer(entry.itemData, stack);
                if (spawned) totalSpawned++;
            }
        }

        // ⚠️ si tu as une persistance dans Container, sauvegarde maintenant
        if (container != null)
            container.SaveFrom(containerInv);
    }

    // =====================
    // Placement & util
    // =====================

    private bool TrySpawnIntoContainer(ItemData data, int stack)
    {
        // trouve une case dispo (avec rotation auto si besoin)
        Vector2Int? pos = containerInv.FindFreeSpaceFor(data);
        bool rotated = false;

        if (!pos.HasValue)
        {
            // tente rotation
            int w = data.width, h = data.height;
            data.width = h; data.height = w;
            pos = containerInv.FindFreeSpaceFor(data);
            rotated = pos.HasValue;

            if (!pos.HasValue)
            {
                // revert
                data.height = h; data.width = w;
                return false;
            }
        }

        // instancie l’ItemUI
        GameObject go = Instantiate(containerInv.itemUIPrefab, containerInv.itemsLayer);
        ItemUI ui = go.GetComponent<ItemUI>();
        ui.Setup(data);

        // place dans la grille
        bool ok = containerInv.PlaceItem(ui, pos.Value.x, pos.Value.y);
        if (!ok)
        {
            Destroy(go);
            return false;
        }

        // stack si applicable
        if (data.isStackable && stack > 1)
        {
            ui.currentStack = Mathf.Clamp(stack, 1, data.maxStack);
            ui.UpdateStackText();
        }

        // remet orientation si on avait pivoté le ScriptableObject (sécurité)
        if (rotated)
        {
            int w = data.width, h = data.height;
            data.width = h; data.height = w;
        }

        // tient la liste logique du conteneur à jour
        containerInv.AddToInventoryList(ui);
        return true;
    }

    private LootEntry PickWeighted(List<LootEntry> entries)
    {
        int total = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            int w = Mathf.Max(0, entries[i].weight);
            total += w;
        }
        if (total <= 0) return null;

        int roll = rng.Next(0, total);
        int cum = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            cum += Mathf.Max(0, entries[i].weight);
            if (roll < cum) return entries[i];
        }
        return entries[entries.Count - 1];
    }

    private bool IsContainerEmpty()
    {
        // visuel
        if (containerInv.itemsLayer != null && containerInv.itemsLayer.childCount > 0)
            return false;

        // logique
        if (containerInv.items != null && containerInv.items.Count > 0)
            return false;

        return true;
    }

    private void ClearContainerVisualOnly()
    {
        if (containerInv.itemsLayer != null)
        {
            for (int i = containerInv.itemsLayer.childCount - 1; i >= 0; i--)
                Destroy(containerInv.itemsLayer.GetChild(i).gameObject);
        }
        if (containerInv.slots != null)
        {
            foreach (var s in containerInv.slots) s?.ClearItem();
        }
        containerInv.items.Clear();
    }
}