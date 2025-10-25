using UnityEngine;

public class HeadBobController : MonoBehaviour
{
    [Header("Références")]
    public Transform weaponHolder;   // optionnel (l'arme suit un peu la cam)

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
    public float inputThreshold = 0.1f;   // 👉 aucune oscillation si input < seuil

    private Vector3 startPos;
    private float timer;
    private bool isMoving;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        // 🔹 Lis l'input pur (pas la physique) → fiable à l’arrêt
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        float inputMag = new Vector2(h, v).magnitude;

        // running ?
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && inputMag > inputThreshold;

        // active/désactive le bob uniquement selon l’input
        isMoving = inputMag > inputThreshold;

        if (!isMoving)
        {
            // stop net du cycle à l’arrêt
            timer = 0f;
            ResetPosition();
            return;
        }

        float bobSpeed = isRunning ? runBobSpeed : walkBobSpeed;
        float bobXAmount = isRunning ? runBobAmountX : walkBobAmountX;
        float bobYAmount = isRunning ? runBobAmountY : walkBobAmountY;

        // avance le cycle en fonction de l’intensité d’input (marche vs demi-marche)
        timer += Time.deltaTime * bobSpeed * Mathf.Clamp01(inputMag);

        // motif "∞"
        float bobX = Mathf.Cos(timer) * bobXAmount;
        float bobY = Mathf.Sin(timer * 2f) * bobYAmount;

        Vector3 targetPos = startPos + new Vector3(bobX, bobY, 0f);

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * smooth);

        if (weaponHolder)
            weaponHolder.localPosition = Vector3.Lerp(weaponHolder.localPosition, targetPos, Time.deltaTime * smooth * 0.5f);
    }

    private void ResetPosition()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, startPos, Time.deltaTime * idleResetSpeed);
        if (weaponHolder)
            weaponHolder.localPosition = Vector3.Lerp(weaponHolder.localPosition, startPos, Time.deltaTime * idleResetSpeed * 0.5f);
    }
}
