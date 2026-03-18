using System.Collections;
using UnityEngine;

public class Spikehead : EnemyDamage
{
    [Header("SpikeHead Attributes")]
    [Tooltip("How far below the spikehead to detect the player")]
    [SerializeField] private float detectRange = 5f;
    [Tooltip("Gravity scale applied when spikehead drops")]
    [SerializeField] private float fallGravity = 6f;
    [Tooltip("Delay before spikehead resets to its start position after hitting ground")]
    [SerializeField] private float resetDelay = 2f;
    [Tooltip("Layer mask used to detect the player")]
    [SerializeField] private LayerMask playerLayer;
    [Tooltip("Should the spikehead reset after falling?")]
    [SerializeField] private bool resetAfterFall = true;
    [Tooltip("Radius used to check overlap with player while falling (in world units)")]
    [SerializeField] private float contactRadius = 0.35f;

    private Rigidbody2D rb;
    private Collider2D col;
    private Vector3 startPosition;
    private bool dropped;
    private int playerLayerMaskInt;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        startPosition = transform.position;
        playerLayerMaskInt = (int)playerLayer;

        if (rb == null)
        {
            Debug.LogError($"[Spikehead] Rigidbody2D missing on {name}");
            return;
        }

        // Anchor spikehead: freeze Y and zero velocity so it doesn't fall until triggered.
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
    }

    private void Update()
    {
        if (dropped || rb == null) return;

        // Raycast downwards to detect player below; offset origin a bit so the spike's own collider doesn't block.
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.05f;
        Debug.DrawRay(origin, Vector2.down * detectRange, Color.cyan);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, detectRange, playerLayer);

        if (hit.collider != null)
        {
            Drop();
        }
    }

    private void FixedUpdate()
    {
        // While falling, also actively check overlaps for player in case physics passes through
        if (dropped && rb != null)
        {
            // Use OverlapCircleAll so collision settings won't stop detection
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, contactRadius, playerLayer);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != null && hits[i].CompareTag("Player"))
                {
                    DamagePlayer(hits[i].gameObject);
                }
            }
        }
    }

    private void Drop()
    {
        if (rb == null || dropped) return;

        // Ensure it can fall
        if (rb.bodyType != RigidbodyType2D.Dynamic)
            rb.bodyType = RigidbodyType2D.Dynamic;

        dropped = true;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = fallGravity;
        rb.linearVelocity = Vector2.zero;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // reduce tunnelling at high speed
    }

    private void DamagePlayer(GameObject player)
    {
        if (player == null) return;

        // Disable common movement-related MonoBehaviours so the player cannot move during death animation.
        // We target component type names that likely control input/movement: "move", "controller", "input", "motor".
        var behaviours = player.GetComponents<MonoBehaviour>();
        foreach (var b in behaviours)
        {
            if (b == null) continue;
            string typeName = b.GetType().Name.ToLowerInvariant();
            if (typeName.Contains("move") || typeName.Contains("controller") || typeName.Contains("input") || typeName.Contains("motor"))
            {
                b.enabled = false;
            }
        }

        // Stop player physics movement if present
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            // Freeze position so external forces won't slide the player during animation
            playerRb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        }

        // Trigger player die animation if Animator exists
        Animator playerAnim = player.GetComponent<Animator>() ?? player.GetComponentInChildren<Animator>();
        if (playerAnim != null)
        {
            playerAnim.SetTrigger("Die");
            playerAnim.SetTrigger("die");
        }

        // Typed call fallback (replace with your player's actual script if available)
        player.SendMessage("Die", SendMessageOptions.DontRequireReceiver);
        player.SendMessage("TakeDamage", int.MaxValue, SendMessageOptions.DontRequireReceiver);
    }

    // If using trigger colliders for damage (keeps original base behavior)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);

        if (collision.CompareTag("Player"))
        {
            DamagePlayer(collision.gameObject);
        }
        else if (dropped && resetAfterFall && collision.CompareTag("Ground"))
        {
            StartCoroutine(ResetAfterDelay());
        }
    }

    // If the spikehead uses normal collision
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            DamagePlayer(collision.collider.gameObject);
        }
        else if (dropped && resetAfterFall && collision.collider.CompareTag("Ground"))
        {
            StartCoroutine(ResetAfterDelay());
        }
    }

    private IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSeconds(resetDelay);

        transform.position = startPosition;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            rb.bodyType = RigidbodyType2D.Dynamic; // keep dynamic so physics works after reset
        }

        dropped = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, contactRadius);
    }
}