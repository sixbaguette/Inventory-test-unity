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
        if (InventoryToggle.IsInventoryOpen) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        float inputMag = new Vector2(h, v).magnitude;
        bool run = Input.GetKey(KeyCode.LeftShift) && inputMag > inputThreshold;

        bool moving = inputMag > inputThreshold;
        if (!moving)
        {
            timer = 0f;
            currentOffset = Vector3.Lerp(currentOffset, Vector3.zero, Time.deltaTime * idleResetSpeed);
        }
        else
        {
            float s = run ? runBobSpeed : walkBobSpeed;
            float ax = run ? runBobAmountX : walkBobAmountX;
            float ay = run ? runBobAmountY : walkBobAmountY;
            timer += Time.deltaTime * s * Mathf.Clamp01(inputMag);

            Vector3 target = new Vector3(Mathf.Cos(timer) * ax, Mathf.Sin(timer * 2f) * ay, 0f);
            currentOffset = Vector3.Lerp(currentOffset, target, Time.deltaTime * smooth);
        }

        transform.localPosition = currentOffset;
    }
}
