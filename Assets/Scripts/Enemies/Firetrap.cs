using UnityEngine;
using System.Collections;
using System.Reflection;

public class Firetrap : MonoBehaviour
{
    [SerializeField] private float damage;

    [Header("Firetrap Timers")]
    [SerializeField] private float activationDelay;
    [SerializeField] private float activeTime;
    private Animator anim;
    private SpriteRenderer spriteRend;

    private bool triggered; //when the trap gets triggered
    private bool active; //when the trap is active and can hurt the player

    private void Awake()
    {
        anim = GetComponent<Animator>();
        spriteRend = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            if (!triggered)
                StartCoroutine(ActivateFiretrap());

            if (active)
            {
                var health = collision.GetComponent<Health>();
                // Play the player's death animation (uses the "die" parameter on your player animator)
                var playerAnim = collision.GetComponent<Animator>();
                if (playerAnim != null)
                    TryPlayDeathAnimation(playerAnim);

                if (health != null)
                {
                    // Prefer an explicit Die() method if available
                    var dieMethod = health.GetType().GetMethod("Die", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (dieMethod != null)
                    {
                        dieMethod.Invoke(health, null);
                    }
                    else
                    {
                        // Fall back to lethal damage
                        health.TakeDamage(float.MaxValue);
                    }
                }
            }
        }
    }

    // Use the player's "die" trigger parameter (matches your animator parameters)
    private void TryPlayDeathAnimation(Animator playerAnim)
    {
        if (playerAnim == null) return;
        playerAnim.SetTrigger("die");
    }

    private IEnumerator ActivateFiretrap()
    {
        //turn the sprite red to notify the player and trigger the trap
        triggered = true;
        spriteRend.color = Color.red;

        //Wait for delay, activate trap, turn on animation, return color back to normal
        yield return new WaitForSeconds(activationDelay);
        spriteRend.color = Color.white; //turn the sprite back to its initial color
        active = true;
        anim.SetBool("activated", true);

        //Wait until X seconds, deactivate trap and reset all variables and animator
        yield return new WaitForSeconds(activeTime);
        active = false;
        triggered = false;
        anim.SetBool("activated", false);
    }
}