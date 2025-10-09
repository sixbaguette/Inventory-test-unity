using UnityEngine;
using TMPro;

public class Tooltip : MonoBehaviour
{
    public GameObject background;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;

    private Vector3 initialPosition;

    private void Awake()
    {
        initialPosition = transform.position;
        Hide();
    }

    // NOTE : on utilise ItemData (ton ScriptableObject actuel)
    public void Show(ItemData item)
    {
        if (item == null) return;

        if (itemNameText != null) itemNameText.text = item.itemName;
        if (itemDescriptionText != null) itemDescriptionText.text = item.description;

        if (background != null) background.SetActive(true);
        transform.position = initialPosition;
    }

    public void Hide()
    {
        if (background != null) background.SetActive(false);
    }
}
