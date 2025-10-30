using UnityEngine;

public class test : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            FindFirstObjectByType<HealthManager>().TakeDamage(5);
        }
    }
}
