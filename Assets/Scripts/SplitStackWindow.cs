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
        if (sourceItem == null) { Close(); return; }

        if (int.TryParse(inputField.text, out int amount))
        {
            amount = Mathf.Clamp(amount, 1, sourceItem.currentStack - 1);

            // Réduit le stack source
            sourceItem.currentStack -= amount;
            sourceItem.UpdateStackText();

            // Crée un nouveau stack identique
            GameObject go = Instantiate(InventoryManager.Instance.itemUIPrefab, InventoryManager.Instance.itemsLayer);
            ItemUI newStack = go.GetComponent<ItemUI>();
            newStack.Setup(sourceItem.itemData);
            newStack.currentStack = amount;
            newStack.UpdateStackText();

            if (InventoryManager.Instance.FindFirstFreePosition(sourceItem.itemData, out int x, out int y))
                InventoryManager.Instance.PlaceItem(newStack, x, y);
            else
                Debug.LogWarning("Aucune place pour le nouveau stack séparé !");
        }

        Close();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        sourceItem = null;
    }
}
