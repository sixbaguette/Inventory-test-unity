using UnityEngine;
using System.Collections;

public class InventorySpawner : MonoBehaviour
{
    public GameObject itemPrefab;
    private InventoryManager inventoryManager;
    private Canvas canvas;

    private IEnumerator Start()
    {
        yield return null; // Attend la fin d’Awake de tous les objets

        inventoryManager = FindFirstObjectByType<InventoryManager>();
        canvas = FindFirstObjectByType<Canvas>();

        if (inventoryManager == null)
        {
            Debug.LogError("[InventorySpawner] InventoryManager introuvable !");
            yield break;
        }

        if (canvas == null)
        {
            Debug.LogError("[InventorySpawner] Canvas introuvable !");
            yield break;
        }

        StartCoroutine(SpawnWhenReady());
    }

    private IEnumerator SpawnWhenReady()
    {
        yield return new WaitUntil(() => inventoryManager != null);

        Item[] items = Resources.LoadAll<Item>("Items");

        if (items.Length == 0)
        {
            Debug.LogWarning("[InventorySpawner] Aucun Item trouvé dans Resources/Items");
            yield break;
        }

        Debug.Log($"[InventorySpawner] {items.Length} items trouvés");

        foreach (var item in items)
        {
            bool placed = false;

            for (int y = 0; y <= inventoryManager.height - item.height && !placed; y++)
            {
                for (int x = 0; x <= inventoryManager.width - item.width && !placed; x++)
                {
                    if (inventoryManager.CanPlaceItem(x, y, item))
                    {
                        GameObject itemObj = Instantiate(itemPrefab); // <- ici on utilise directement itemPrefab
                        ItemUI itemUI = itemObj.GetComponent<ItemUI>();
                        itemUI.Setup(item);

                        inventoryManager.PlaceItem(itemUI, x, y);
                        placed = true;
                        break;
                    }
                }
            }

            if (!placed)
            {
                Debug.LogWarning($"[InventorySpawner] Impossible de placer {item.itemName}");
            }
        }
    }
}
