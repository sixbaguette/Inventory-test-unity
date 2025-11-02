using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Parameters")]
    public float speed = 100f;
    public float damage = 20f;
    public float lifetime = 3f;
    public BulletType bulletType;

    private Vector3 dir;
    private Vector3 previousPosition;
    private Collider[] ignore;
    private float lifeTimer;

    // 🔧 Permet de changer la vitesse à chaud
    public void SetSpeed(float newSpeed) => speed = newSpeed;

    /// <summary>
    /// Initialise la balle (appelé depuis GunSystem)
    /// </summary>
    public void Initialize(Vector3 direction, float newSpeed, float newDamage, BulletType type, Collider[] ignoreColliders = null)
    {
#if UNITY_EDITOR
        Debug.Log($"[Bullet] Initialize speed = {newSpeed}");
#endif
        dir = direction.normalized;
        speed = newSpeed;
        damage = newDamage;
        bulletType = type;
        ignore = ignoreColliders;
        previousPosition = transform.position;
        lifeTimer = lifetime;
    }

    private void FixedUpdate()
    {
        // 💀 Durée de vie
        lifeTimer -= Time.fixedDeltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // 🧭 Déplacement constant
        float moveDistance = speed * Time.fixedDeltaTime;
        Vector3 nextPos = previousPosition + dir * moveDistance;

        // 🧱 Raycast pour détecter l'impact
        if (Physics.Raycast(previousPosition, dir, out RaycastHit hit, moveDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            // 🔒 Ignore certains colliders (ex : joueur ou arme)
            if (ignore != null)
            {
                foreach (var col in ignore)
                {
                    if (col != null && hit.collider == col)
                    {
                        previousPosition = nextPos;
                        transform.position = nextPos;
                        return;
                    }
                }
            }

            // 🎯 Application des dégâts
            var bodyPart = hit.collider.GetComponent<BodyPart>();
            if (bodyPart != null)
                bodyPart.ApplyDamage(damage, bulletType);

#if UNITY_EDITOR
            Debug.DrawLine(previousPosition, hit.point, Color.yellow, 0.2f);
            Debug.Log($"[Bullet] Hit {hit.collider.name} ({bulletType}, dmg={damage})");
#endif
            Destroy(gameObject);
            return;
        }

        // 🔁 Si pas d’impact → avancer
        transform.position = nextPos;
        previousPosition = nextPos;
    }
}
