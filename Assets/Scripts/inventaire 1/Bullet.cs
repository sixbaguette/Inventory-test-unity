using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 100f;
    public float damage = 20f;
    public float lifetime = 3f;
    public BulletType bulletType;

    private Vector3 previousPosition;
    private float maxDistancePerFrame;

    void Start()
    {
        previousPosition = transform.position;
        maxDistancePerFrame = speed * Time.fixedDeltaTime * 2f; // marge de sécurité
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        Vector3 currentPosition = transform.position;
        Vector3 moveDir = transform.forward;
        float moveDistance = speed * Time.deltaTime;

        // 🔦 Raycast entre la position précédente et la future
        if (Physics.Raycast(previousPosition, moveDir, out RaycastHit hit, moveDistance + 0.05f))
        {
            // Ignore self / bullet
            if (hit.collider.GetComponent<Bullet>()) return;
            if (hit.collider.isTrigger) return;

            BodyPart bodyPart = hit.collider.GetComponent<BodyPart>();
            if (bodyPart != null)
            {
                bodyPart.ApplyDamage(damage, bulletType);
                Debug.DrawRay(previousPosition, moveDir * moveDistance, Color.green, 0.3f);
            }
            else
            {
                Debug.DrawRay(previousPosition, moveDir * moveDistance, Color.red, 0.3f);
                Debug.Log($"Bullet hit {hit.collider.name}");
            }

            Destroy(gameObject);
            return;
        }

        // 🔁 Mouvement continu
        transform.position += moveDir * moveDistance;
        previousPosition = transform.position;
    }
}
