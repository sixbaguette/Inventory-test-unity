using UnityEngine;

public class WorldItem : MonoBehaviour
{
    [Header("Lien avec ScriptableObject")]
    public ItemData itemData; // ton scriptableObject assigné dans l’inspecteur

    // Appelé quand l’objet est ramassé
    public void OnPickedUp()
    {
        // Simplement désactive le GameObject 3D
        gameObject.SetActive(false);
    }

    // (Optionnel) pour le faire respawn
    public void SpawnAt(Vector3 position)
    {
        transform.position = position;
        gameObject.SetActive(true);
    }
}
