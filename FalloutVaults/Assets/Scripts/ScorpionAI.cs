using System.Collections;
using UnityEngine;

public class ScorpionAI : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;

    public Transform pointA; // Patrol start position
    public Transform pointB; // Patrol end position
    public float AttackDamage = 20f;
    public float patrolSpeed = 2f;
    public float idleTime = 2f;
    public float chaseSpeed = 4f;
    public float detectionRange = 5f; // Distance to detect player
    public float maxChaseDistance = 6f;
    public float attackRange = 1.2f;
    public float attackCooldown = 2f;

    private Animator animator;
    private Transform player;
    private Vector3 initialPosition;
    private bool movingToB = true;
    private bool isChasing = false;
    private bool isAttacking = false;
    private bool canAttack = true;

    // 🛠 Debug Mode (Enable this for visual debugging)
    public bool debugMode = true;

    private void Start()
    {
        animator = GetComponent<Animator>();

        if (playerController == null)
        {
            Debug.LogError("❌ ERROR: PlayerController is NOT assigned in Inspector!");
        }
        else
        {
            player = playerController.transform; // Assign player transform
        }

        initialPosition = transform.position;
        StartCoroutine(Patrol());
    }

    private void Update()
    {
        if (player == null || playerController.IsDead)
        {
            if (isChasing || isAttacking)
            {
                Debug.Log("❌ Player is dead. Stopping all actions.");
                isChasing = false;
                isAttacking = false;
                StopAllCoroutines();
                StartCoroutine(Patrol());
            }
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float distanceFromStart = Vector3.Distance(transform.position, initialPosition);

        // ✅ Stop chasing if player is dead
        if (!isChasing && distanceToPlayer < detectionRange && distanceFromStart < maxChaseDistance && !isAttacking)
        {
            StopAllCoroutines();
            isChasing = true;
            StartCoroutine(Chase());
        }

        if (isChasing && (distanceFromStart >= maxChaseDistance || distanceToPlayer > detectionRange * 1.5f))
        {
            if (!isAttacking)
            {
                isChasing = false;
                StopAllCoroutines();
                StartCoroutine(Patrol());
            }
        }

        if (isChasing && distanceToPlayer < attackRange && canAttack && !isAttacking)
        {
            StopAllCoroutines();
            StartCoroutine(Attack());
        }
    }

    private IEnumerator Patrol()
    {
        while (!isChasing)
        {
            animator.Play("Walk");
            Vector3 target = movingToB ? pointB.position : pointA.position;

            target.y = transform.position.y;

            while (Vector3.Distance(transform.position, target) > 0.1f)
            {
                if (isChasing) yield break;
                transform.position = Vector3.MoveTowards(transform.position, target, patrolSpeed * Time.deltaTime);

                Vector3 lookDirection = target - transform.position;
                lookDirection.y = 0;
                if (lookDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(lookDirection);
                }

                yield return null;
            }

            animator.Play("Idle");
            yield return new WaitForSeconds(idleTime);

            movingToB = !movingToB;
        }
    }

    private IEnumerator Chase()
    {
        animator.Play("Walk");

        while (isChasing)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // **Stop chasing if player is out of detection + buffer**
            if (distanceToPlayer > detectionRange * 1.5f)
            {
                isChasing = false;
                StartCoroutine(Patrol());
                yield break;
            }

            // **Only stop when in attack range**
            if (distanceToPlayer > attackRange - 0.5f) // Buffer added
            {
                Vector3 targetPosition = new Vector3(player.position.x, transform.position.y, player.position.z);
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, chaseSpeed * Time.deltaTime);

                Vector3 lookDirection = player.position - transform.position;
                lookDirection.y = 0;
                if (lookDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(lookDirection);
                }
            }

            yield return null;
        }
    }

    private IEnumerator Attack()
    {
        // ✅ Check if player is dead before attacking
        if (playerController == null || playerController.IsDead)
        {
            Debug.Log("❌ Player is dead! Stopping attack.");
            isChasing = false;
            isAttacking = false;
            StartCoroutine(Patrol());
            yield break;  // 🚀 Exit attack immediately
        }

        isAttacking = true;
        canAttack = false;

        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");

        Debug.Log("⚔️ Scorpion is Attacking!");

        yield return new WaitForSeconds(0.5f);

        // ✅ Check again if player is dead before dealing damage
        if (playerController != null && !playerController.IsDead && Vector3.Distance(transform.position, player.position) < attackRange)
        {
            Debug.Log("💥 Scorpion Dealing Damage!");
            playerController.TakeDamage(AttackDamage);
        }
        else
        {
            Debug.Log("❌ Player moved out of attack range or died.");
        }

        yield return new WaitForSeconds(attackCooldown);

        animator.ResetTrigger("Attack");

        isAttacking = false;
        canAttack = true;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (playerController != null && !playerController.IsDead)
        {
            if (distanceToPlayer < attackRange)
            {
                Debug.Log("⚔️ Scorpion Attacking Again!");
                StartCoroutine(Attack());
            }
            else if (distanceToPlayer < detectionRange)
            {
                Debug.Log("🔄 Resuming Chase!");
                isChasing = true;
                StartCoroutine(Chase());
            }
            else
            {
                Debug.Log("🔄 Returning to Patrol.");
                isChasing = false;
                StartCoroutine(Patrol());
            }
        }
        else
        {
            Debug.Log("❌ Player is dead. Stopping combat.");
            isChasing = false;
            isAttacking = false;
            StartCoroutine(Patrol());
        }
    }

    // 🔹 **Debug Visualization using Gizmos**
    private void OnDrawGizmos()
    {
        if (!debugMode) return;

        // **Detection Range**
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // **Attack Range**
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // **Max Chase Distance (Color changes based on state)**
        Gizmos.color = isChasing ? Color.green : Color.blue;
        Gizmos.DrawWireSphere(transform.position, maxChaseDistance + 1f);
    }

}
