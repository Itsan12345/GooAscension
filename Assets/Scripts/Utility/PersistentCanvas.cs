using UnityEngine;

/// <summary>
/// Attach to the Canvas GameObject.
/// Keeps the Canvas (and all its UI children) alive across scene loads.
/// If a second Canvas with this script appears (e.g. loaded in the next scene),
/// the duplicate is destroyed so only one Canvas ever exists.
/// </summary>
public class PersistentCanvas : MonoBehaviour
{
    private static PersistentCanvas instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            // A Canvas already exists from a previous scene — destroy this duplicate
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
