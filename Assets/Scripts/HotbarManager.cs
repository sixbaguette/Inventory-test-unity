using UnityEngine;
using System.Collections.Generic;

public class HotbarManager : MonoBehaviour
{
    [System.Serializable]
    public class HotbarSlot
    {
        public int keyNumber;
        public EquipementSlot linkedSlot;
    }

    [Header("Configuration")]
    public List<HotbarSlot> hotbarSlots = new List<HotbarSlot>();
    public KeyCode[] keyBindings = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4 };

    [HideInInspector] public PlayerEquipHandler playerEquipHandler;

    private int currentActiveIndex = -1;

    private void Start()
    {
        currentActiveIndex = -1;
        if (playerEquipHandler == null)
            playerEquipHandler = FindFirstObjectByType<PlayerEquipHandler>();
    }

    public void ToggleHotbarSlotByIndex(int index)
    {
        // Sécurité
        if (index < 0 || index >= hotbarSlots.Count)
            return;

        // Si on rappuie sur le même slot -> déséquipe
        if (index == currentActiveIndex)
        {
            Debug.Log("[Hotbar] Déséquipement du slot " + (index + 1));
            UnequipCurrent();
            currentActiveIndex = -1;
            return;
        }

        // Sinon on équipe le nouveau
        UseHotbarSlotByIndex(index);
        currentActiveIndex = index;
    }

    public void UseHotbarSlotByIndex(int index)
    {
        if (index < 0 || index >= hotbarSlots.Count) return;

        var slot = hotbarSlots[index].linkedSlot;
        if (slot == null || slot.CurrentItem == null) return;

        ItemData item = slot.CurrentItem.itemData;

        if (playerEquipHandler != null)
        {
            if (item != null && item.worldPrefab != null)
            {
                playerEquipHandler.EquipItem(item);
            }
            else
            {
                playerEquipHandler.UnequipAll();
            }
        }

        Debug.Log("[Hotbar] Equipé slot " + (index + 1) + " -> " + item.itemName);
    }

    public void UnequipCurrent()
    {
        if (playerEquipHandler != null)
        {
            playerEquipHandler.UnequipAll();
            Debug.Log("[Hotbar] UnequipAll() exécuté");
        }
    }
}
