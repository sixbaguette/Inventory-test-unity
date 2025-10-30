using UnityEngine;

public class PlayerContainerInteractor : MonoBehaviour
{
    public float interactDistance = 3f;
    public LayerMask interactMask;
    public KeyCode interactKey = KeyCode.E;

    private Camera cam;
    private Container lastTarget;

    void Start()
    {
        cam = GetComponentInChildren<Camera>();
        if (cam == null)
            Debug.LogError("[Interactor] ❌ Aucune caméra trouvée !");
        else
            Debug.Log("[Interactor] ✅ Caméra trouvée : " + cam.name);
    }

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            Debug.Log("[Interactor] Touche E pressée.");

            // Si un conteneur est déjà ouvert → refermer le conteneur ET l’inventaire complet
            if (ContainerUIController.Instance != null && ContainerUIController.Instance.IsContainerOpen)
            {
                Debug.Log("[Interactor] Fermeture du conteneur + inventaire joueur...");

                // Ferme le conteneur
                ContainerUIController.Instance.CloseContainer();

                // Ferme aussi l'inventaire joueur si ouvert
                var toggle = FindFirstObjectByType<InventoryToggle>();
                if (toggle != null && InventoryToggle.IsInventoryOpen)
                {
                    toggle.ToggleInventory(); // agit comme "fermer"
                }

                return;
            }

            // Sinon, on tente d’en ouvrir un nouveau
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask))
            {
                Debug.Log("[Interactor] Raycast a touché : " + hit.collider.name);

                Container cont = hit.collider.GetComponent<Container>();
                if (cont != null)
                {
                    Debug.Log("[Interactor] Conteneur trouvé → ouverture !");
                    cont.OpenContainer();
                    lastTarget = cont;
                }
                else
                {
                    Debug.Log("[Interactor] Pas de script Container sur l’objet touché.");
                }
            }
            else
            {
                Debug.Log("[Interactor] Rien touché à distance " + interactDistance);
            }
        }
    }
}
