using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class TopDownCharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f; // Speed of WASD movement

    [Header("Click-to-Move Settings")]
    public Camera cam;           // Camera used for Raycasting in click-to-move
    public NavMeshAgent agent;   // NavMeshAgent for Pathfinding

    [Header("Mining Settings")]
    public float miningRange = 1.5f;  // Distance player must be from mineral to mine it
    public float miningDelay = 0.5f;  // Time delay before mineral is destroyed (sync with animation)

    [Header("Effects")]
    public GameObject miningEffectPrefab; // Particle effect prefab spawned when mining

    // Components
    private Rigidbody rb;
    private Animator animator;

    // Movement control
    private Vector3 moveDirection;
    private string lastDirection = "Down"; // Last facing direction (used for animations & mining)
    private bool isMining = false;         // Whether player is currently mining

    // Locks for preventing diagonal movement
    private bool horizontalLocked = false;
    private bool verticalLocked = false;

    void Start()
    {
        // Get required components
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // Auto-assign main camera if not set
        if (cam == null)
            cam = Camera.main;

        // Auto-assign NavMeshAgent if not set
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        // Disable rotation & vertical updates for top-down movement
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    void Update()
    {
        if (!isMining)
        {
            // Handle WASD first
            HandleWASDMovement();

            // Handle click-to-move
            HandleClickToMove();

            // Move toward agent path if active
            FollowAgentPath();

            // Update animations
            UpdateAnimations();
        }

        HandleMining();
    }

    void FollowAgentPath()
    {
        if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance)
        {
            // Direction toward next navmesh corner
            Vector3 direction = (agent.nextPosition - transform.position).normalized;

            // Move Rigidbody in that direction
            rb.linearVelocity = direction * moveSpeed;

            // Update last direction for animations/mining
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
                lastDirection = direction.x > 0 ? "Right" : "Left";
            else
                lastDirection = direction.z > 0 ? "Up" : "Down";
        }
    }

    // -------------------------
    // WASD MOVEMENT
    // -------------------------
    void HandleWASDMovement()
    {
        // Get raw input (no smoothing)
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        // Axis lock to prevent diagonal movement
        if (moveX != 0 && !verticalLocked)
        {
            horizontalLocked = true;
            verticalLocked = false;
            moveZ = 0; // Disable vertical movement if moving horizontally
        }
        else if (moveZ != 0 && !horizontalLocked)
        {
            verticalLocked = true;
            horizontalLocked = false;
            moveX = 0; // Disable horizontal movement if moving vertically
        }

        // Reset lock when no movement
        if (moveX == 0 && moveZ == 0)
        {
            horizontalLocked = false;
            verticalLocked = false;
        }

        // Calculate movement direction
        moveDirection = new Vector3(moveX, 0f, moveZ).normalized;

        // Apply velocity for movement
        rb.linearVelocity = moveDirection * moveSpeed;

        // Cancel NavMeshAgent path if using WASD
        if (moveDirection.magnitude > 0)
        {
            agent.ResetPath();
        }

        // Store last movement direction for animation & mining
        if (moveX < 0) lastDirection = "Left";
        else if (moveX > 0) lastDirection = "Right";
        else if (moveZ > 0) lastDirection = "Up";
        else if (moveZ < 0) lastDirection = "Down";
    }

    // -------------------------
    // CLICK TO MOVE
    // -------------------------
    void HandleClickToMove()
    {
        if (Input.GetMouseButtonDown(1)) // Right click
        {
            // Raycast from camera to mouse position
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Set NavMeshAgent destination
                agent.SetDestination(hit.point);
            }
        }
    }

    // -------------------------
    // ANIMATIONS
    // -------------------------
    void UpdateAnimations()
    {
        // Reset all walk states before setting a new one
        animator.SetBool("Walk_Left", false);
        animator.SetBool("Walk_Right", false);
        animator.SetBool("Walk_Up", false);
        animator.SetBool("Walk_Down", false);

        // Default to Rigidbody velocity (WASD)
        Vector3 velocity = rb.linearVelocity;

        // If NavMeshAgent is moving, use its velocity instead
        if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance)
        {
            velocity = agent.velocity;
        }

        // Check if the character is moving
        bool isWalking = velocity.magnitude > 0.05f;

        // Set walk animation based on movement direction
        if (isWalking)
        {
            if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.z))
            {
                if (velocity.x > 0)
                {
                    animator.SetBool("Walk_Right", true);
                    lastDirection = "Right";
                }
                else
                {
                    animator.SetBool("Walk_Left", true);
                    lastDirection = "Left";
                }
            }
            else
            {
                if (velocity.z > 0)
                {
                    animator.SetBool("Walk_Up", true);
                    lastDirection = "Up";
                }
                else
                {
                    animator.SetBool("Walk_Down", true);
                    lastDirection = "Down";
                }
            }
        }
    }

    // -------------------------
    // MINING
    // -------------------------
    void HandleMining()
    {
        // Left click to mine (all 4 directions now)
        if (Input.GetMouseButtonDown(0) && !isMining)
        {
            if (lastDirection == "Down" || lastDirection == "Left" || lastDirection == "Right" || lastDirection == "Up")
            {
                StartCoroutine(MiningAction());
            }
        }
    }

    IEnumerator MiningAction()
    {
        isMining = true; // Block other actions while mining

        // Play correct mining animation based on last movement direction
        switch (lastDirection)
        {
            case "Left":
                animator.SetTrigger("Mine_Left");
                break;
            case "Right":
                animator.SetTrigger("Mine_Right");
                break;
            case "Down":
                animator.SetTrigger("Mine_Down");
                break;
            case "Up":
                animator.SetTrigger("Mine_Up"); // added this
                break;
        }

        // Find the closest object tagged "Mineable" within range
        Collider[] hits = Physics.OverlapSphere(transform.position, miningRange);
        GameObject target = null;
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Mineable"))
            {
                target = hit.gameObject;
                break;
            }
        }

        // Wait before destroying (to sync with animation hit)
        yield return new WaitForSeconds(miningDelay);

        if (target != null)
        {
            // Spawn mining particle effect at mineral position
            if (miningEffectPrefab != null)
            {
                GameObject effect = Instantiate(miningEffectPrefab, target.transform.position, Quaternion.identity);
                Destroy(effect, 2f); // Remove effect after 2 seconds
            }

            // Remove the mineral from the scene
            Destroy(target);
        }

        isMining = false; // Allow movement and other actions again
    }
}