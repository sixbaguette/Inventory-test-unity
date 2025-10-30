using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SplitStackWindow : MonoBehaviour
{
    public static SplitStackWindow Instance;

    [Header("UI References")]
    public TMP_InputField inputField;
    public Button okButton;
    public Button cancelButton;
    public TextMeshProUGUI titleText;

    private ItemUI sourceItem;

    private void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);

        // 🔝 Force le SplitStackWindow à être dans un Canvas overlay tout en haut
        Canvas overlay = null;
        var existing = GameObject.Find("TopOverlayCanvas");
        if (existing != null)
        {
            overlay = existing.GetComponent<Canvas>();
        }
        if (overlay == null)
        {
            var go = new GameObject("TopOverlayCanvas", typeof(Canvas), typeof(GraphicRaycaster));
            overlay = go.GetComponent<Canvas>();
            overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            overlay.overrideSorting = true;
            overlay.sortingOrder = 5000;
            DontDestroyOnLoad(go);
        }

        // Replace ce menu sous ce canvas
        transform.SetParent(overlay.transform, false);
        transform.SetAsLastSibling();

        // Sécurité : ajoute un CanvasGroup pour bloquer les clics derrière
        var cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        cg.interactable = true;
        cg.blocksRaycasts = true;
        cg.alpha = 1f;

        okButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(Close);
    }

    public void Open(ItemUI source)
    {
        if (source == null || !source.itemData.isStackable || source.currentStack <= 1)
            return;

        sourceItem = source;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        inputField.text = "";
        inputField.ActivateInputField();

        if (titleText != null)
            titleText.text = $"Split {source.itemData.itemName} ({source.currentStack})";

        // positionner la fenêtre près de la souris
        RectTransform rect = GetComponent<RectTransform>();
        Vector2 mousePos = Input.mousePosition;
        rect.position = mousePos + new Vector2(100f, -50f);
    }

    private void OnConfirm()
    {
        if (sourceItem == null)
        {
            Close();
            return;
        }

        if (!int.TryParse(inputField.text, out int amount))
        {
            Close();
            return;
        }

        // borne : 1 .. currentStack-1
        amount = Mathf.Clamp(amount, 1, sourceItem.currentStack - 1);
        if (amount <= 0)
        {
            Close();
            return;
        }

        var data = sourceItem.itemData;
        if (data == null)
        {
            Close();
            return;
        }

        // ➜ Détermine la "même" origine que la source
        bool fromPlayer = (sourceItem.Owner == ItemUI.ItemOwner.Player);
        bool fromContainer = (sourceItem.Owner == ItemUI.ItemOwner.Container);

        InventoryManager inv = null;
        ContainerInventoryManager cont = null;

        if (fromPlayer)
        {
            inv = InventoryManager.Instance;
            if (inv == null) { Debug.LogWarning("[SplitStack] InventoryManager introuvable."); Close(); return; }
        }
        else if (fromContainer)
        {
            // 1) parent direct si possible
            cont = sourceItem.GetComponentInParent<ContainerInventoryManager>();
            // 2) sinon l'instance ouverte
            if (cont == null) cont = ContainerUIController.Instance != null ? ContainerUIController.Instance.containerInv : null;
            // 3) sinon cherche dans la scène (rare)
            if (cont == null)
            {
                var all = GameObject.FindObjectsByType<ContainerInventoryManager>(FindObjectsSortMode.None);
                foreach (var c in all)
                {
                    if (c == null) continue;
                    if (sourceItem.transform.IsChildOf(c.transform) ||
                        (c.GetComponentInParent<Canvas>() == sourceItem.GetComponentInParent<Canvas>()))
                    {
                        cont = c; break;
                    }
                }
            }
            if (cont == null) { Debug.LogWarning("[SplitStack] ContainerInventoryManager introuvable."); Close(); return; }
        }
        else
        {
            // Fallback : essaie au moins de savoir où est l'UI
            inv = sourceItem.GetComponentInParent<InventoryManager>();
            cont = sourceItem.GetComponentInParent<ContainerInventoryManager>();
            if (inv == null && cont == null)
            {
                Debug.LogWarning("[SplitStack] Impossible de déterminer la source du stack (aucun inventaire trouvé).");
                Close();
                return;
            }
            fromPlayer = inv != null;
            fromContainer = cont != null;
        }

        // ➜ Réduit le stack d’origine
        sourceItem.currentStack -= amount;
        sourceItem.UpdateStackText();

        // ➜ Crée le nouveau stack DANS LE MÊME INVENTAIRE
        if (fromPlayer)
        {
            // instancie dans la couche Items du joueur
            GameObject go = Instantiate(inv.itemUIPrefab, inv.itemsLayer != null ? inv.itemsLayer : inv.slotParent);
            ItemUI newStack = go.GetComponent<ItemUI>();
            newStack.Setup(data);
            newStack.currentStack = amount;
            newStack.UpdateStackText();
            newStack.Owner = ItemUI.ItemOwner.Player;

            // placement (position libre + rotation auto possible)
            if (!inv.FindFirstFreePosition(data, out int x, out int y))
            {
                // Essaie auto-place (gère la rotation interne)
                if (!inv.TryAutoPlace(newStack))
                {
                    // rollback si vraiment pas de place
                    Destroy(go);
                    sourceItem.currentStack += amount;
                    sourceItem.UpdateStackText();
                    Debug.LogWarning("[SplitStack] Inventaire joueur plein, split annulé.");
                    Close();
                    return;
                }
            }
            else
            {
                inv.PlaceItem(newStack, x, y);
            }

            inv.AddToInventoryList(newStack);
            newStack.UpdateSize();
            newStack.UpdateOutline();
            newStack.ResetVisualLayout();
        }
        else // fromContainer
        {
            // instancie dans la couche Items du container
            GameObject go = Instantiate(cont.itemUIPrefab, cont.itemsLayer != null ? cont.itemsLayer : cont.slotParent);
            ItemUI newStack = go.GetComponent<ItemUI>();
            newStack.Setup(data);
            newStack.currentStack = amount;
            newStack.UpdateStackText();
            newStack.Owner = ItemUI.ItemOwner.Container;

            // placement (position libre + rotation auto possible)
            Vector2Int? pos = cont.FindFreeSpaceFor(data);
            if (!pos.HasValue)
            {
                if (!cont.TryAutoPlace(newStack))
                {
                    // rollback si vraiment pas de place
                    Destroy(go);
                    sourceItem.currentStack += amount;
                    sourceItem.UpdateStackText();
                    Debug.LogWarning("[SplitStack] Conteneur plein, split annulé.");
                    Close();
                    return;
                }
            }
            else
            {
                cont.PlaceItem(newStack, pos.Value.x, pos.Value.y);
            }

            cont.AddToInventoryList(newStack);
            newStack.UpdateSize();
            newStack.UpdateOutline();
            newStack.ResetVisualLayout();
        }

        Debug.Log($"[Split] Nouveau stack de {amount} créé pour {sourceItem.itemData.itemName} dans {(fromPlayer ? "l'inventaire joueur" : "le conteneur")}.");
        Close();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        sourceItem = null;
    }
}

// 🔧 Helper pour afficher le chemin complet d'un transform
public static class TransformExtensions
{
    public static string GetHierarchyPath(this Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}