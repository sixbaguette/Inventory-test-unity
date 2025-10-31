using UnityEngine;

public class test : MonoBehaviour
{
    private ArmorManager armorManager;
    private HealthManager playerHealth;

    void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[test] Aucun GameObject tagué 'Player' !");
            return;
        }

        armorManager = player.GetComponentInChildren<ArmorManager>();
        playerHealth = player.GetComponentInChildren<HealthManager>();

        if (armorManager == null)
            Debug.LogError("[test] Aucun ArmorManager trouvé sur le Player !");
        if (playerHealth == null)
            Debug.LogError("[test] Aucun HealthManager trouvé sur le Player !");
    }

    void Update()
    {
        if (playerHealth == null) return;

        // 💥 Tête
        if (Input.GetKeyDown(KeyCode.N))
        {
            Debug.Log("→ Dégâts appliqués sur la TÊTE");
            if (armorManager != null)
                armorManager.ApplyLocalizedDamage(50, BodyPart.PartType.Head);
            else
                playerHealth.TakeDamage(50);
        }

        // 💥 Torse
        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("→ Dégâts appliqués sur le TORSE");
            if (armorManager != null)
                armorManager.ApplyLocalizedDamage(25, BodyPart.PartType.Torso);
            else
                playerHealth.TakeDamage(25);
        }

        // 💥 Jambes
        if (Input.GetKeyDown(KeyCode.V))
        {
            Debug.Log("→ Dégâts appliqués sur les JAMBES");
            if (armorManager != null)
                armorManager.ApplyLocalizedDamage(20, BodyPart.PartType.Legs);
            else
                playerHealth.TakeDamage(20);
        }
    }
}
