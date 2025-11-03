using UnityEngine;
using System.Collections;

public class MeleeWeapon : MonoBehaviour
{
    [Header("Références")]
    public EquipementManager equipementManager;
    public Camera playerCamera; // 👈 Ajoute ta caméra ici
    public LayerMask hitMask;

    private bool canAttack = true;
    public ItemData currentWeapon;

    void Update()
    {
        if (InventoryToggle.IsInventoryOpen) return;

        if (Input.GetMouseButtonDown(0))
            TryAttack();
    }

    void TryAttack()
    {
        currentWeapon = equipementManager?.GetEquippedMeleeWeapon();
        if (currentWeapon == null || !currentWeapon.isMeleeWeapon) return;

        if (!canAttack) return;
        StartCoroutine(AttackCoroutine());
    }

    IEnumerator AttackCoroutine()
    {
        canAttack = false;
        Debug.Log($"🗡 Attaque avec {currentWeapon.itemName}");
        PerformRaycastDamage();
        yield return new WaitForSeconds(1f / currentWeapon.attackSpeed);
        canAttack = true;
    }

    void PerformRaycastDamage()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("[Melee] Aucune caméra trouvée !");
                return;
            }
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.attackRange, hitMask))
        {
            BodyPart bodyPart = hit.collider.GetComponent<BodyPart>();
            HealthManager targetHealth = hit.collider.GetComponentInParent<HealthManager>();

            if (targetHealth == null)
            {
                Debug.Log($"[Melee] Touché {hit.collider.name}, pas de HealthManager");
                return;
            }

            float baseDamage = currentWeapon.meleeDamage;

            if (bodyPart != null)
            {
                Debug.Log($"[Melee] {bodyPart.partType} touché → base {baseDamage}");
                // ⚙️ on laisse BodyPart gérer le multiplicateur
                bodyPart.ApplyDamage(baseDamage, BulletType.Cal9mm);
            }
            else
            {
                Debug.Log($"[Melee] {hit.collider.name} sans BodyPart → {baseDamage}");
                targetHealth.TakeDamage(baseDamage);
            }

            // 👁 Debug visuel
            Debug.DrawLine(playerCamera.transform.position, hit.point, Color.red, 1f);
        }
        else
        {
            Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * currentWeapon.attackRange, Color.gray, 1f);
            Debug.Log("[Melee] Aucun contact (raycast)");
        }
    }
}
