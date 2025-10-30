using UnityEngine;

public class test : MonoBehaviour
{
    private HealthManager playerHealth;

    void Start()
    {
        playerHealth = FindFirstObjectByType<HealthManager>();
        if (playerHealth == null)
            Debug.LogError("Aucun HealthManager trouvé sur le joueur !");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N) && playerHealth != null)
        {
            playerHealth.TakeDamage(5);
        }
    }
}
