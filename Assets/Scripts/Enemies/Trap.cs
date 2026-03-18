using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Trap : MonoBehaviour
{
    [Tooltip("Amount of damage applied to the player when hitting the trap.")]
    [SerializeField] private float damage = 1f;

    [Tooltip("If true, call the player's Die() method after a short delay (if the player's health reaches zero).")]
    [SerializeField] private bool callDieAfterDelay = false;

    [Tooltip("Delay before calling Die() on the player (seconds).")]
    [SerializeField] private float dieDelay = 0.5f;

    [Tooltip("If true, trigger the player's Animator using the name in DeathAnimationTrigger.")]
    [SerializeField] private bool triggerDeathAnimation = true;

    [Tooltip("Animator trigger name to set on the player when they hit the trap. Match this exactly to the parameter in the Animator (case sensitive).")]
    [SerializeField] private string deathAnimationTrigger = "die";

    [Tooltip("Stop the player from moving while the death animation plays.")]
    [SerializeField] private bool stopMovementOnHit = true;

    [Tooltip("Freeze Rigidbody2D (zero velocity + set to Kinematic) when stopping movement.")]
    [SerializeField] private bool freezeRigidbodyOnStop = true;

    [Tooltip("Reload the current scene after death (useful if you want an instant restart).")]
    [SerializeField] private bool reloadSceneOnDeath = false;

    [Tooltip("Delay before reloading the scene (seconds).")]
    [SerializeField] private float reloadDelay = 0.5f;

    // --- Movement settings (added) ---
    [Header("Movement")]
    [Tooltip("Enable trap movement.")]
    [SerializeField] private bool enableMovement = true;

    [Tooltip("Local direction the trap moves in (relative to the trap's rotation).")]
    [SerializeField] private Vector2 moveDirection = Vector2.right;

    [Tooltip("Distance (in world units) to move from the start position.")]
    [SerializeField] private float moveDistance = 3f;

    [Tooltip("Speed of movement.")]
    [SerializeField] private float moveSpeed = 2f;

    private Vector3 startPosition;
    private Vector2 startPosition2D;
    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        startPosition2D = rb != null ? rb.position : (Vector2)startPosition;
    }

    private void FixedUpdate()
    {
        if (enableMovement && moveDistance > 0f && moveSpeed > 0f)
        {
            // PingPong produces a value from 0..moveDistance, move between start and start+dir*distance
            Vector2 dir = moveDirection.normalized;
            float offset = Mathf.PingPong(Time.time * moveSpeed, moveDistance);
            Vector2 newPos2D = startPosition2D + dir * offset;

            if (rb != null && rb.bodyType != RigidbodyType2D.Static)
            {
                rb.MovePosition(newPos2D);
            }
            else
            {
                transform.position = (Vector3)newPos2D;
            }
        }
    }

    // Called for trigger colliders
    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other.gameObject);
    }

    // Called for non-trigger collisions
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.gameObject);
    }

    private void HandleHit(GameObject other)
    {
        // Only react to objects tagged "Player"
        if (!other.CompareTag("Player"))
            return;

        // Send damage message; player scripts that implement TakeDamage(float) will receive it.
        other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

        // Trigger the player's death animation if requested.
        if (triggerDeathAnimation)
        {
            Animator animator = other.GetComponent<Animator>();
            if (animator == null)
                animator = other.GetComponentInChildren<Animator>();

            if (animator != null && !string.IsNullOrEmpty(deathAnimationTrigger))
            {
                // Try to set the configured trigger; if the exact name isn't found, try common variants present on the Animator.
                if (!TrySetAnimatorTrigger(animator, deathAnimationTrigger))
                {
                    // fallback attempts for common casing differences
                    if (!TrySetAnimatorTrigger(animator, "Die"))
                        if (!TrySetAnimatorTrigger(animator, "die"))
                            Debug.LogWarning($"Trap: trigger parameter not found on Animator. Tried '{deathAnimationTrigger}', 'Die', 'die'. Player: {other.name}");
                }
            }
            else
            {
                Debug.LogWarning($"Trap: no Animator found on player or death trigger name is empty. Player: {other.name}");
            }
        }

        // Stop player movement during death animation.
        if (stopMovementOnHit)
            StopPlayerMovement(other);

        // Optionally call Die() after a short delay (do not force immediate destruction here)
        if (callDieAfterDelay)
            StartCoroutine(CallDieAfterDelay(other, dieDelay));

        // If you want to reload the scene when the player dies, let the player's Die() method (or other logic) trigger that.
        // As a fallback, reload the scene after a delay if configured (this does not check player health).
        if (reloadSceneOnDeath)
            StartCoroutine(ReloadSceneAfterDelay(reloadDelay));
    }

    private void StopPlayerMovement(GameObject player)
    {
        // Preferred: let the player's script handle disabling input by implementing DisableMovement()
        player.SendMessage("DisableMovement", SendMessageOptions.DontRequireReceiver);

        // Best-effort: stop physics movement and freeze Rigidbody2D so the character doesn't slide.
        if (freezeRigidbodyOnStop)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        // Optionally disable common movement components if present (safe best-effort).
        // If your movement component has a known type name, add it below.
        var behaviours = player.GetComponents<MonoBehaviour>();
        foreach (var b in behaviours)
        {
            if (b == null) continue;
            var typeName = b.GetType().Name;
            // common movement/controller names - adjust to your project's component names if needed
            if (typeName.Contains("Player") || typeName.Contains("Controller") || typeName.Contains("Movement") || typeName.Contains("Input"))
            {
                // avoid disabling health/respawn scripts (conservative check)
                if (typeName.Contains("Health") || typeName.Contains("Respawn") || typeName.Contains("UI"))
                    continue;

                b.enabled = false;
            }
        }
    }

    private bool TrySetAnimatorTrigger(Animator animator, string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName))
            return false;

        var parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Trigger && parameters[i].name == triggerName)
            {
                animator.SetTrigger(triggerName);
                return true;
            }
        }

        return false;
    }

    private IEnumerator CallDieAfterDelay(GameObject other, float delay)
    {
        yield return new WaitForSeconds(delay);
        other.SendMessage("Die", SendMessageOptions.DontRequireReceiver);
    }

    private IEnumerator ReloadSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
