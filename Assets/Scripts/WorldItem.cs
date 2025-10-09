using UnityEngine;

public class WorldItem : MonoBehaviour
{
    [Header("Lien avec ScriptableObject")]
    public ItemData itemData; // ton scriptableObject assign� dans l�inspecteur

    // Appel� quand l�objet est ramass�
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
