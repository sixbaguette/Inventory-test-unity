using System.Collections.Generic;
using UnityEngine;

public class InventoryUIHider : MonoBehaviour
{
    public static InventoryUIHider Instance;

    [Header("Objets à masquer visuellement en mode conteneur")]
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
            if (obj == null) continue;

            // 🟡 On garde l’objet actif, mais on le rend invisible et non interactif
            CanvasGroup cg = obj.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = obj.AddComponent<CanvasGroup>();

            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
    }

    public void ShowAfterContainer()
    {
        if (!hidden) return;
        hidden = false;

        foreach (var obj in objectsToHide)
        {
            if (obj == null) continue;

            CanvasGroup cg = obj.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }
    }
}
