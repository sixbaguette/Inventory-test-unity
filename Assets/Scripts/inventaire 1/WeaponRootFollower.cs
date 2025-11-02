using UnityEngine;

public class WeaponRootFollower : MonoBehaviour
{
    [Header("Références")]
    public Transform cameraTransform;
    public Vector3 offsetLocal = Vector3.zero;

    void LateUpdate()
    {
        if (!cameraTransform) return;

        transform.SetPositionAndRotation(
            cameraTransform.TransformPoint(offsetLocal),
            cameraTransform.rotation
        );
    }
}
