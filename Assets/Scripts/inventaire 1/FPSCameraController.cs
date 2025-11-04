// FPSCameraController.cs
using UnityEngine;

public class FPSCameraController : MonoBehaviour
{
    [Header("Refs")]
    public Transform playerBody;
    public Transform cameraRoot;

    [Header("Mouse")]
    public float sensitivity = 150f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Lean")]
    public float leanSpeed = 10f;
    public float leanAngle = 15f;
    public float leanDistance = 0.3f;
    public float leanCollisionPadding = 0.05f;
    public LayerMask leanBlockMask = ~0;

    // === RECOIL CAM ===
    [Header("Recoil Camera")]
    public float camRecoilSnappiness = 8f;
    public float camRecoilReturn = 4f;
    public bool useKickOnCam = false;   // tu peux laisser false si tu utilises CameraCollisionResolver

    private Vector2 camRecoilTarget;    // (pitchUp, yawSide)
    private Vector2 camRecoilCurrent;
    private float kickTargetZ = 0f;     // négatif = recule
    private float kickCurrentZ = 0f;

    float xRotation;
    float currentLean, targetLean;
    public Vector3 lastRecoil; // (pitchUp, yawSide, kickBack)

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraRoot && playerBody)
        {
            var local = cameraRoot.localPosition;
            local.y = Mathf.Max(local.y, 1.5f);
            cameraRoot.localPosition = local;
        }
    }

    // Appelé par le GunSystem à chaque tir
    public void AddRecoil(float pitchUp, float yawSide, float kickBack)
    {
        // Note: xRotation baisse pour regarder vers le haut → on stocke le "pitchUp" tel quel
        camRecoilTarget += new Vector2(pitchUp, yawSide);
        if (useKickOnCam) kickTargetZ -= Mathf.Abs(kickBack);
    }

    // (optionnel) pour synchroniser les dynamiques avec celles du gun à l’équipement
    public void SetRecoilDynamics(float snappiness, float returnSpeed)
    {
        camRecoilSnappiness = snappiness;
        camRecoilReturn = returnSpeed;
    }

    void LateUpdate()
    {
        if (InventoryToggle.IsInventoryOpen) return;

        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        // pitch souris
        xRotation = Mathf.Clamp(xRotation - mouseY, minPitch, maxPitch);
        if (playerBody) playerBody.Rotate(Vector3.up * mouseX);

        // === RECOIL CAM === (damping)
        camRecoilTarget = Vector2.Lerp(camRecoilTarget, Vector2.zero, camRecoilReturn * Time.deltaTime);
        camRecoilCurrent = Vector2.Lerp(camRecoilCurrent, camRecoilTarget, camRecoilSnappiness * Time.deltaTime);

        if (useKickOnCam)
        {
            kickTargetZ = Mathf.Lerp(kickTargetZ, 0f, camRecoilReturn * Time.deltaTime);
            kickCurrentZ = Mathf.Lerp(kickCurrentZ, kickTargetZ, camRecoilSnappiness * Time.deltaTime);
        }
        else
        {
            kickCurrentZ = 0f; // tu laisses la collision caméra gérer le Z
        }

        // Lean input
        targetLean = Input.GetKey(KeyCode.Q) ? -1f :
                     Input.GetKey(KeyCode.E) ? 1f : 0f;
        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSpeed);

        // Collision lean (latéral + diagonale avant)
        float allowedLean = currentLean;
        if (Mathf.Abs(currentLean) > 0.01f)
        {
            float r = 0.15f;
            Vector3 dirRight = cameraRoot.right * currentLean;
            Vector3 dirDiag = (cameraRoot.right * currentLean + cameraRoot.forward * 0.4f).normalized;

            float minRatio = 1f;
            if (Physics.SphereCast(cameraRoot.position, r, dirRight, out var hit1, leanDistance + leanCollisionPadding, leanBlockMask))
            {
                float blocked = hit1.distance - leanCollisionPadding;
                minRatio = Mathf.Min(minRatio, Mathf.Clamp01(blocked / leanDistance));
            }
            if (Physics.SphereCast(cameraRoot.position, r, dirDiag, out var hit2, leanDistance + leanCollisionPadding, leanBlockMask))
            {
                float blocked = hit2.distance - leanCollisionPadding;
                minRatio = Mathf.Min(minRatio, Mathf.Clamp01(blocked / leanDistance));
            }
            allowedLean *= minRatio;
        }

        // Position locale (garde Y), ajoute éventuel kick Z
        Vector3 basePos = new Vector3(0f, cameraRoot.localPosition.y, cameraRoot.localPosition.z);
        Vector3 targetLocalPos = basePos + new Vector3(allowedLean * leanDistance, 0f, kickCurrentZ);
        cameraRoot.localPosition = Vector3.Lerp(cameraRoot.localPosition, targetLocalPos, Time.deltaTime * leanSpeed);

        // === ROTATION ===
        // pitch = souris + recoil-pitch ; yaw = recoil-yaw ; roll = lean
        Quaternion pitchQ = Quaternion.Euler(xRotation + (-camRecoilCurrent.x), 0f, 0f);
        // ⚠️ signe: -camRecoilCurrent.x car regarder vers le haut -> diminuer xRotation
        Quaternion yawQ = Quaternion.Euler(0f, camRecoilCurrent.y, 0f);
        Quaternion rollQ = Quaternion.Euler(0f, 0f, -allowedLean * leanAngle);

        transform.localRotation = pitchQ * yawQ * rollQ;
    }

    public Vector3 GenerateRecoil(float up, float side, float back, float mult = 1f)
    {
        float recoilX = Random.Range(up * 0.8f, up * 1.2f) * mult;
        float recoilY = Random.Range(-side, side) * mult;
        float recoilZ = back;

        camRecoilTarget += new Vector2(recoilX, recoilY);
        if (useKickOnCam) kickTargetZ -= Mathf.Abs(recoilZ);

        lastRecoil = new Vector3(recoilX, recoilY, recoilZ);
        return lastRecoil;
    }
}
