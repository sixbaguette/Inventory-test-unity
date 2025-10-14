using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryFilterUI : MonoBehaviour
{
    public static InventoryFilterUI Instance;

    [Header("Références")]
    public TMP_Dropdown filterDropdown; // 👈 au lieu de Dropdown
    public CanvasGroup inventoryCanvasGroup;

    private EquipSlotType currentFilter = EquipSlotType.None;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (filterDropdown == null)
        {
            Debug.LogWarning("Aucun Dropdown de filtre assigné !");
            return;
        }

        // Remplir la liste des types automatiquement à partir de l’enum
        filterDropdown.ClearOptions();
        var options = new List<string>();
        foreach (var type in System.Enum.GetValues(typeof(EquipSlotType)))
        {
            options.Add(type.ToString());
        }
        filterDropdown.AddOptions(options);

        filterDropdown.onValueChanged.AddListener(OnFilterChanged);
    }

    private void OnFilterChanged(int index)
    {
        currentFilter = (EquipSlotType)index;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        // Récupère tous les ItemUI présents dans l’inventaire
        var allItems = FindObjectsByType<ItemUI>(FindObjectsSortMode.None);
        foreach (var item in allItems)
        {
            if (item.itemData == null) continue;

            bool shouldHighlight = currentFilter == EquipSlotType.None || item.itemData.equipSlotType == currentFilter;

            var img = item.icon;
            if (img != null)
            {
                Color c = img.color;
                c.a = shouldHighlight ? 1f : 0.3f; // 30 % de transparence si pas dans la catégorie
                img.color = c;
            }
        }
    }

    public void ResetFilter()
    {
        filterDropdown.value = 0;
        currentFilter = EquipSlotType.None;
        ApplyFilter();
    }
}
