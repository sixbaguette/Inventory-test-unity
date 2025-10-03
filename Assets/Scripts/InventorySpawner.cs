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
            GameObject itemObj = Instantiate(itemPrefab, canvas.transform); // Spawn sous le Canvas
            ItemUI itemUI = itemObj.GetComponent<ItemUI>();
            itemUI.Setup(item);

            bool placed = false;

            for (int y = 0; y < inventoryManager.height && !placed; y++)
            {
                for (int x = 0; x < inventoryManager.width && !placed; x++)
                {
                    if (inventoryManager.CanPlaceItem(x, y, item))
                    {
                        inventoryManager.PlaceItem(itemUI, x, y);

                        // Mettre l'item comme enfant du slot et centrer
                        itemObj.transform.SetParent(inventoryManager.slots[x, y].transform, false);
                        RectTransform rt = itemObj.GetComponent<RectTransform>();
                        rt.anchoredPosition = Vector2.zero;

                        placed = true;
                        break;
                    }
                }
            }

            if (!placed)
            {
                Debug.LogWarning($"[InventorySpawner] Impossible de placer {item.itemName}");
                Destroy(itemObj);
            }
        }
    }
}
