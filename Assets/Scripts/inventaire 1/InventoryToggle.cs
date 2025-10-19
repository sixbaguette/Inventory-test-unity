using UnityEngine;
using UnityEngine.UI;

public class InventoryToggle : MonoBehaviour
{
    [Header("Références UI")]
    public GameObject inventoryUI;        // parent du Canvas d’inventaire
    public CanvasGroup canvasGroup;       // ajoute un CanvasGroup sur le même objet
    public KeyCode toggleKey = KeyCode.I; // touche d’ouverture

    [Header("Player Controls")]
    public MonoBehaviour firstPersonController; // ton script FPS

    [Header("Bouton optionnel")]
    public Button toggleButton;

    private bool isOpen = false;
    public static bool IsInventoryOpen = false;
    private bool isAnimating = false;

    private void Start()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleInventory);

        // 🔥 Active temporairement le Canvas pour init la grille
        inventoryUI.SetActive(true);
        InventoryManager.Instance.InitializeGrid();
        inventoryUI.SetActive(false);

        // S’assure que le CanvasGroup est présent
        if (canvasGroup == null)
        {
            canvasGroup = inventoryUI.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = inventoryUI.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0;
        inventoryUI.transform.localScale = Vector3.zero;
        inventoryUI.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ToggleInventory();
    }

    public void ToggleInventory()
    {
        if (isAnimating) return; // bloque le spam pendant l’anim
        isOpen = !isOpen;

        if (isOpen)
            OpenInventory();
        else
            CloseInventory();
    }

    private void OpenInventory()
    {
        IsInventoryOpen = true;
        inventoryUI.SetActive(true);
        isAnimating = true;

        // réinit
        canvasGroup.alpha = 0f;
        inventoryUI.transform.localScale = Vector3.zero;

        if (InventoryAudioManager.Instance != null)
            InventoryAudioManager.Instance.Play("open_inventory");

        // fade + scale
        LeanTween.value(inventoryUI, 0f, 1f, 0.25f)
            .setEaseOutCubic()
            .setOnUpdate((float val) =>
            {
                canvasGroup.alpha = val;
                inventoryUI.transform.localScale = Vector3.one * val;
            })
            .setOnComplete(() =>
            {
                canvasGroup.alpha = 1f;
                inventoryUI.transform.localScale = Vector3.one;
                isAnimating = false;
            });

        // Curseur visible
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        // 🔥 désactive les contrôles FPS
        if (firstPersonController != null)
            firstPersonController.enabled = false;
    }

    private void CloseInventory()
    {
        IsInventoryOpen = false;
        isAnimating = true;

        if (InventoryAudioManager.Instance != null)
            InventoryAudioManager.Instance.Play("close_inventory");

        // fade out + shrink
        LeanTween.value(inventoryUI, 1f, 0f, 0.2f)
            .setEaseInBack()
            .setOnUpdate((float val) =>
            {
                canvasGroup.alpha = val;
                inventoryUI.transform.localScale = Vector3.one * val;
            })
            .setOnComplete(() =>
            {
                inventoryUI.SetActive(false);
                isAnimating = false;
            });

        // Ferme aussi le mode inspecteur 3D s’il est ouvert
        Tooltip tooltip = FindFirstObjectByType<Tooltip>();
        if (tooltip != null && tooltip.IsInspecting)
        {
            tooltip.HideAll();
        }

        // Cache le curseur
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        // 🔥 réactive les contrôles FPS
        if (firstPersonController != null)
            firstPersonController.enabled = true;
    }
}
