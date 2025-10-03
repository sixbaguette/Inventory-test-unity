using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName;
    public string description;
    public Sprite icon;

    [Header("Dimensions")]
    public int width = 1;  // Nombre de slots horizontaux
    public int height = 1; // Nombre de slots verticaux
}
