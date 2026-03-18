using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float startingHealth;
    [SerializeField] private string trapTag = "Trap";
    public float currentHealth { get; private set; }
    private Animator anim;
    private bool dead;

    private void Awake()
    {
        currentHealth = startingHealth;
        anim = GetComponent<Animator>();
    }

    public void TakeDamage(float _damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - _damage, 0, startingHealth);

        if (currentHealth > 0)
        {
            anim.SetTrigger("hurt");
            //iframes
        }
        else
        {
            if (!dead)
            {
                anim.SetTrigger("die");
                GetComponent<PlayerMovement>().enabled = false;
                dead = true;
            }
        }
    }

    public void AddHealth(float _value)
    {
        currentHealth = Mathf.Clamp(currentHealth + _value, 0, startingHealth);
    }

    // Instant-kill when contacting a trap (supports both trigger and normal collisions).
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (dead) return;
        if (other.CompareTag(trapTag))
        {
            TakeDamage(currentHealth); // reduce to 0 => die
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (dead) return;
        if (collision.collider.CompareTag(trapTag))
        {
            TakeDamage(currentHealth); // reduce to 0 => die
        }
    }
}
