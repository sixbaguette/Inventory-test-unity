using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Container : MonoBehaviour
{
    [Header("Paramètres du conteneur")]
    public string containerName = "Coffre";
    public int width = 6;
    public int height = 5;

    [Header("Items de départ")]
    public ItemData[] startingItems;

    private bool isOpen = false;

    private void Start()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    public void OpenContainer()
    {
        // Si le conteneur est déjà ouvert → on ignore (sauf si l’UI a été fermée manuellement)
        if (isOpen && ContainerUIController.Instance != null && ContainerUIController.Instance.IsContainerOpen)
        {
            Debug.Log("[Container] Déjà ouvert, on ignore.");
            return;
        }

        Debug.Log($"[Container] Ouverture du conteneur : {containerName}");

        isOpen = true;

        if (ContainerUIController.Instance != null)
        {
            ContainerUIController.Instance.OpenContainer(this);
        }
    }

    public void CloseContainer()
    {
        if (!isOpen) return;

        Debug.Log($"[Container] Fermeture du conteneur : {containerName}");

        isOpen = false;

        if (ContainerUIController.Instance != null)
        {
            ContainerUIController.Instance.CloseContainer();
        }
    }

    public void InitializeContents(ContainerInventoryManager inv)
    {
        if (startingItems == null || inv == null) return;

        foreach (var item in startingItems)
        {
            if (item != null)
                inv.AddItem(item);
        }
    }

    // ✅ Appelé automatiquement par le contrôleur UI quand il ferme
    public void OnUIClosed()
    {
        isOpen = false;
    }
}
