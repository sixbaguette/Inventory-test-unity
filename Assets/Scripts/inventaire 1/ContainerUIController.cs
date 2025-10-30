using UnityEngine;

public class ContainerUIController : MonoBehaviour
{
    public static ContainerUIController Instance;

    [Header("Références UI")]
    public GameObject containerUIRoot;
    public CanvasGroup canvasGroup;
    public ContainerInventoryManager containerInv;

    [Header("Position de la grille joueur")]
    public RectTransform playerGridPanel;        // 🔹 panel de la grille joueur
    public Vector2 offsetWhenContainerOpen = new Vector2(-150f, 0f); // 🔹 déplacement ajustable
    public float moveSpeed = 0.25f;

    private Vector2 originalPos;
    private bool isMoved = false;

    private Container currentContainer;

    void Awake()
    {
        Instance = this;

        if (canvasGroup == null)
        {
            canvasGroup = containerUIRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = containerUIRoot.AddComponent<CanvasGroup>();
        }

        containerUIRoot.SetActive(false);
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (playerGridPanel != null)
            originalPos = playerGridPanel.anchoredPosition;
    }

    public bool IsContainerOpen => currentContainer != null;

    public void OpenContainer(Container container)
    {
        if (container == null)
        {
            Debug.LogWarning("[ContainerUI] Aucun conteneur à ouvrir !");
            return;
        }

        Debug.Log($"[ContainerUI] Ouverture du conteneur : {container.containerName}");

        // 🔹 Cache les éléments inutiles
        if (InventoryUIHider.Instance != null)
            InventoryUIHider.Instance.HideForContainer();

        // 🔹 Ouvre l’inventaire joueur si fermé
        var toggle = FindFirstObjectByType<InventoryToggle>();
        if (toggle != null && !InventoryToggle.IsInventoryOpen)
            toggle.ToggleInventory();

        // 🔹 Initialise la grille du conteneur
        containerUIRoot.SetActive(true);
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = false; // ✅ ne bloque pas les raycasts sur l'inventaire joueur

        currentContainer = container;
        container.LoadInto(containerInv);

        // 🔧 Supprime tout Canvas local des items instanciés dans le conteneur
        foreach (Transform child in containerInv.itemsLayer)
        {
            var ui = child.GetComponent<ItemUI>();
            if (ui != null)
            {
                var localCanvas = ui.GetComponent<Canvas>();
                if (localCanvas != null) Destroy(localCanvas);

                var gr = ui.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (gr != null) Destroy(gr);

                ui.EnsureCanvasRaycastable(); // garde le raycast fonctionnel
            }
        }

        // 🔹 Déplace la grille joueur vers la gauche
        if (playerGridPanel != null && !isMoved)
        {
            LeanTween.cancel(playerGridPanel);
            Vector2 targetPos = originalPos + offsetWhenContainerOpen;
            LeanTween.move(playerGridPanel, targetPos, moveSpeed).setEaseOutCubic();
            // 🟢 Déplace aussi la couche d'items du joueur pour suivre la grille
            if (InventoryManager.Instance != null && InventoryManager.Instance.itemsLayer != null)
            {
                var layer = InventoryManager.Instance.itemsLayer;
                LeanTween.move(layer, targetPos, moveSpeed).setEaseOutCubic();
            }
            isMoved = true;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseContainer()
    {
        if (currentContainer == null) return;

        Debug.Log("[ContainerUI] Fermeture du conteneur");

        // 🔹 Réaffiche les parties cachées
        if (InventoryUIHider.Instance != null)
            InventoryUIHider.Instance.ShowAfterContainer();

        // 🔹 Cache la grille conteneur
        containerUIRoot.SetActive(false);
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // 🔹 Replace la grille joueur à sa position d’origine
        if (playerGridPanel != null && isMoved)
        {
            LeanTween.cancel(playerGridPanel);
            LeanTween.move(playerGridPanel, originalPos, moveSpeed).setEaseOutCubic();

            // 🔵 Replace la couche d'items du joueur à sa position d'origine
            if (InventoryManager.Instance != null && InventoryManager.Instance.itemsLayer != null)
            {
                var layer = InventoryManager.Instance.itemsLayer;
                LeanTween.move(layer, originalPos, moveSpeed).setEaseOutCubic();
            }
            isMoved = false;
        }

        // 🧩 Sauvegarde le contenu actuel du conteneur dans sa mémoire
        if (currentContainer != null)
            currentContainer.SaveFrom(containerInv);

        // 🧹 Nettoie toutes les instances d’items visuelles de la grille
        if (containerInv != null && containerInv.itemsLayer != null)
        {
            for (int i = containerInv.itemsLayer.childCount - 1; i >= 0; i--)
            {
                GameObject.Destroy(containerInv.itemsLayer.GetChild(i).gameObject);
            }
        }

        // 🧽 Vide tous les slots (plus aucun item assigné)
        if (containerInv != null && containerInv.slots != null)
        {
            foreach (var s in containerInv.slots)
                if (s != null) s.ClearItem();
        }

        // 🔹 Termine la fermeture logique
        if (currentContainer != null)
            currentContainer.OnUIClosed();

        currentContainer = null;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log("[ContainerUI] Conteneur fermé, items détruits et grille réinitialisée.");
    }

    public ContainerInventoryManager GetActiveContainerInventory()
    {
        // on ne dépend plus de activeSelf, on vérifie juste que le canvas est visible
        if (containerInv != null && canvasGroup != null && canvasGroup.alpha > 0f)
            return containerInv;
        return null;
    }
}
