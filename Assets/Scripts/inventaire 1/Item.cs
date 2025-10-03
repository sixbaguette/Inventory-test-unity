using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName;
    public string description; // ← Ajouté pour ToolTip
    public Sprite icon;
    public int width = 1;  // Largeur en slots
    public int height = 1; // Hauteur en slots
}
