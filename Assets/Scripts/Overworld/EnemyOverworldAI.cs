using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyOverworldAI : MonoBehaviour
{
    [Header("Unique ID (set per-instance/prefab)")]
    [Tooltip("Give each overworld enemy a unique id (e.g. goblin_01). This is used for persistent death state if you use WorldStateManager).")]
    public string uniqueID;

    [Header("Battle")]
    public GameObject battlePrefab;

    [Header("Movement Settings")]
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 3f;

    [Header("Vision / Detection")]
    [Tooltip("Max distance the enemy can see")]
    public float viewDistance = 5f;
    [Tooltip("Field of view angle (degrees). Enemy must face player (based on movement direction) to detect.")]
    [Range(10f, 180f)] public float fovAngle = 90f;
    [Tooltip("Layers treated as obstacles (tilemap collision, walls, etc.). Should NOT include Player layer.")]
    public LayerMask obstacleLayerMask;
    [Tooltip("Layers that contain the player (for Raycast checks)")]
    public LayerMask playerLayerMask;

    [Header("Chase Behavior")]
    [Tooltip("Seconds of lost sight before enemy gives up and returns to patrol")]
    public float chaseLoseTime = 3f;
    [Tooltip("How long the enemy will remember the player's last known position (optional usage)")]
    public float rememberPlayerPositionTime = 1.0f;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    private int currentPatrolIndex = 0;

    // internals
    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D myCollider;

    private Vector2 targetPosition;
    private bool isChasing = false;
    private bool lootDropped = false;
    private bool isFrozen = false;

    // movement tracking used for facing (Option A)
    private Vector2 lastMoveDir = Vector2.right; // default face right

    // chasing sight timers
    private float lostSightTimer = 0f;
    private float rememberTimer = 0f;
    private Vector3 lastKnownPlayerPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>();
    }

    IEnumerator Start()
    {
        // wait for player to be present (OverworldSceneManager may spawn it)
        while (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
            else
                yield return null;
        }

        // init patrol target
        if (patrolPoints != null && patrolPoints.Length > 0)
            targetPosition = patrolPoints[currentPatrolIndex].position;

        // If world state says this enemy dead, restore position+dead state (if you're using WorldStateManager)
        if (!string.IsNullOrEmpty(uniqueID) && WorldStateManager.Instance != null)
        {
            if (WorldStateManager.Instance.GetFlag("ENEMY_" + uniqueID))
            {
                if (WorldStateManager.Instance.TryGetPosition("ENEMY_POS_" + uniqueID, out Vector3 savedPos))
                    transform.position = savedPos;

                BecomeDeadAndLootable();
                DropLootOnce();
                yield break;
            }
        }
    }

    void Update()
    {
        if (isFrozen || player == null) return;

        // Detection happens every frame (but uses facing direction computed from last movement)
        bool canSee = CanSeePlayer();

        if (!isChasing)
        {
            if (canSee)
            {
                StartChase();
            }
        }
        else // is chasing
        {
            if (canSee)
            {
                // reset lost-sight timers and update last known position
                lostSightTimer = 0f;
                rememberTimer = rememberPlayerPositionTime;
                lastKnownPlayerPos = player.position;
            }
            else
            {
                // increment lost sight timer
                lostSightTimer += Time.deltaTime;

                // if we still remember player's last position, decrement remember timer to continue moving there
                if (rememberTimer > 0f) rememberTimer -= Time.deltaTime;

                // give up if exceeded allowed lose time
                if (lostSightTimer >= chaseLoseTime && rememberTimer <= 0f)
                    StopChaseAndReturnToPatrol();
            }
        }

        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (isFrozen) return;

        if (isChasing)
            Chase();
        else
            Patrol();
    }

    // -------------------------
    // Movement / Patrol / Chase
    // -------------------------
    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            lastMoveDir = Vector2.zero;
            return;
        }

        Vector2 direction = ((Vector2)targetPosition - rb.position);
        float distance = direction.magnitude;

        if (distance > 0.01f)
        {
            Vector2 moveDir = direction.normalized;
            Vector2 movement = moveDir * patrolSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movement);

            if (movement.sqrMagnitude > 0.0001f)
                lastMoveDir = movement.normalized;
        }
        else
        {
            // reach point -> advance
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            targetPosition = patrolPoints[currentPatrolIndex].position;
        }
    }

    void Chase()
    {
        Vector2 destination;

        // If we still remember the actual player pos (recently seen) use actual position, otherwise last known
        if (rememberTimer > 0f)
            destination = (Vector2)player.position;
        else
            destination = (Vector2)lastKnownPlayerPos;

        Vector2 dir = (destination - rb.position).normalized;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Vector2 movement = dir * chaseSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movement);
            lastMoveDir = movement.normalized;
        }
    }

    void StartChase()
    {
        isChasing = true;
        lostSightTimer = 0f;
        rememberTimer = rememberPlayerPositionTime;
        lastKnownPlayerPos = player.position;
    }

    void StopChaseAndReturnToPatrol()
    {
        isChasing = false;
        lostSightTimer = 0f;
        rememberTimer = 0f;

        // pick nearest patrol index to resume from current position
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            float bestDist = float.MaxValue;
            int bestIdx = 0;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                float d = Vector2.Distance(transform.position, patrolPoints[i].position);
                if (d < bestDist) { bestDist = d; bestIdx = i; }
            }
            currentPatrolIndex = bestIdx;
            targetPosition = patrolPoints[currentPatrolIndex].position;
        }
    }

    // -------------------------
    // Vision / LOS checks
    // -------------------------
    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector2 toPlayer = (player.position - transform.position);
        float distToPlayer = toPlayer.magnitude;
        if (distToPlayer > viewDistance) return false;

        // Determine "forward" from movement-facing direction (Option A)
        Vector2 forward = lastMoveDir;
        if (forward.sqrMagnitude < 0.001f)
        {
            // if not moving, default forward to the local right (so guard looks to local right by default)
            forward = transform.right;
        }
        forward.Normalize();

        Vector2 dirToPlayer = toPlayer.normalized;
        float angle = Vector2.Angle(forward, dirToPlayer);
        if (angle > (fovAngle * 0.5f)) return false;

        // Raycast for obstacles
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer, distToPlayer, obstacleLayerMask | playerLayerMask);

        if (hit.collider == null)
        {
            // no obstacle found and no player caught (unlikely) -> cannot see
            return false;
        }

        // If the first thing we hit is on player layer, we see the player.
        // Some setups use tags, so we also check for "Player" tag as fallback.
        if (((1 << hit.collider.gameObject.layer) & playerLayerMask) != 0 || hit.collider.CompareTag("Player"))
        {
            return true;
        }

        // otherwise we hit an obstacle before player
        return false;
    }

    // -------------------------
    // Utilities: freeze, animations
    // -------------------------
    public void FreezeTemporarily(float duration)
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(FreezeCoroutine(duration));
    }

    private IEnumerator FreezeCoroutine(float duration)
    {
        isFrozen = true;
        isChasing = false;
        lastMoveDir = Vector2.zero;

        rb.simulated = false;
        if (myCollider != null) myCollider.enabled = false;

        if (animator != null)
        {
            animator.SetFloat("Speed", 0);
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", 0);
        }

        yield return new WaitForSeconds(duration);

        if (myCollider != null) myCollider.enabled = true;
        rb.simulated = true;
        isFrozen = false;
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetFloat("MoveX", lastMoveDir.x);
        animator.SetFloat("MoveY", lastMoveDir.y);
        animator.SetFloat("Speed", lastMoveDir.sqrMagnitude);

        // Explicitly override spriteRenderer.flipX each frame to avoid sticky flip keyframes.
        if (spriteRenderer != null && lastMoveDir.x != 0)
            spriteRenderer.flipX = lastMoveDir.x < 0;
    }

    // -------------------------
    // Triggers & Death/loot
    // -------------------------
    void OnTriggerEnter2D(Collider2D other)
    {
        if (isFrozen) return;

        if (other.CompareTag("Player"))
        {
            OverworldBattleTrigger trigger = other.GetComponent<OverworldBattleTrigger>();
            if (trigger != null)
                trigger.EnterBattle(this);
        }
    }

    // Call when the enemy dies (persist dead + pos)
    public void BecomeDeadAndLootable()
    {
        isChasing = false;
        rb.simulated = false;
        if (GetComponent<Collider2D>() != null) GetComponent<Collider2D>().enabled = true;
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        lastMoveDir = Vector2.zero;
        if (animator != null)
        {
            animator.SetFloat("Speed", 0);
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", 0);
        }

        if (!string.IsNullOrEmpty(uniqueID) && WorldStateManager.Instance != null)
        {
            WorldStateManager.Instance.SetFlag("ENEMY_" + uniqueID, true);
            WorldStateManager.Instance.SetPosition("ENEMY_POS_" + uniqueID, transform.position);
        }

        Debug.Log($"{gameObject.name} is dead and lootable now!");
    }

    public void DropLootOnce()
    {
        if (lootDropped) return;
        lootDropped = true;

        var loot = GetComponent<EnemyLoot>();
        if (loot != null)
        {
            loot.DropLoot();
            Debug.Log($"Loot dropped at {transform.position}");
        }
    }

    // -------------------------
    // Editor debugging gizmos
    // -------------------------
    void OnDrawGizmosSelected()
    {
        // draw view distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        // draw FOV lines
        Vector3 forward = (lastMoveDir.sqrMagnitude > 0.001f) ? (Vector3)lastMoveDir : transform.right;
        float half = fovAngle * 0.5f;
        Quaternion leftRot = Quaternion.Euler(0, 0, half);
        Quaternion rightRot = Quaternion.Euler(0, 0, -half);

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.4f);
        Vector3 leftDir = leftRot * forward;
        Vector3 rightDir = rightRot * forward;
        Gizmos.DrawLine(transform.position, transform.position + leftDir.normalized * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + rightDir.normalized * viewDistance);
    }
}
