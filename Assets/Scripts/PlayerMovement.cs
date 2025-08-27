using UnityEngine;
using System.Collections; // for IEnumerator / Coroutine

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Rigidbody rb;
    public Animator animator;

    Vector3 movement;
    Vector3 lastMoveDirection;

    [Header("Attack Settings")]
    public float attackRange = 1f;
    public float destroyDelay = 0.2f; 
    public GameObject destroyParticlePrefab; 

    private void Update()
    {
        FixedMovement();

        // Movement Animation
        animator.SetFloat("Horizontal", movement.x);
        animator.SetFloat("Vertical", movement.z);
        animator.SetFloat("Speed", movement.sqrMagnitude);

        //Attacking
        if (Input.GetMouseButton(0))
        {
            Attack();
        }
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    private void FixedMovement()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        //To prevent diagonal movement
        if (Mathf.Abs(inputX) > 0.1f)
        {
            movement.x = inputX;
            movement.z = 0f;
        }
        else if (Mathf.Abs(inputZ) > 0.1f)
        {
            movement.x = 0f;
            movement.z = inputZ;
        }
        else
        {
            movement = Vector3.zero;
        }

        if (movement != Vector3.zero)
        {
            lastMoveDirection = movement;
        }

    }

    private void Attack()
    {
        Vector3 attackDir = lastMoveDirection.normalized;
        if (attackDir == Vector3.zero)
            attackDir = Vector3.back;

        //attack animation BlendTree
        animator.SetFloat("AttackX", attackDir.x);
        animator.SetFloat("AttackZ", attackDir.z);
        animator.SetTrigger("Attack");
    }

    //this will be called as an animation event, at the end of each animation attack
    public void PerformAttackHit()
    {
        Vector3 attackDir = new Vector3
        (
            Mathf.Round(lastMoveDirection.x),
            0f,
            Mathf.Round(lastMoveDirection.z)

        ).normalized;

        if (attackDir == Vector3.zero)
            attackDir = Vector3.back;

        // Check in the facing direction
        RaycastHit hit;
        if (Physics.Raycast(transform.position, attackDir, out hit, attackRange))
        {
            if (hit.collider.CompareTag("Mineable"))
            {
                StartCoroutine(DestroyWithEffect(hit.collider.gameObject));
            }
        }
    }

    private IEnumerator DestroyWithEffect(GameObject target)
    {
        yield return new WaitForSeconds(destroyDelay);

        if (destroyParticlePrefab != null)
        {
            Instantiate(destroyParticlePrefab, target.transform.position, Quaternion.identity);
        }

        Destroy(target);
    }
}
