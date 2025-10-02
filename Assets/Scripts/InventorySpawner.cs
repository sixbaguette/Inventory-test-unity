using UnityEngine;

public class InventorySpawner : MonoBehaviour
{
    public GameObject itemPrefab;   // Prefab de l'item UI
    public Transform inventoryPanel; // Parent (GridPanel)
    public Item[] itemsToSpawn;     // Liste d'items ScriptableObject à instancier

    private void Start()
    {
        foreach (var itemData in itemsToSpawn)
        {
            SpawnItem(itemData);
        }
    }

    private void SpawnItem(Item itemData)
    {
        GameObject itemObj = Instantiate(itemPrefab, inventoryPanel);
        ItemUI itemUI = itemObj.GetComponent<ItemUI>();
        itemUI.Setup(itemData); // Configure automatiquement l'UI
    }
}
