using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{

    private PlayerMovement player;
    private PlayerCombat playerCombat;

    private void Awake()
    {
        player = GetComponentInParent<PlayerMovement>();
        playerCombat = GetComponentInParent<PlayerCombat>();
        
        if (playerCombat == null)
        {
            Debug.LogError("PlayerCombat not found in parent! Make sure PlayerCombat is on the same GameObject or parent.");
        }
    }

    public void DamageTarget()
    {
        if (playerCombat != null)
            playerCombat.DamageTarget();
    }

    private void DisableMovementAndJump() => player.EnableMovementAndJump(false);    

    private void EnableMovementAndJump() => player.EnableMovementAndJump(true);

    
}
