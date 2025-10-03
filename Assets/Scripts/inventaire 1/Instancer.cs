using UnityEngine;
using System.Collections;

public class Instancer : MonoBehaviour
{
    /*
    public GameObject itemPrefab;
    public InventoryManager inventoryManager;
    public Item itemData; // notre ScriptableObject

    void Start()
    {
        StartCoroutine(SpawnItemWhenReady());
    }

    IEnumerator SpawnItemWhenReady()
    {
        while (inventoryManager.firstSlot == null)
        {
            yield return null;
        }

        GameObject newItem = Instantiate(itemPrefab, inventoryManager.firstSlot);
        RectTransform rt = newItem.GetComponent<RectTransform>();
        if (rt != null)
            rt.anchoredPosition = Vector2.zero;

        ItemUI itemUI = newItem.GetComponent<ItemUI>();
        if (itemUI != null)
            itemUI.Setup(itemData);
    }
    */
}
