using UnityEngine;

// Attach this to the animator child object (same object that has the Animator component).
// Then in each animation clip, add Animation Events that call these methods at the right frame.
public class MechBossAnimationEvents : MonoBehaviour
{
    private MechBossAI bossAI;

    private void Awake()
    {
        Transform parent = transform.parent;
        if (parent != null)
            bossAI = parent.GetComponentInChildren<MechBossAI>();

        if (bossAI == null)
            Debug.LogError("[MechBossAnimationEvents] MechBossAI not found! Make sure this script is on the animator child object.");
    }

    // Call this at the melee hit frame in the melee attack animation
    public void DamageMelee()
    {
        if (bossAI != null) bossAI.DamageMelee();
    }

    // Call this at the fire frame in the laser attack animation
    public void FireLaser()
    {
        if (bossAI != null) bossAI.FireLaser();
    }

    // Call this at the impact frame in the stomp animation
    public void DamageStomped()
    {
        if (bossAI != null) bossAI.DamageStomped();
    }
}
