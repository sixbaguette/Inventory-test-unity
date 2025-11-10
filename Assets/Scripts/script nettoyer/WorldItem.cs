using UnityEngine;
using TMPro;

public class WorldItem : MonoBehaviour
{
    [Header("Lien avec ScriptableObject")]
    public ItemData itemData; // ton ScriptableObject assigné dans l’inspecteur

    [Header("Stack info (optionnel)")]
    [Min(1)] public int stackCount = 1; // pour les objets stackables

    [Header("Affichage 3D (optionnel)")]
    public TextMeshPro countText; // world-space text au-dessus de l'objet

    [Header("Runtime State")]
    public int currentAmmo = -1; // -1 = non défini

    private void Start()
    {
        //Sécurité : s’il n’est pas stackable, toujours 1
        if (itemData != null && !itemData.isStackable)
            stackCount = 1;

        // empêche stackCount négatif
        if (stackCount < 1)
            stackCount = 1;

        UpdateVisual();

        // Si c’est une arme et que currentAmmo n’a pas été défini → full par défaut
        if (itemData != null && itemData.isGun && currentAmmo < 0)
            currentAmmo = itemData.ammoCapacity;
    }

    public void UpdateVisual()
    {
        if (countText != null)
        {
            if (itemData != null && itemData.isStackable && stackCount > 1)
                countText.text = "x" + stackCount;
            else
                countText.text = "";
        }
    }

    public void SetStackCount(int value)
    {
        stackCount = Mathf.Max(1, value);
        UpdateVisual();
    }

    // Appelé quand l’objet est ramassé
    public void OnPickedUp()
    {
        Destroy(gameObject);
    }

    // (Optionnel) pour le faire respawn
    public void SpawnAt(Vector3 position)
    {
        transform.position = position;
        gameObject.SetActive(true);
    }
}
