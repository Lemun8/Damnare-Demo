using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(AudioSource))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 4f;

    [Header("Footstep Audio")]
    public AudioClip[] footstepClips; // Assign in Inspector
    public float footstepInterval = 0.4f; // time between steps
    private float footstepTimer;

    private bool movementEnabled = true;

    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator animator;
    private AudioSource audioSource;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        // Optional setup
        audioSource.loop = false; // footsteps should not loop continuously
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (movementEnabled)
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
        }
        else
        {
            movement = Vector2.zero;
        }

        // Normalize diagonal movement
        if (movement.magnitude > 1)
            movement.Normalize();

        // Animation
        if (animator != null)
        {
            animator.SetFloat("MoveX", movement.x);
            animator.SetFloat("MoveY", movement.y);
            animator.SetFloat("Speed", movement.sqrMagnitude);
        }

        HandleFootsteps();
    }

    void FixedUpdate()
    {
        // Movement
        if (movementEnabled)
        {
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
        }
    }

    void HandleFootsteps()
    {
        // Only play footsteps if moving
        if (movement.magnitude > 0.1f)
        {
            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0f && footstepClips.Length > 0)
            {
                AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
                audioSource.pitch = Random.Range(0.9f, 1.1f); // small pitch variation for realism
                audioSource.PlayOneShot(clip);
                footstepTimer = footstepInterval;
            }
        }
        else
        {
            // reset timer so it doesn’t immediately play when moving again
            footstepTimer = 0f;
        }
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;

        // If disabled, stop all movement immediately
        if (!enabled)
        {
            movement = Vector2.zero;
            rb.velocity = Vector2.zero;

            if (animator != null)
                animator.SetFloat("Speed", 0);
        }
    }
}
