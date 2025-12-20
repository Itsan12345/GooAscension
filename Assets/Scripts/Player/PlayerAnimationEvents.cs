using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{

    private PlayerMovement player;

    private void Awake()
    {
        player = GetComponentInParent<PlayerMovement>();
    }

    private void DisableMovementAndJump() => player.EnableMovementAndJump(false);    

    private void EnableMovementAndJump() => player.EnableMovementAndJump(true);
    

    
}
