using UnityEngine;

public class CameraCollisionResolver : MonoBehaviour
{
    public Transform cameraRoot;
    public float collisionRadius = 0.15f;
    public float maxDistance = 0.4f; // distance max du pivot
    public float smooth = 15f;
    public LayerMask mask = ~0;

    Vector3 desiredLocalPos;

    void LateUpdate()
    {
        if (!cameraRoot) return;

        // Position locale actuelle du root
        Vector3 local = cameraRoot.localPosition;

        // On veut juste corriger la profondeur (Z)
        float targetZ = -maxDistance;

        // Calcule la position monde de la caméra désirée
        Vector3 worldTarget = transform.TransformPoint(new Vector3(local.x, local.y, targetZ));
        Vector3 dir = (worldTarget - transform.position).normalized;

        // Test de collision
        if (Physics.SphereCast(transform.position, collisionRadius, dir, out var hit, maxDistance, mask))
        {
            float newZ = -(hit.distance - collisionRadius);
            targetZ = Mathf.Max(newZ, -0.05f); // ne jamais coller à 0
        }

        // Applique en gardant le lean (x) et la hauteur (y)
        Vector3 targetLocal = new Vector3(local.x, local.y, targetZ);
        cameraRoot.localPosition = Vector3.Lerp(cameraRoot.localPosition, targetLocal, Time.deltaTime * smooth);
    }
}
