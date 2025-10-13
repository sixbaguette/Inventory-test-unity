using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;
    public Transform cameraTransform;

    private void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Rien pour l’instant — juste la référence
    }
}
