using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject background;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;

    [Header("3D Inspect Settings")]
    public RawImage renderDisplay;
    public Camera inspectCamera;
    public Transform inspectPivot;
    public float rotationSpeed = 100f;
    public Image darkOverlay;
    public RenderTexture renderTexture;

    private float hoverLockUntil = 0f;
    public bool IsInspecting => isInspecting;

    private GameObject current3DObject;
    private bool isInspecting = false;

    private Vector3 initialPosition;

    private void Awake()
    {
        initialPosition = transform.position;
        Hide();
    }

    private void Update()
    {
        if (isInspecting)
        {
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CloseInspect3D();
                return;
            }

            if (Input.GetMouseButton(0) && current3DObject != null)
            {
                float rotX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
                float rotY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

                current3DObject.transform.Rotate(Vector3.up, -rotX, Space.World);
                current3DObject.transform.Rotate(Vector3.right, rotY, Space.World);
            }
        }

        // Si on est en mode Inspect et que le joueur clique droit → fermer tout
        if (isInspecting && Input.GetMouseButtonDown(1))
        {
            HideAll();
        }
    }

    public bool CanShowHover()
    {
        return !isInspecting && Time.time >= hoverLockUntil;
    }

    // ------------------------------
    // TOOLTIP CLASSIQUE
    // ------------------------------
    public void Show(ItemData item)
    {
        if (!CanShowHover()) return;          // bloque le hover si verrou
        gameObject.SetActive(true);

        if (item == null) return;

        if (itemNameText != null) itemNameText.text = item.itemName;
        if (itemDescriptionText != null) itemDescriptionText.text = item.description;

        if (background != null) background.SetActive(true);
        if (darkOverlay != null) darkOverlay.enabled = false;
        if (renderDisplay != null) renderDisplay.gameObject.SetActive(false);

        SetTextsVisible(true);
    }

    public void Hide()
    {
        if (background != null) background.SetActive(false);
        if (darkOverlay != null) darkOverlay.enabled = false;
        if (renderDisplay != null) renderDisplay.gameObject.SetActive(false);

        SetTextsVisible(false);
    }

    // ------------------------------
    // MODE INSPECTEUR 3D
    // ------------------------------
    public void ShowInspect3D(ItemData item)
    {
        gameObject.SetActive(true);
        isInspecting = true;

        if (item == null || item.worldPrefab == null)
        {
            Debug.LogWarning("[Inspect3D] Aucun prefab 3D trouvé pour cet item !");
            return;
        }

        isInspecting = true;

        if (background != null)
        {
            background.SetActive(true);
            var bgImage = background.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.enabled = true;
                bgImage.color = new Color(0, 0, 0, 0.6f); // noir semi-transparent
            }
        }

        if (renderDisplay != null)
        {
            renderDisplay.gameObject.SetActive(true);
            renderDisplay.color = Color.white;
        }

        if (inspectCamera == null)
        {
            Debug.LogError("[Inspect3D] Aucune caméra d'inspect assignée !");
            return;
        }
        else
        {
            // Réactive la caméra si elle avait été désactivée
            inspectCamera.enabled = true;
            inspectCamera.gameObject.SetActive(true);
        }

        // Crée la RenderTexture et relie tout
        if (renderTexture == null)
            renderTexture = new RenderTexture(1024, 1024, 16);

        inspectCamera.targetTexture = renderTexture;
        renderDisplay.texture = renderTexture;
        // Force un rendu immédiat (utile si la caméra vient d'être réactivée)
        inspectCamera.Render();

        // Nettoie ancien modèle
        foreach (Transform child in inspectPivot)
            Destroy(child.gameObject);

        // Instancie le modèle
        current3DObject = Instantiate(item.worldPrefab, inspectPivot);
        current3DObject.transform.localPosition = Vector3.zero;
        current3DObject.transform.localRotation = Quaternion.identity;

        foreach (var rb in current3DObject.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = true;
        foreach (var col in current3DObject.GetComponentsInChildren<Collider>())
            col.enabled = false;

        if (itemNameText != null) itemNameText.text = item.itemName;
        if (itemDescriptionText != null) itemDescriptionText.text = item.description;

        SetTextsVisible(true);

        // Fond noir visuel sûr
        if (background)
        {
            background.SetActive(true);
            var img = background.GetComponent<Image>();
            if (img) { img.enabled = true; img.color = new Color(0, 0, 0, 0.6f); }
        }

        if (renderDisplay)
        {
            renderDisplay.gameObject.SetActive(true);
            renderDisplay.color = Color.white;
        }
    }

    public void HideAll(float hoverLockDuration = 0.15f)
    {
        isInspecting = false;

        // Cache les textes
        SetTextsVisible(false);

        // Désactive les éléments visuels sans désactiver le GameObject principal
        if (renderDisplay) renderDisplay.gameObject.SetActive(false);
        if (inspectCamera)
        {
            inspectCamera.targetTexture = null;
            inspectCamera.enabled = false;
        }
        if (inspectPivot)
        {
            foreach (Transform child in inspectPivot)
                Destroy(child.gameObject);
        }
        if (renderTexture != null)
        {
            renderTexture.Release();
            renderTexture = null;
        }

        if (background)
        {
            var bgImage = background.GetComponent<Image>();
            if (bgImage) bgImage.enabled = false;
            background.SetActive(false);
        }

        if (darkOverlay) darkOverlay.enabled = false;

        // Verrou anti-hover pour éviter un flicker immédiat
        hoverLockUntil = Time.time + hoverLockDuration;

        // NE PAS désactiver le GameObject principal (gameObject.SetActive(false))
        // sinon plus rien ne fonctionne au prochain inspect
    }

    public void CloseInspect3D()
    {
        isInspecting = false;

        if (current3DObject != null)
            Destroy(current3DObject);

        darkOverlay.enabled = false;
        renderDisplay.gameObject.SetActive(false);

        SetTextsVisible(false);
    }

    private void SetTextsVisible(bool visible)
    {
        if (itemNameText)
        {
            itemNameText.enabled = visible;
            itemNameText.gameObject.SetActive(visible);
            if (!visible) itemNameText.text = "";
        }

        if (itemDescriptionText)
        {
            itemDescriptionText.enabled = visible;
            itemDescriptionText.gameObject.SetActive(visible);
            if (!visible) itemDescriptionText.text = "";
        }
    }
}
