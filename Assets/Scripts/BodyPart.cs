using UnityEngine;

public class BodyPart : MonoBehaviour
{
    public enum PartType { Head, Torso, Arm, Leg }
    public PartType partType;

    private HealthManager healthManager;

    void Awake()
    {
        healthManager = GetComponentInParent<HealthManager>();
    }

    public void ApplyDamage(float baseDamage, BulletType bulletType)
    {
        if (healthManager == null) return;

        float multiplier = partType switch
        {
            PartType.Head => 2f,
            PartType.Torso => 1.2f,
            PartType.Arm => 0.75f,
            PartType.Leg => 0.6f,
            _ => 1f
        };

        float finalDamage = baseDamage * multiplier;
        healthManager.TakeDamage(finalDamage);
    }
}
