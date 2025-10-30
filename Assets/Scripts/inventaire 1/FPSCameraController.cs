using UnityEngine;

public class FPSCameraController : MonoBehaviour
{
    [Header("Refs")]
    public Transform playerBody;
    public Transform cameraStand;
    public Transform cameraCrouch;

    [Header("Mouse")]
    public float sensitivity = 150f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Lean Settings")]
    public float leanSpeed = 10f;
    public float leanAngle = 15f;
    public float leanDistance = 0.3f;
    private float currentLean = 0f;
    private float targetLean = 0f;

    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        // 🔒 Gestion du curseur
        bool inventoryOpen = InventoryToggle.IsInventoryOpen;
        if (inventoryOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // 🎯 Mouvement souris
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        xRotation = Mathf.Clamp(xRotation - mouseY, minPitch, maxPitch);
        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);

        // 👇 Gestion du lean
        if (Input.GetKey(KeyCode.Q)) targetLean = -1f;
        else if (Input.GetKey(KeyCode.E)) targetLean = 1f;
        else targetLean = 0f;

        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSpeed);

        // === POSITION ===
        // ⚙️ Lean local seulement (pas de SetWorldPos)
        Vector3 targetLocalPos = new Vector3(currentLean * leanDistance, 0f, 0f);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * leanSpeed);

        // === ROTATION ===
        Quaternion pitch = Quaternion.Euler(xRotation, 0f, 0f);
        Quaternion roll = Quaternion.Euler(0f, 0f, -currentLean * leanAngle);
        transform.localRotation = pitch * roll;
    }
}
