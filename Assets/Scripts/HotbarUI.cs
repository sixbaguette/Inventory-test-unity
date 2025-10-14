using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class HotbarUI : MonoBehaviour
{
    [System.Serializable]
    public class HotbarSlotUI
    {
        public Image background;
        public Image icon;
        public TextMeshProUGUI numberText;
    }

    [Header("Références")]
    public List<HotbarSlotUI> slots = new List<HotbarSlotUI>();
    public Image selectionHighlight;
    public GameObject inventoryUI; // 👈 référence vers ton InventoryUI
    public float fadeSpeed = 10f;  // pour un petit effet smooth

    private int currentIndex = -1;
    private HotbarManager hotbarManager;
    private CanvasGroup canvasGroup;
    private float targetAlpha = 1f;

    void Awake()
    {
        // ajoute un CanvasGroup s’il n’existe pas
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void Start()
    {
        hotbarManager = FindFirstObjectByType<HotbarManager>();
        RefreshIcons();
        DeselectSlot(); // ✅ aucun slot sélectionné au démarrage
    }

    void Update()
    {
        // 🔹 1️⃣ Gestion de visibilité selon l’état de l’inventaire
        bool inventoryOpen = (inventoryUI != null && inventoryUI.activeSelf);
        targetAlpha = inventoryOpen ? 0f : 1f;

        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        canvasGroup.interactable = canvasGroup.alpha > 0.9f;
        canvasGroup.blocksRaycasts = canvasGroup.alpha > 0.9f;

        for (int i = 0; i < slots.Count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                // on demande le toggle logique au manager
                if (hotbarManager != null)
                    hotbarManager.ToggleHotbarSlotByIndex(i);

                // et on gère uniquement le visuel ici
                if (currentIndex == i)
                    DeselectSlot(); // pour highlight
                else
                    SelectSlot(i);
            }
        }
        // 🔹 3️⃣ Mise à jour icônes
        RefreshIcons();
    }

    void SelectSlot(int index)
    {
        // si on reclique sur le même slot → on déséquipe tout
        if (index == currentIndex)
        {
            DeselectSlot();
            return;
        }

        // sélection normale
        if (index < 0 || index >= slots.Count) return;
        currentIndex = index;

        // déplace le highlight
        if (selectionHighlight != null && slots[index].background != null)
        {
            selectionHighlight.transform.SetParent(slots[index].background.transform, false);
            selectionHighlight.rectTransform.anchoredPosition = Vector2.zero;
            selectionHighlight.enabled = true;
        }

        // coloration du numéro actif
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].numberText != null)
                slots[i].numberText.color = (i == currentIndex ? Color.yellow : Color.white);
        }
    }

    void DeselectSlot()
    {
        // retire la sélection
        currentIndex = -1;

        // désactive le highlight
        if (selectionHighlight != null)
            selectionHighlight.enabled = false;

        // remet la couleur des numéros à blanc
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].numberText != null)
                slots[i].numberText.color = Color.white;
        }

        // déséquipe l’objet du joueur
        if (hotbarManager != null && hotbarManager.playerEquipHandler != null)
            hotbarManager.playerEquipHandler.UnequipAll();
    }

    void RefreshIcons()
    {
        if (hotbarManager == null) return;

        for (int i = 0; i < slots.Count; i++)
        {
            var ui = slots[i];
            if (hotbarManager.hotbarSlots.Count <= i) continue;

            var linkedSlot = hotbarManager.hotbarSlots[i].linkedSlot;
            if (linkedSlot != null && linkedSlot.CurrentItem != null)
            {
                ui.icon.enabled = true;
                ui.icon.sprite = linkedSlot.CurrentItem.itemData.icon;
            }
            else
            {
                ui.icon.enabled = false;
                ui.icon.sprite = null;
            }

            if (ui.numberText != null)
                ui.numberText.text = (i + 1).ToString();
        }
    }
}
