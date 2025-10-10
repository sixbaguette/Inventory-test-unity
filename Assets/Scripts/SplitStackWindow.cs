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

        if (int.TryParse(inputField.text, out int amount))
        {
            // empêche un split invalide (ex. 0 ou >= stack total)
            amount = Mathf.Clamp(amount, 1, sourceItem.currentStack - 1);

            // ✅ Vérifie d'abord s’il y a de la place libre dans l’inventaire
            if (!InventoryManager.Instance.FindFirstFreePosition(sourceItem.itemData, out int x, out int y))
            {
                Debug.LogWarning("[Split] Inventaire plein, impossible de séparer le stack !");
                // Optionnel : feedback visuel
                Close();
                return;
            }

            // ✅ Réduit le stack d’origine
            sourceItem.currentStack -= amount;
            sourceItem.UpdateStackText();

            // ✅ Crée le nouveau stack seulement si place trouvée
            GameObject go = Instantiate(InventoryManager.Instance.itemUIPrefab, InventoryManager.Instance.itemsLayer);
            ItemUI newStack = go.GetComponent<ItemUI>();
            newStack.Setup(sourceItem.itemData);
            newStack.currentStack = amount;
            newStack.UpdateStackText();

            // ✅ Ajoute le nouvel item à la liste interne (important pour le drop / gestion)
            InventoryManager.Instance.AddToInventoryList(newStack);

            // ✅ Place le nouveau stack à la position trouvée
            InventoryManager.Instance.PlaceItem(newStack, x, y);

            Debug.Log($"[Split] Nouveau stack de {amount} créé pour {sourceItem.itemData.itemName}");
        }

        Close();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        sourceItem = null;
    }
}
