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

    private Canvas topOverlayCanvas;

    private void Awake()
    {
        Instance = this;
        panelRect = panel.GetComponent<RectTransform>();
        HideInstant();

        // 🔝 Assure un canvas tout en haut
        var existing = GameObject.Find("TopOverlayCanvas");
        if (existing != null) topOverlayCanvas = existing.GetComponent<Canvas>();
        if (topOverlayCanvas == null)
        {
            var go = new GameObject("TopOverlayCanvas", typeof(Canvas), typeof(GraphicRaycaster));
            topOverlayCanvas = go.GetComponent<Canvas>();
            topOverlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            topOverlayCanvas.overrideSorting = true;
            topOverlayCanvas.sortingOrder = 5000;   // bien au-dessus des items/grilles
            DontDestroyOnLoad(go);
        }

        // 👇 Place ce ContextMenu sous ce canvas
        transform.SetParent(topOverlayCanvas.transform, false);
        transform.SetAsLastSibling();

        // Listeners (inchangé)
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

        // 🔝 Place le menu dans le canvas global overlay
        if (InventoryManager.Instance != null && InventoryManager.Instance.overlayCanvas != null)
        {
            Canvas overlay = InventoryManager.Instance.overlayCanvas;
            panel.transform.SetParent(overlay.transform, false);

            // S'assure que le canvas overlay est bien au-dessus de tout
            overlay.overrideSorting = true;
            overlay.sortingOrder = 10000;
        }
        else
        {
            Debug.LogWarning("[ContextMenuUI] Aucun overlayCanvas trouvé, le menu risque d'être masqué !");
        }
        currentItem = itemUI;

        if (topOverlayCanvas == null)  // sécurité si Awake ne s’est pas exécuté
        {
            topOverlayCanvas = GameObject.Find("TopOverlayCanvas")?.GetComponent<Canvas>();
            if (topOverlayCanvas == null) return;
            transform.SetParent(topOverlayCanvas.transform, false);
        }

        // 🔝 On passe tout en haut à chaque ouverture
        transform.SetParent(topOverlayCanvas.transform, false);
        transform.SetAsLastSibling();

        panel.SetActive(true);
        isVisible = true;

        // Position + clamp (ton code)
        Vector2 adjustedPos = pos + new Vector2(15f, -15f);
        panelRect.position = adjustedPos;
        Vector2 size = panelRect.sizeDelta;
        Vector2 clampedPos = adjustedPos;
        clampedPos.x = Mathf.Clamp(clampedPos.x, size.x / 2, Screen.width - size.x / 2);
        clampedPos.y = Mathf.Clamp(clampedPos.y, size.y / 2, Screen.height - size.y / 2);
        panelRect.position = clampedPos;

        // Fade in (ton code)
        canvasGroup.alpha = 0f;
        panelRect.localScale = Vector3.one * 0.8f;
        LeanTween.cancel(panel);
        LeanTween.value(panel, 0f, 1f, 0.15f)
            .setEaseOutCubic()
            .setOnUpdate((float val) =>
            {
                canvasGroup.alpha = val;
                panelRect.localScale = Vector3.one * Mathf.Lerp(1.5f, 1.5f, val);
            });

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Sécurité : si le Canvas parent du panel n’a pas de GraphicRaycaster
        var canvasParent = panel.GetComponentInParent<Canvas>();
        if (canvasParent != null)
        {
            var gr = canvasParent.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (gr == null) canvasParent.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // 🎯 Setup des boutons (Drop / Split / Inspect / Équipé)
        dropButton.onClick.RemoveAllListeners();
        splitButton.onClick.RemoveAllListeners();
        inspectButton.onClick.RemoveAllListeners();
        equipButton.onClick.RemoveAllListeners();

        // 🔸 Drop
        dropButton.onClick.RemoveAllListeners();
        dropButton.onClick.AddListener(() =>
        {
            var ppm = FindFirstObjectByType<PlayerPickupManager>();
            var equipSlot = currentItem.GetComponentInParent<EquipementSlot>();

            if (equipSlot != null && equipSlot.CurrentItem == currentItem)
            {
                // ✅ Cas : l’objet est dans un slot d’équipement
                if (currentItem.itemData.isStackable && currentItem.currentStack > 1)
                {
                    // 🟢 Drop 1 exemplaire, reste équipé
                    ppm?.DropSpecificItem(currentItem);

                    // Réactualise juste le visuel du slot (on garde l’objet)
                    equipSlot.ForceRefreshVisual(currentItem);
                    Debug.Log($"[EquipSlot Drop] 1x {currentItem.itemData.itemName} lâché depuis équipement (reste {currentItem.currentStack}).");
                }
                else
                {
                    // 🔴 Dernier exemplaire ou non-stackable → drop complet et vide le slot
                    ppm?.DropSpecificItem(currentItem);
                    equipSlot.ForceClearSlot();  // ⬅️ au lieu de UnequipItem()
                    Debug.Log($"[EquipSlot Drop] {currentItem.itemData.itemName} lâché et slot vidé.");
                }
            }
            else
            {
                // 🔁 Drop normal depuis l’inventaire
                ppm?.DropSpecificItem(currentItem);
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
                var ppm = FindFirstObjectByType<PlayerPickupManager>();
                var equipSlot = currentItem.GetComponentInParent<EquipementSlot>();

                if (equipSlot != null && equipSlot.CurrentItem == currentItem)
                {
                    // ✅ Drop tout le stack → vide le slot après
                    ppm?.DropEntireStack(currentItem);
                    equipSlot.ForceClearSlot();
                    Debug.Log($"[EquipSlot DropStack] Stack complet de {currentItem.itemData.itemName} lâché et slot vidé.");
                }
                else
                {
                    ppm?.DropEntireStack(currentItem);
                }

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

        inspectButton.onClick.AddListener(() =>
        {
            Tooltip tooltip = FindFirstObjectByType<Tooltip>();
            if (tooltip != null)
            {
                if (tooltip.IsInspecting)
                    tooltip.HideAll();
                else
                    tooltip.ShowInspect3D(currentItem.itemData);
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
        // 🔧 Patch provisoire : rend tous les CanvasGroup parents interactifs
        Transform t = panel.transform;
        while (t != null)
        {
            var cg = t.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
                cg.alpha = 1f;
            }
            t = t.parent;
        }
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
                panelRect.localScale = Vector3.one * Mathf.Lerp(1.5f, 1.5f, 1.5f - val);
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

        // 🧩 Vérifie si l’item vient d’un slot d’équipement
        var equipSlot = currentItem.GetComponentInParent<EquipementSlot>();
        if (equipSlot != null)
        {
            Debug.Log("[ContextMenu] Drop depuis un slot d’équipement → on le déséquipe d’abord.");
            equipSlot.UnequipItem(); // ✅ vide le slot et replace l’item dans l’inventaire temporairement
        }

        // 🟢 Drop logique au sol
        PlayerPickupManager ppm = FindFirstObjectByType<PlayerPickupManager>();
        if (ppm != null)
        {
            ppm.DropSpecificItem(currentItem);
        }
        else
        {
            // fallback : supprime l'item de l'inventaire si aucun PlayerPickupManager
            InventoryManager.Instance.RemoveItem(currentItem);
        }

        Hide();
    }

    private void OnDropStack()
    {
        if (currentItem == null) return;

        var equipSlot = currentItem.GetComponentInParent<EquipementSlot>();
        if (equipSlot != null)
        {
            Debug.Log("[ContextMenu] Drop stack depuis un slot d’équipement → déséquipe d’abord.");
            equipSlot.UnequipItem();
        }

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
            // Si le mode inspecteur 3D est déjà actif → le fermer
            var bg = tooltip.background;
            if (bg != null && bg.activeSelf && tooltip.renderDisplay != null && tooltip.renderDisplay.gameObject.activeSelf)
            {
                tooltip.CloseInspect3D();
            }
            else
            {
                // Ouvre le mode Inspect 3D complet
                tooltip.ShowInspect3D(currentItem.itemData);
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
