using System.Collections;
using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    [Tooltip("Animator with the death animation (should have a 'Die' trigger)")]
    public Animator animator;

    [Tooltip("Assign the Game Over UI GameObject (Canvas root). It will be activated when the death animation finishes.")]
    public GameObject gameOverUI;

    [Tooltip("Optional: list of components (movement, input handlers, etc.) to disable when player dies.")]
    public MonoBehaviour[] disableOnDeath;

    [Tooltip("If true the script expects an Animation Event to call OnDeathAnimationComplete(). If false, the script will wait for the death clip automatically.")]
    public bool useAnimationEvent = false;

    // Call this to start the death sequence (e.g. from health logic)
    public void Die()
    {
        // Disable player controls immediately so player can't move during death anim
        if (disableOnDeath != null)
        {
            foreach (var comp in disableOnDeath)
            {
                if (comp != null) comp.enabled = false;
            }
        }

        if (animator != null)
        {
            animator.SetTrigger("Die");

            if (!useAnimationEvent)
            {
                // start coroutine that waits for the death animation to finish then shows game over
                StartCoroutine(WaitForDeathAnimationAndShowGameOver());
            }
            // if useAnimationEvent == true, OnDeathAnimationComplete() will be called by the animation event
        }
        else
        {
            // Fallback: if no animator, show Game Over immediately
            ShowGameOver();
        }
    }

    // This method is still available for users who prefer Animation Events.
    public void OnDeathAnimationComplete()
    {
        ShowGameOver();
    }

    IEnumerator WaitForDeathAnimationAndShowGameOver()
    {
        if (animator == null)
        {
            ShowGameOver();
            yield break;
        }

        // Give animator a frame to transition into the death state
        yield return null;

        // Try to detect the current playing clip (repeat a short time in case of transition)
        float waitForStateTimeout = 2f;
        float timer = 0f;

        while (timer < waitForStateTimeout)
        {
            var clips = animator.GetCurrentAnimatorClipInfo(0);
            if (clips != null && clips.Length > 0)
            {
                var clip = clips[0].clip;
                if (clip != null)
                {
                    // Wait the clip length adjusted by animator speed
                    float clipLength = clip.length / Mathf.Max(0.0001f, animator.speed);
                    yield return new WaitForSeconds(clipLength);
                    ShowGameOver();
                    yield break;
                }
            }

            // not yet in a state with a clip; wait a frame and try again
            yield return null;
            timer += Time.deltaTime;
        }

        // Fallback: timed wait if we couldn't determine clip length
        yield return new WaitForSeconds(1f);
        ShowGameOver();
    }

    void ShowGameOver()
    {
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }
        else
        {
            Debug.LogWarning("GameOver UI not assigned on PlayerDeath.");
        }
    }
}