using UnityEngine;

public class CodeFragment : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Try to find PlayerMovement on the colliding object or its parent
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null)
        {
            player = other.GetComponentInParent<PlayerMovement>();
        }

        if (player != null)
        {
            Debug.Log("Code Fragment collected!");
            player.EnableHumanTransformation(); // Allow form switch
            Destroy(gameObject); // Remove the Code Fragment from scene
        }
    }
}
