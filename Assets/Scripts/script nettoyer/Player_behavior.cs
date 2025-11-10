using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerBehavior : MonoBehaviour
{
    [Header("Références")]
    public Transform orientation;           // Référence à la rotation du joueur
    private Climb2 climb;
    private Rigidbody rb;

    [Header("Mouvements")]
    public float baseSpeed = 4f;
    public float sprintBonus = 3f;
    public float crouchSpeedDebuff = 1.5f;
    private bool crouched = false;

    [Header("Saut")]
    public float jumpForce = 5f;
    public bool touchGround = false;
    public float groundCheckDistance = 1.1f;
    public LayerMask groundMask;

    [Header("Crouch")]
    public float crouchHeight = 0.5f;
    public float standHeight = 1f;
    public float crouchSmooth = 6f;
    private Vector3 targetScale;

    [Header("Audio")]
    public AudioSource footstepSound;
    public float walkStepInterval = 0.5f;
    public float runStepInterval = 0.25f;
    private float stepTimer = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        climb = GetComponent<Climb2>();
        targetScale = Vector3.one * standHeight;
    }

    private void FixedUpdate()
    {
        GroundCheck();
    }

    private void Update()
    {
        // Bloque toute action si inventaire ouvert
        if (InventoryToggle.IsInventoryOpen)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // Si on grimpe → ne pas bouger par le script de déplacement
        if (climb != null && climb.isClimbing)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        HandleMovement();
        HandleJump();
        HandleCrouch();
        HandleFootsteps();
    }

    private void GroundCheck()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        touchGround = Physics.Raycast(origin, Vector3.down, groundCheckDistance, groundMask);
        Debug.DrawRay(origin, Vector3.down * groundCheckDistance, touchGround ? Color.green : Color.red);
    }

    private void HandleMovement()
    {
        // Si on grimpe → on ne fait rien ici (évite les erreurs et glissements)
        if (climb != null && climb.isClimbing)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 moveDir = Vector3.zero;

        // Lecture des touches directionnelles
        if (Input.GetKey(KeyCode.W)) moveDir += orientation.forward;
        if (Input.GetKey(KeyCode.S)) moveDir -= orientation.forward;
        if (Input.GetKey(KeyCode.A)) moveDir -= orientation.right;
        if (Input.GetKey(KeyCode.D)) moveDir += orientation.right;

        // Normalisation pour éviter d’aller plus vite en diagonale
        if (moveDir != Vector3.zero)
            moveDir.Normalize();

        // Calcul de la vitesse actuelle
        float speed = baseSpeed;

        if (Input.GetKey(KeyCode.LeftShift) && !crouched)
            speed += sprintBonus;

        if (crouched)
            speed -= crouchSpeedDebuff;

        // Applique la vitesse horizontale uniquement (on garde la gravité sur Y)
        Vector3 targetVelocity = moveDir * speed;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }

    private void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && touchGround && !crouched)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            crouched = !crouched;
            targetScale = Vector3.one * (crouched ? crouchHeight : standHeight);
        }

        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * crouchSmooth);
    }

    private void HandleFootsteps()
    {
        if (!touchGround) return;

        bool isMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                        Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

        if (!isMoving)
        {
            stepTimer = 0f;
            return;
        }

        float currentInterval = (Input.GetKey(KeyCode.LeftShift) && !crouched) ? runStepInterval : walkStepInterval;
        stepTimer += Time.deltaTime;

        if (stepTimer >= currentInterval)
        {
            if (footstepSound != null)
                footstepSound.Play();
            stepTimer = 0f;
        }
    }
}
