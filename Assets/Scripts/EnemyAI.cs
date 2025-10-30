using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack }

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 2.5f;
    [SerializeField] private bool faceMovement = true;

    [Header("Detection")]
    [SerializeField, Min(0f)] private float detectRange = 6f;
    [SerializeField, Min(0f)] private float attackRange = 1.25f;
    [SerializeField, Min(0f)] private float losePlayerTime = 1.5f;

    [Header("Patrol")]
    [SerializeField] private Transform patrolPointA;
    [SerializeField] private Transform patrolPointB;
    [SerializeField, Min(0.05f)] private float patrolArriveThreshold = 0.15f;
    [SerializeField] private bool startIdle = false;

    [Header("Attack")]
    [SerializeField, Min(0.1f)] private float attackCooldown = 0.75f;
    [SerializeField, Min(0f)] private float attackDamageDelay = 0.3f; // Delay before damage is applied
    [SerializeField, Min(1)] private int attackDamage = 10;

    [Header("Visual Feedback (Optional)")]
    [SerializeField] private SpriteRenderer sr; // Used for color feedback based on current state
    [SerializeField] private Color idleColor = new Color(0.8f, 0.8f, 0.8f);
    [SerializeField] private Color patrolColor = new Color(0.4f, 0.8f, 0.4f);
    [SerializeField] private Color chaseColor = new Color(0.9f, 0.7f, 0.3f);
    [SerializeField] private Color attackColor = new Color(0.9f, 0.4f, 0.4f);

    private Rigidbody2D rb;
    private PlayerHealth playerHealth;
    private EnemyHealth enemyHealth;
    private Animator anim;

    private State state;
    private Transform currentPatrolTarget;
    private float lastSeenTimer;   // Time since player was last seen
    private float lastAttackTime;  // Time since last attack

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyHealth = GetComponent<EnemyHealth>();
        anim = GetComponent<Animator>();
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        if (player) playerHealth = player.GetComponent<PlayerHealth>();

        currentPatrolTarget = patrolPointA ? patrolPointA : transform;

        state = startIdle ? State.Idle : State.Patrol;
        ApplyStateVisual();
        UpdateAnimator();
    }

    private void Update()
    {
        // State transition logic
        bool hasPlayer = player != null;
        float distToPlayer = hasPlayer ? Vector2.Distance(transform.position, player.position) : float.MaxValue;
        bool playerInDetect = hasPlayer && distToPlayer <= detectRange;
        bool playerInAttack = hasPlayer && distToPlayer <= attackRange;

        switch (state)
        {
            case State.Idle:
                if (playerInDetect) ChangeState(State.Chase);
                else if (patrolPointA && patrolPointB) ChangeState(State.Patrol);
                break;

            case State.Patrol:
                if (playerInDetect) ChangeState(State.Chase);
                break;

            case State.Chase:
                if (playerInAttack) ChangeState(State.Attack);
                else if (!playerInDetect)
                {
                    // Player escaped; after some time, return to patrol
                    lastSeenTimer += Time.deltaTime;
                    if (lastSeenTimer >= losePlayerTime)
                    {
                        lastSeenTimer = 0f;
                        ChangeState(patrolPointA && patrolPointB ? State.Patrol : State.Idle);
                    }
                }
                else
                {
                    lastSeenTimer = 0f;
                }
                break;

            case State.Attack:
                if (!playerInAttack)
                {
                    // Player left attack range; chase again or return to patrol
                    ChangeState(playerInDetect ? State.Chase : (patrolPointA && patrolPointB ? State.Patrol : State.Idle));
                }
                break;
        }
    }

    private void FixedUpdate()
    {
        // Don't move if in hit reaction (knockback + animation playing simultaneously)
        if (enemyHealth != null && enemyHealth.IsInHitReaction)
        {
            // During knockback, let the physics play out
            if (enemyHealth.IsInKnockback)
            {
                // Don't interfere with knockback velocity
                return;
            }
            // If animation is still playing but knockback ended, stop movement
            else
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }
        }

        // Don't move if stunned (after hit reaction ends)
        if (enemyHealth != null && enemyHealth.IsStunned)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        switch (state)
        {
            case State.Idle:
                rb.linearVelocity = Vector2.zero;
                break;

            case State.Patrol:
                PatrolMove();
                break;

            case State.Chase:
                ChaseMove();
                break;

            case State.Attack:
                rb.linearVelocity = Vector2.zero;
                TryAttack();
                break;
        }

        // Update animator based on movement
        UpdateAnimator();

        // Flip sprite based on movement direction
        if (faceMovement && rb.linearVelocity.sqrMagnitude > 0.0001f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(rb.linearVelocity.x == 0 ? scale.x : rb.linearVelocity.x);
            transform.localScale = scale;
        }

        if (state != State.Attack)
        {
            UpdateAnimator();
        }
    }

    private void PatrolMove()
    {
        if (!patrolPointA || !patrolPointB)
        {
            ChangeState(State.Idle);
            return;
        }

        Vector2 target = currentPatrolTarget.position;
        Vector2 pos = rb.position;
        Vector2 dir = (target - pos).normalized;
        rb.linearVelocity = dir * moveSpeed;

        // Switch patrol point when reached
        if (Vector2.Distance(pos, target) <= patrolArriveThreshold)
        {
            currentPatrolTarget = currentPatrolTarget == patrolPointA ? patrolPointB : patrolPointA;
        }
    }

    private void ChaseMove()
    {
        if (!player)
        {
            ChangeState(patrolPointA && patrolPointB ? State.Patrol : State.Idle);
            return;
        }

        Vector2 dir = ((Vector2)player.position - rb.position).normalized;
        rb.linearVelocity = dir * moveSpeed;
    }

    private void TryAttack()
    {
        if (!player) return;

        // Check if attack animation is currently playing
        if (anim)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            bool isAttacking = stateInfo.IsName("Attack");

            // Don't start new attack if animation is playing
            if (isAttacking)
            {
                return;
            }
        }

        // Check cooldown separately
        if (Time.time - lastAttackTime < attackCooldown)
        {
            return;
        }

        // Start attack coroutine
        StartCoroutine(AttackSequence());
    }

    private IEnumerator AttackSequence()
    {
        lastAttackTime = Time.time;

        // Trigger attack animation
        if (anim)
        {
            anim.SetTrigger("attack");
        }

        if (sr)
        {
            sr.color = attackColor;
        }

        // Wait for damage delay
        yield return new WaitForSeconds(attackDamageDelay);

        // Apply damage
        if (player && Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            if (playerHealth)
            {
                playerHealth.TakeDamage(attackDamage);
                Debug.Log($"Enemy attacked. Player HP: {playerHealth.CurrentHP}/{playerHealth.MaxHP}");
            }
        }

        // Wait for animation to complete
        if (anim)
        {
            // Wait one frame to ensure animator has updated
            yield return null;

            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            // Wait for the animation to finish from current normalized time
            while (stateInfo.normalizedTime < 1.0f)
            {
                yield return null;
                stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            }
        }
    }

    private IEnumerator ApplyDamageAfterDelay()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(attackDamageDelay);

        // Check if player still exists and is in range
        if (player && Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            if (playerHealth)
            {
                playerHealth.TakeDamage(attackDamage);
                Debug.Log($"Enemy attacked. Player HP: {playerHealth.CurrentHP}/{playerHealth.MaxHP}");
            }
            else
            {
                Debug.Log("Enemy attacks (no PlayerHealth found).");
            }
        }
        else
        {
            Debug.Log("Attack missed - player out of range.");
        }
    }

    private void ChangeState(State newState)
    {
        if (state == newState) return;
        state = newState;
        ApplyStateVisual();
        UpdateAnimator();

        if (state == State.Attack)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void UpdateAnimator()
    {
        if (!anim) return;

        // Don't update animator parameters while attack animation is playing
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("Attack"))
        {
            return;
        }

        // Set isRunning to true when in Patrol or Chase state (when moving)
        // But NOT when in hit reaction or stunned
        bool isRunning = (state == State.Patrol || state == State.Chase);

        // Disable running animation during hit reaction or stun
        if (enemyHealth != null && (enemyHealth.IsInHitReaction || enemyHealth.IsStunned))
        {
            isRunning = false;
        }

        anim.SetBool("isRunning", isRunning);
    }

    private void ApplyStateVisual()
    {
        if (!sr) return;
        switch (state)
        {
            case State.Idle: sr.color = idleColor; break;
            case State.Patrol: sr.color = patrolColor; break;
            case State.Chase: sr.color = chaseColor; break;
            case State.Attack: sr.color = attackColor; break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (patrolPointA && patrolPointB)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(patrolPointA.position, patrolPointB.position);
            Gizmos.DrawWireSphere(patrolPointA.position, 0.1f);
            Gizmos.DrawWireSphere(patrolPointB.position, 0.1f);
        }
    }
}