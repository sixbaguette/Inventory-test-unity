using UnityEngine;

[DisallowMultipleComponent]
public class ItemHandOffset : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Transform de la main du joueur (trouvé automatiquement si vide)")]
    public Transform handSocket;

    [Header("Offsets d’alignement")]
    [Tooltip("Décalage de position appliqué à la main quand cet objet est équipé.")]
    public Vector3 handPositionOffset = Vector3.zero;

    [Tooltip("Décalage de rotation appliqué à la main quand cet objet est équipé.")]
    public Vector3 handRotationOffset = Vector3.zero;

    [Header("Transition")]
    [Tooltip("Vitesse de transition entre la position originale et l’offset (0 = instantané).")]
    public float transitionSpeed = 10f;

    private Vector3 originalHandPos;
    private Quaternion originalHandRot;
    private bool applied = false;

    void Awake()
    {
        //Trouve automatiquement le HandSocket dans la scène
        if (handSocket == null)
        {
            var found = GameObject.Find("HandSocket");
            if (found != null)
            {
                handSocket = found.transform;
            }
            else
            {
                Debug.LogWarning($"[{name}] Aucun HandSocket trouvé dans la scène !");
            }
        }
    }

    void OnEnable()
    {
        if (handSocket != null)
        {
            originalHandPos = handSocket.localPosition;
            originalHandRot = handSocket.localRotation;
            ApplyOffsetSmooth();
        }
    }

    void OnDisable()
    {
        if (handSocket != null)
        {
            StopAllCoroutines();
            ResetOffset();
        }
    }

    /// <summary>
    /// Applique instantanément l’offset à la main
    /// </summary>
    public void ApplyOffset()
    {
        if (handSocket == null) return;

        if (!applied)
        {
            originalHandPos = handSocket.localPosition;
            originalHandRot = handSocket.localRotation;
            applied = true;
        }

        handSocket.localPosition = originalHandPos + handPositionOffset;
        handSocket.localRotation = originalHandRot * Quaternion.Euler(handRotationOffset);
    }

    /// <summary>
    /// Applique l’offset en douceur avec une interpolation
    /// </summary>
    public void ApplyOffsetSmooth()
    {
        if (transitionSpeed <= 0f)
        {
            ApplyOffset();
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(LerpOffsetCoroutine());
        }
    }

    private System.Collections.IEnumerator LerpOffsetCoroutine()
    {
        Vector3 startPos = handSocket.localPosition;
        Quaternion startRot = handSocket.localRotation;

        Vector3 targetPos = originalHandPos + handPositionOffset;
        Quaternion targetRot = originalHandRot * Quaternion.Euler(handRotationOffset);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * transitionSpeed;
            handSocket.localPosition = Vector3.Lerp(startPos, targetPos, t);
            handSocket.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        applied = true;
    }

    /// <summary>
    /// Restaure la position/rotation originale de la main
    /// </summary>
    public void ResetOffset()
    {
        if (handSocket == null) return;

        handSocket.localPosition = originalHandPos;
        handSocket.localRotation = originalHandRot;
        applied = false;
    }
}
