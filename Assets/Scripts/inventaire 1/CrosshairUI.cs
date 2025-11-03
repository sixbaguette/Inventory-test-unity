using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    public static CrosshairUI Instance;

    [Header("UI Elements")]
    public Image crosshairImage;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color targetColor = Color.red;

    [Header("Detection")]
    public float detectionRange = 3f;
    public LayerMask detectionMask;

    private Camera cam;

    private void Awake()
    {
        Instance = this;
        cam = Camera.main;
    }

    private void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || crosshairImage == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        bool hasTarget = Physics.Raycast(ray, out RaycastHit hit, detectionRange, detectionMask);

        crosshairImage.color = hasTarget ? targetColor : normalColor;

        // === 🔕 Cacher le viseur si inventaire ou conteneur ouvert ===
        bool anyInventoryOpen = InventoryToggle.IsInventoryOpen
            || (ContainerUIController.Instance != null && ContainerUIController.Instance.IsContainerOpen);

        SetVisible(!anyInventoryOpen);
    }

    public void SetVisible(bool visible)
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        LeanTween.cancel(gameObject);
        LeanTween.value(gameObject, cg.alpha, visible ? 1f : 0f, 0.15f)
            .setOnUpdate((float v) => cg.alpha = v)
            .setEaseOutCubic();

        cg.blocksRaycasts = visible;
    }
}
