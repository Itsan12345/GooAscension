using UnityEngine;

public class CodeFragment : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null)
            player = other.GetComponentInParent<PlayerMovement>();

        if (player != null)
        {
            Debug.Log("Code Fragment collected!");
            player.EnableHumanTransformation();

            // Show the keybind tutorial panel (finds it even if hidden in the Hierarchy)
            CodeFragmentPanel panel = FindObjectOfType<CodeFragmentPanel>(true);
            if (panel != null)
                panel.ShowPanel();
            else
                Debug.LogWarning("CodeFragment: No CodeFragmentPanel found in the scene.");

            Destroy(gameObject);
        }
    }
}
