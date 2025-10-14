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
            // ⚙️ Empêche un split invalide
            amount = Mathf.Clamp(amount, 1, sourceItem.currentStack - 1);

            var inv = InventoryManager.Instance;
            var data = sourceItem.itemData;

            // ✅ D’abord, on cherche une place libre normalement
            bool placed = inv.FindFirstFreePosition(data, out int x, out int y);

            // 🌀 Si aucune place dans l’orientation actuelle, tente la rotation inverse
            if (!placed)
            {
                int oldW = data.width;
                int oldH = data.height;
                data.width = oldH;
                data.height = oldW;

                placed = inv.FindFirstFreePosition(data, out x, out y);

                if (placed)
                {
                    // Ajuste la taille visuelle du nouvel item après rotation
                    data.width = oldH;
                    data.height = oldW;
                }
                else
                {
                    // revert si aucune place même pivoté
                    data.width = oldW;
                    data.height = oldH;

                    Debug.LogWarning("[Split] Inventaire plein, aucune place dans aucune orientation !");
                    Close();
                    return;
                }
            }

            // ✅ Réduit le stack d’origine
            sourceItem.currentStack -= amount;
            sourceItem.UpdateStackText();

            // ✅ Crée le nouveau stack
            GameObject go = Instantiate(inv.itemUIPrefab, inv.itemsLayer);
            ItemUI newStack = go.GetComponent<ItemUI>();
            newStack.Setup(sourceItem.itemData);
            newStack.currentStack = amount;
            newStack.UpdateStackText();

            // ✅ Ajoute à la liste interne
            inv.AddToInventoryList(newStack);

            // ✅ Place le nouvel item dans la grille
            inv.PlaceItem(newStack, x, y);

            // ✅ Ajuste son visuel à la bonne rotation
            newStack.UpdateSize();
            newStack.UpdateOutline();
            newStack.ResetVisualLayout();

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
