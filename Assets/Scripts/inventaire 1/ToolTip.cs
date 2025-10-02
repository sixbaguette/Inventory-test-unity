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
        initialPosition = transform.position; // sauvegarde la position de départ
        Hide();
    }

    public void Show(Item item)
    {
        if (item == null) return;

        itemNameText.text = item.itemName;
        itemDescriptionText.text = item.description;

        background.SetActive(true);
        transform.position = initialPosition; // fixe la position à l'origine
    }

    public void Hide()
    {
        background.SetActive(false);
    }
}
