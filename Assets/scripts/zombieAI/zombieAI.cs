using UnityEngine;
using UnityEngine.AI;
using InfimaGames.LowPolyShooterPack;

public class ZombieAI : MonoBehaviour
{
    private Transform player;
    private NavMeshAgent agent;

    [SerializeField] private Animator animator;
    [SerializeField] private float detectRange = 15f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float stoppingDistance = 1.5f;

    private bool isAttacking;
    private bool isDead;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                Debug.LogError("Animator component is missing on " + gameObject.name);
        }
    }

    private void Start()
    {
        if (agent == null || animator == null)
        {
            Debug.LogError("Missing NavMeshAgent or Animator on " + gameObject.name);
            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("Zombie is not placed on a NavMesh!");
            enabled = false;
            return;
        }

        player = FindAnyObjectByType<CharacterBehaviour>()?.transform;

        if (player == null)
        {
            Debug.LogError("Player not found in the scene.");
            enabled = false;
            return;
        }

        agent.speed = moveSpeed;
        agent.stoppingDistance = stoppingDistance; // Ensures zombie stops at the right distance
        agent.updateRotation = true; // Allows smooth turning
    }

    private void Update()
    {
        if (player == null || agent == null || !agent.isOnNavMesh || isDead)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= detectRange)
        {
            if (distance > attackRange)
            {
                ChasePlayer();
            }
            else
            {
                AttackPlayer();
            }
        }
        else
        {
            Idle();
        }
    }

    private void ChasePlayer()
    {
        if (isAttacking || isDead) return;

        agent.isStopped = false;
        agent.SetDestination(player.position);

        animator.SetBool("isWalking", true);
        animator.SetBool("isAttacking", false);

        // Rotate zombie smoothly towards player
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Keep rotation on the Y-axis only
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }


    private void AttackPlayer()
    {
        if (isAttacking || isDead) return;

        isAttacking = true;
        agent.isStopped = true; // Stop movement
        agent.velocity = Vector3.zero; // Prevent sliding

        animator.SetBool("isWalking", false);
        animator.SetBool("isAttacking", true);

        Invoke(nameof(ResetAttack), attackCooldown);
    }

    private void ResetAttack()
    {
        isAttacking = false;
        animator.SetBool("isAttacking", false);

        if (!isDead)
        {
            agent.isStopped = false; // Resume movement
        }
    }

    private void Idle()
    {
        if (isDead) return;

        agent.ResetPath();
        animator.SetBool("isWalking", false);
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        agent.isStopped = true;

        animator.SetBool("isWalking", false);
        animator.SetBool("isAttacking", false);
        animator.SetBool("isDead", true);

        Destroy(gameObject, 5f);
    }
}
