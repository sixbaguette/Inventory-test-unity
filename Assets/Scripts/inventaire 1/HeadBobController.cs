using UnityEngine;

public class HeadBobController : MonoBehaviour
{
    [Header("Réglages marche")]
    public float walkBobSpeed = 6f;
    public float walkBobAmountX = 0.05f;
    public float walkBobAmountY = 0.05f;

    [Header("Réglages course")]
    public float runBobSpeed = 10f;
    public float runBobAmountX = 0.10f;
    public float runBobAmountY = 0.08f;

    [Header("Lissage & seuils")]
    public float smooth = 10f;
    public float idleResetSpeed = 6f;
    public float inputThreshold = 0.1f;

    [Header("Optionnel : arme")]
    public Transform weaponHolder;

    private Vector3 baseLocalPos;   // position locale de référence (celle que la caméra pose normalement)
    private Vector3 currentOffset;  // offset du headbob
    private float timer;
    private bool isMoving;

    void Start()
    {
        baseLocalPos = transform.localPosition;
    }

    void LateUpdate()
    {
        // 🔒 Bloque si inventaire ouvert
        if (InventoryToggle.IsInventoryOpen)
            return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        float inputMag = new Vector2(h, v).magnitude;
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && inputMag > inputThreshold;

        isMoving = inputMag > inputThreshold;

        if (!isMoving)
        {
            timer = 0f;
            currentOffset = Vector3.Lerp(currentOffset, Vector3.zero, Time.deltaTime * idleResetSpeed);
        }
        else
        {
            float bobSpeed = isRunning ? runBobSpeed : walkBobSpeed;
            float bobXAmount = isRunning ? runBobAmountX : walkBobAmountX;
            float bobYAmount = isRunning ? runBobAmountY : walkBobAmountY;

            timer += Time.deltaTime * bobSpeed * Mathf.Clamp01(inputMag);

            float bobX = Mathf.Cos(timer) * bobXAmount;
            float bobY = Mathf.Sin(timer * 2f) * bobYAmount;

            Vector3 targetOffset = new Vector3(bobX, bobY, 0f);
            currentOffset = Vector3.Lerp(currentOffset, targetOffset, Time.deltaTime * smooth);
        }

        // 🧩 Application ADDITIVE
        // On ne touche PAS à la position que le FPSCameraController a déjà posée cette frame.
        transform.localPosition = baseLocalPos + currentOffset;

        // Arme optionnelle
        if (weaponHolder)
        {
            weaponHolder.localPosition = Vector3.Lerp(
                weaponHolder.localPosition,
                baseLocalPos + currentOffset * 0.5f,
                Time.deltaTime * smooth
            );
        }
    }
}
