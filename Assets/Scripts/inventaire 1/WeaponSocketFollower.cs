using UnityEngine;

public class WeaponSocketFollower : MonoBehaviour
{
    [Header("Références")]
    public Transform cameraTransform;   // la caméra FPS
    public Transform playerRoot;        // (optionnel) le root/torse du joueur pour clamp hauteur

    [Header("Placement relatif à la caméra")]
    public Vector3 offsetLocal = new Vector3(0.25f, -0.18f, 0.45f); // x= droite/gauche, y= haut/bas, z= avant/arrière

    [Header("Lissage (facultatif)")]
    public float posLerp = 20f;
    public float rotLerp = 20f;

    [Header("Options")]
    public bool ignoreHeadBobPosition = true; // si true, on neutralise les translations de la caméra (headbob)
    public Transform referenceForPosition;     // si défini et ignoreHeadBobPosition=true : on prend sa position de base

    void Reset()
    {
        cameraTransform = Camera.main ? Camera.main.transform : null;
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Orientation = exactement celle de la caméra (pitch + yaw)
        Quaternion targetRot = cameraTransform.rotation;

        // Position : à partir de la caméra + offset dans l'espace caméra
        Vector3 basePos = cameraTransform.position;

        // Si on veut ignorer le headbob (la cam bouge en Y) on peut prendre une ref stable (ex: tête du joueur)
        if (ignoreHeadBobPosition && referenceForPosition != null)
            basePos = referenceForPosition.position;

        Vector3 targetPos = basePos + cameraTransform.TransformDirection(offsetLocal);

        // Applique en douceur
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotLerp);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * posLerp);
    }
}
