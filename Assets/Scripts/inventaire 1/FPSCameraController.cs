using UnityEngine;

public class FPSCameraController : MonoBehaviour
{
    [Header("Refs")]
    public Transform playerBody;              // l’objet que tu fais tourner en yaw (a le Rigidbody)
    public Transform cameraStand;             // position tête debout
    public Transform cameraCrouch;            // position tête accroupi

    [Header("Mouse")]
    public float sensitivity = 150f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Lean")]
    public float leanSpeed = 10f;
    public float leanAngle = 15f;             // rotation Z
    private float currentLean = 0f;

    private float xRotation = 0f;

    // Réf crouch: on lit l’état directement via la hauteur réelle (ou expose un bool public si tu préfères)
    [Header("Crouch (lecture uniquement)")]
    public float crouchBlend = 0f;            // 0 = debout, 1 = accroupi (calculé à partir de la position cible)

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        // 🧭 Gestion propre du curseur (évite les micro-freeze)
        bool inventoryOpen = InventoryToggle.IsInventoryOpen;
        if (inventoryOpen && Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (!inventoryOpen && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Bloque la caméra si inventaire ouvert
        if (inventoryOpen)
            return;

        // === Mouvement souris ===
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        xRotation = Mathf.Clamp(xRotation - mouseY, minPitch, maxPitch);

        // Pitch sur la caméra uniquement
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Yaw sur le corps joueur
        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);

        // === Choix de la base (debout ou accroupi) ===
        Transform basePos = cameraStand;
        bool isCrouchKey = Input.GetKey(KeyCode.C);
        if (isCrouchKey && cameraCrouch != null)
            basePos = cameraCrouch;

        // === Lean cible ===
        float targetLean = 0f;
        if (Input.GetKey(KeyCode.Q))
        {
            if (!Physics.Raycast(transform.position, -transform.right, out _, 1f))
                targetLean = -1f;
        }
        if (Input.GetKey(KeyCode.E))
        {
            if (!Physics.Raycast(transform.position, transform.right, out _, 1f))
                targetLean = 1f;
        }

        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSpeed);

        // === Position caméra ===
        if (basePos != null)
            transform.position = basePos.position + basePos.right * currentLean;

        // === Rotation finale (pitch + roll) ===
        Quaternion roll = Quaternion.Euler(0f, 0f, -currentLean * leanAngle);
        transform.localRotation = transform.localRotation * roll;
    }
}
