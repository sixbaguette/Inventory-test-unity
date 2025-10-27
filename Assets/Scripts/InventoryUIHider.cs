using System.Collections.Generic;
using UnityEngine;

public class InventoryUIHider : MonoBehaviour
{
    public static InventoryUIHider Instance;

    [Header("Objets à désactiver en mode conteneur")]
    public List<GameObject> objectsToHide = new List<GameObject>();

    private bool hidden = false;

    void Awake()
    {
        Instance = this;
    }

    public void HideForContainer()
    {
        if (hidden) return;
        hidden = true;

        foreach (var obj in objectsToHide)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    public void ShowAfterContainer()
    {
        if (!hidden) return;
        hidden = false;

        foreach (var obj in objectsToHide)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }
}
