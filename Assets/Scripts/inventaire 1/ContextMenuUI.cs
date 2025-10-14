using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ContextMenuUI : MonoBehaviour
{
    public static ContextMenuUI Instance;

    [Header("UI Elements")]
    public GameObject panel;
    public CanvasGroup canvasGroup;
    public Button splitButton;
    public Button dropButton;
    public Button dropStackButton;
    public Button equipButton;
    public Button inspectButton;

    private ItemUI currentItem;
    private RectTransform panelRect;
    private bool isVisible = false;

    private void Awake()
    {
        Instance = this;
        panelRect = panel.GetComponent<RectTransform>();
        HideInstant();

        // Lier les boutons à leurs actions
        splitButton.onClick.AddListener(OnSplit);
        dropButton.onClick.AddListener(OnDrop);
        dropStackButton.onClick.AddListener(OnDropStack);
        inspectButton.onClick.AddListener(OnInspect);
        equipButton.onClick.AddListener(OnEquip);
    }

    private void Update()
    {
        // clic gauche ailleurs → ferme
        if (isVisible && Input.GetMouseButtonDown(0))
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(panelRect, Input.mousePosition))
                Hide();
        }
    }

    public void Show(ItemUI itemUI, Vector2 pos)
    {
        if (itemUI == null) return;
        currentItem = itemUI;

        if (panel == null || panelRect == null || canvasGroup == null)
        {
            Debug.LogError("[ContextMenuUI] Panel ou composants manquants !");
            return;
        }

        // 🟢 Active le panneau
        panel.SetActive(true);
        isVisible = true;

        // 🧭 Positionne près du curseur
        Vector2 adjustedPos = pos + new Vector2(15f, -15f);
        panelRect.position = adjustedPos;

        // 🧱 Empêche de sortir de l’écran
        Vector2 size = panelRect.sizeDelta;
        Vector2 clampedPos = adjustedPos;
        clampedPos.x = Mathf.Clamp(clampedPos.x, size.x / 2, Screen.width - size.x / 2);
        clampedPos.y = Mathf.Clamp(clampedPos.y, size.y / 2, Screen.height - size.y / 2);
        panelRect.position = clampedPos;

        // 🎞 Animation d’ouverture
        canvasGroup.alpha = 0f;
        panelRect.localScale = Vector3.one * 0.8f;
        LeanTween.cancel(panel);
        LeanTween.value(panel, 0f, 1f, 0.15f)
            .setEaseOutCubic()
            .setOnUpdate((float val) =>
            {
                canvasGroup.alpha = val;
                panelRect.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, val);
            });

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // 🎯 Setup des boutons (Drop / Split / Inspect / Équipé)
        dropButton.onClick.RemoveAllListeners();
        splitButton.onClick.RemoveAllListeners();
        inspectButton.onClick.RemoveAllListeners();
        equipButton.onClick.RemoveAllListeners();

        // 🔸 Drop
        dropButton.onClick.AddListener(() =>
        {
            PlayerPickupManager ppm = FindFirstObjectByType<PlayerPickupManager>();
            if (ppm != null)
            {
                ppm.DropSpecificItem(currentItem);
            }
            Hide();
        });

        // 🔸 Drop tout le stack
        if (dropStackButton != null)
        {
            dropStackButton.gameObject.SetActive(currentItem.itemData.isStackable && currentItem.currentStack > 1);
            dropStackButton.onClick.RemoveAllListeners();
            dropStackButton.onClick.AddListener(() =>
            {
                PlayerPickupManager ppm = FindFirstObjectByType<PlayerPickupManager>();
                if (ppm != null)
                    ppm.DropEntireStack(currentItem);
                Hide();
            });
        }

        // 🔸 Split
        splitButton.gameObject.SetActive(currentItem.itemData.isStackable && currentItem.currentStack > 1);
        splitButton.onClick.AddListener(() =>
        {
            if (SplitStackWindow.Instance != null)
                SplitStackWindow.Instance.Open(currentItem);
            Hide();
        });

        // 🔸 Inspect
        inspectButton.onClick.AddListener(() =>
        {
            Tooltip tooltip = FindFirstObjectByType<Tooltip>();
            if (tooltip != null)
            {
                if (tooltip.background.activeSelf)
                    tooltip.Hide();
                else
                    tooltip.Show(currentItem.itemData);
            }
            Hide();
        });

        // 🔸 Équiper / Déséquiper
        bool isEquipped = false;
        if (EquipementManager.Instance != null && EquipementManager.Instance.equipSlots != null)
        {
            foreach (var slot in EquipementManager.Instance.equipSlots)
            {
                if (slot.CurrentItem == currentItem)
                {
                    isEquipped = true;
                    break;
                }
            }
        }

        equipButton.GetComponentInChildren<TextMeshProUGUI>().text = isEquipped ? "Unequip" : "Equip";
        equipButton.onClick.AddListener(() =>
        {
            if (EquipementManager.Instance == null)
            {
                UIMessage.Instance.Show("Aucun gestionnaire d'équipement trouvé !");
                return;
            }

            if (isEquipped)
            {
                EquipementManager.Instance.UnequipItem(currentItem);
            }
            else
            {
                bool equipped = EquipementManager.Instance.TryEquipItem(currentItem);
                if (!equipped)
                    UIMessage.Instance.Show("Cet objet ne peut pas être équipé !");
            }

            Hide();
        });
    }

    public void Hide()
    {
        if (!isVisible) return;

        isVisible = false;
        LeanTween.cancel(panel);
        LeanTween.value(panel, 1f, 0f, 0.12f)
            .setEaseInCubic()
            .setOnUpdate((float val) =>
            {
                canvasGroup.alpha = val;
                panelRect.localScale = Vector3.one * Mathf.Lerp(1f, 0.9f, 1 - val);
            })
            .setOnComplete(() =>
            {
                panel.SetActive(false);
                canvasGroup.alpha = 0f;
            });
    }

    public void HideInstant()
    {
        if (panel == null) return;
        panel.SetActive(false);
        isVisible = false;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    // ======================  ACTIONS DES BOUTONS  ======================

    private void OnSplit()
    {
        if (currentItem == null) return;

        if (SplitStackWindow.Instance != null)
        {
            SplitStackWindow.Instance.Open(currentItem);
            Debug.Log($"[ContextMenu] Split ouvert pour {currentItem.itemData.itemName}");
        }
        else
        {
            Debug.LogWarning("[ContextMenu] Aucun SplitStackWindow trouvé !");
        }

        Hide();
    }

    private void OnDrop()
    {
        if (currentItem == null) return;

        Debug.Log($"[ContextMenu] Drop de {currentItem.itemData.itemName}");

        // On appelle ton PlayerPickupManager pour gérer le drop
        PlayerPickupManager ppm = FindFirstObjectByType<PlayerPickupManager>();
        if (ppm != null)
        {
            ppm.DropSpecificItem(currentItem);
        }
        else
        {
            // Si jamais il n’existe pas encore, on supprime l’item directement
            InventoryManager.Instance.RemoveItem(currentItem);
        }

        Hide();
    }

    private void OnDropStack()
    {
        if (currentItem == null) return;

        PlayerPickupManager ppm = FindFirstObjectByType<PlayerPickupManager>();
        if (ppm != null)
        {
            ppm.DropEntireStack(currentItem);
        }
        else
        {
            // fallback sécurité
            InventoryManager.Instance.RemoveItem(currentItem);
        }

        Hide();
    }

    private void OnInspect()
    {
        if (currentItem == null) return;

        Tooltip tooltip = FindFirstObjectByType<Tooltip>();
        if (tooltip != null)
        {
            // Si le tooltip est déjà affiché pour le même item → on le ferme
            if (tooltip.background.activeSelf &&
                tooltip.itemNameText.text == currentItem.itemData.itemName)
            {
                tooltip.Hide();
            }
            else
            {
                tooltip.Show(currentItem.itemData);
            }
        }

        Hide();
    }

    private void OnEquip()
    {
        if (currentItem == null)
            return;

        bool equipped = EquipementManager.Instance != null && EquipementManager.Instance.TryEquipItem(currentItem);

        if (!equipped)
        {
            if (UIMessage.Instance != null)
                UIMessage.Instance.Show("Cet objet ne peut pas être équipé !");
            else
                Debug.Log("[Equip] Échec : slot incompatible ou plein.");
        }

        Hide();
    }
}
