using UnityEngine;

public class Climb2 : MonoBehaviour
{
    [Header("Sécurité")]
    public float maxClimbTime = 8f;
    private float climbTimer = 0f;

    [Header("Références")]
    public Transform playerCamera;
    public Rigidbody rb;

    [Header("Paramètres de grimpe")]
    public float forwardCheckDistance = 1.5f;   // distance devant le joueur
    public float climbSpeed = 2f;
    public float minClimbHeight = 0.2f;
    public float maxClimbHeight = 1.5f;

    [Header("Audio")]
    public AudioSource ClimbSound;

    public bool isClimbing = false;
    private bool climbingVertical = false;
    private bool climbingHorizontal = false;

    private Vector3 climbTarget;
    private Vector3 verticalTarget;
    private Vector3 horizontalTarget;

    void Update()
    {
        // déclenchement : Space + regarde un mur proche
        if (Input.GetKeyDown(KeyCode.Space) && !isClimbing)
        {
            TryClimb();
        }

        if (!isClimbing) return;

        climbTimer += Time.deltaTime;
        if (climbTimer >= maxClimbTime)
        {
            CancelClimb();
            return;
        }

        if (climbingVertical)
        {
            transform.position = Vector3.MoveTowards(transform.position, verticalTarget, climbSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, verticalTarget) < 0.05f)
            {
                climbingVertical = false;
                climbingHorizontal = true;
            }
        }
        else if (climbingHorizontal)
        {
            transform.position = Vector3.MoveTowards(transform.position, horizontalTarget, climbSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, horizontalTarget) < 0.05f)
            {
                CancelClimb();
            }
        }
    }

    void TryClimb()
    {
        // direction de regard horizontale
        Vector3 flatForward = playerCamera.forward;
        flatForward.y = 0f;
        flatForward.Normalize();

        // on tire un ray droit devant le joueur
        if (Physics.Raycast(playerCamera.position, flatForward, out RaycastHit wallHit, forwardCheckDistance))
        {
            if (wallHit.collider.CompareTag("Climbable"))
            {
                // maintenant on regarde vers le haut depuis le point d’impact
                Vector3 topCheckStart = wallHit.point + Vector3.up * maxClimbHeight;
                if (Physics.Raycast(topCheckStart, Vector3.down, out RaycastHit topHit, maxClimbHeight * 2f))
                {
                    float height = topHit.point.y - transform.position.y;
                    if (height > minClimbHeight && height <= maxClimbHeight)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                        rb.linearVelocity = Vector3.zero;

                        isClimbing = true;
                        climbingVertical = true;
                        climbingHorizontal = false;

                        Vector3 upOffset = Vector3.up * 1.2f;
                        Vector3 forwardOffset = flatForward * 0.4f;
                        climbTarget = topHit.point + upOffset - forwardOffset;

                        verticalTarget = new Vector3(transform.position.x, topHit.point.y + 1.2f, transform.position.z);
                        horizontalTarget = climbTarget;

                        climbTimer = 0f;

                        if (ClimbSound && !ClimbSound.isPlaying)
                            ClimbSound.Play();
                    }
                }
            }
        }
    }

    void CancelClimb()
    {
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;

        isClimbing = false;
        climbingVertical = false;
        climbingHorizontal = false;
        climbTimer = 0f;
    }
}
