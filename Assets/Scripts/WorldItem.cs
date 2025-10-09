using UnityEngine;

public class WorldItem : MonoBehaviour
{
    [Header("Lien avec ScriptableObject")]
    public ItemData itemData; // ton scriptableObject assign� dans l�inspecteur

    // Appel� quand l�objet est ramass�
    public void OnPickedUp()
    {
        // Simplement d�sactive le GameObject 3D
        gameObject.SetActive(false);
    }

    // (Optionnel) pour le faire respawn
    public void SpawnAt(Vector3 position)
    {
        transform.position = position;
        gameObject.SetActive(true);
    }
}
