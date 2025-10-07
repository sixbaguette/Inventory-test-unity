using UnityEngine;
using UnityEngine.UI;

public class InventoryToggle : MonoBehaviour
{
    [Header("Références UI")]
    public GameObject inventoryUI;       // Le parent des slots et items
    public KeyCode toggleKey = KeyCode.I; // Touche pour ouvrir/fermer

    [Header("Bouton optionnel")]
    public Button toggleButton;

    private bool isOpen = false;

    private void Start()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleInventory);

        // Cache tout au démarrage
        inventoryUI.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        isOpen = !isOpen;
        inventoryUI.SetActive(isOpen);
        //InventoryManager.Instance.SetInventoryVisible(isOpen);

        // Optionnel : on bloque le curseur si fermé
        Cursor.visible = isOpen;
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
