using UnityEngine;

/// <summary>
/// Simple lever interaction:
/// - When player presses the interaction key while inside the lever trigger, it toggles targets.
/// - Common usage: disable a wall collider/object and enable the next-level entrance/teleporter.
/// </summary>
public class LeverToggle : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactionKey = KeyCode.F;
    [SerializeField] private string playerTag = "Player";

    [Header("Toggle Targets")]
    [Tooltip("Objects to enable when the lever is pulled (teleporter/entrance).")]
    [SerializeField] private GameObject[] enableOnPull;

    [Tooltip("Objects to disable when the lever is pulled (wall blocks).")]
    [SerializeField] private GameObject[] disableOnPull;

    [Header("Behavior")]
    [Tooltip("If true, lever only works once (cannot close again).")]
    [SerializeField] private bool oneShot = true;

    [Header("Timing")]
    [Tooltip("If > 0, waits this many frames before applying the toggle. Useful if the lever animation should play first.")]
    [SerializeField] private int toggleDelayFrames = 0;

    [Header("Lever Animation (Optional)")]
    [Tooltip("If assigned, the lever animator will be driven by triggers after the delay.")]
    [SerializeField] private Animator leverAnimator;
    [Tooltip("Trigger name to play the pulled animation.")]
    [SerializeField] private string pullTriggerName = "Pull";
    [Tooltip("Trigger name to play the return animation.")]
    [SerializeField] private string returnTriggerName = "Return";

    private bool playerNearby;
    private bool isOn;
    private bool isApplying;

    public bool CanInteract()
    {
        if (isApplying) return false;
        if (oneShot && isOn) return false; // cannot close again in one-shot mode
        return true;
    }

    private void Update()
    {
        if (!playerNearby) return;
        if (!CanInteract()) return;
        if (PauseController.IsGamePaused) return;

        if (Input.GetKeyDown(interactionKey))
            Interact();
    }

    public void Interact()
    {
        if (!CanInteract()) return;

        Debug.Log($"[LeverToggle] Interact on '{name}'. isOn(before)={isOn}, playerNearby={playerNearby}");

        // Toggle state (open/close).
        isOn = !isOn;
        StartCoroutine(ApplyToggleAfterDelay());
    }

    private System.Collections.IEnumerator ApplyToggleAfterDelay()
    {
        isApplying = true;

        // Wait lever animation frames before switching targets.
        for (int i = 0; i < Mathf.Max(0, toggleDelayFrames); i++)
            yield return null;

        if (isOn)
        {
            Debug.Log($"[LeverToggle] Applying ON state on '{name}'.");
            if (leverAnimator != null && !string.IsNullOrEmpty(pullTriggerName))
                leverAnimator.SetTrigger(pullTriggerName);

            if (enableOnPull != null)
            {
                for (int i = 0; i < enableOnPull.Length; i++)
                {
                    GameObject obj = enableOnPull[i];
                    if (obj != null) obj.SetActive(true);
                }
            }

            if (disableOnPull != null)
            {
                for (int i = 0; i < disableOnPull.Length; i++)
                {
                    GameObject obj = disableOnPull[i];
                    if (obj != null) obj.SetActive(false);
                }
            }
        }
        else
        {
            Debug.Log($"[LeverToggle] Applying OFF state on '{name}'.");
            if (leverAnimator != null && !string.IsNullOrEmpty(returnTriggerName))
                leverAnimator.SetTrigger(returnTriggerName);

            // Toggle back: reverse the original actions.
            if (enableOnPull != null)
            {
                for (int i = 0; i < enableOnPull.Length; i++)
                {
                    GameObject obj = enableOnPull[i];
                    if (obj != null) obj.SetActive(false);
                }
            }

            if (disableOnPull != null)
            {
                for (int i = 0; i < disableOnPull.Length; i++)
                {
                    GameObject obj = disableOnPull[i];
                    if (obj != null) obj.SetActive(true);
                }
            }
        }

        isApplying = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayerCollider(other))
            playerNearby = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayerCollider(other))
            playerNearby = false;
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null) return false;
        if (other.CompareTag(playerTag)) return true;

        // Fallback: if the collider isn't tagged (common for child hitboxes),
        // still allow interaction when it belongs to the player.
        return other.GetComponentInParent<PlayerMovement>() != null;
    }
}

