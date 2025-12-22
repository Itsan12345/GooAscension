using System.Collections; // Needed for the flash coroutine
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Needed to restart the game


public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("UI Reference")]
    public Slider healthSlider;

    [Header("Damage Flash")]
    [Tooltip("The color the sprite flashes when hit.")]
    [SerializeField] private Color flashColor = Color.red;
    [Tooltip("How long the flash lasts in seconds.")]
    [SerializeField] private float flashDuration = 0.1f;


    
    // Internal references
    private Coroutine flashCoroutine;
    private PlayerMovement playerMovement;

    private void Start()
    {
        currentHealth = maxHealth;
        // Get reference to the sibling movement script so we know which sprite to flash
        playerMovement = GetComponent<PlayerMovement>();
        UpdateUI();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateUI();

        // Triggers the flash effect
        Flash();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Flash()
    {
        // If a flash is already happening, stop it so we can restart a new one instantly
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    // The routine that handles changing colors over time
    private IEnumerator FlashRoutine()
    {
        // 1. Ask PlayerMovement for the currently active sprite
        SpriteRenderer currentSprite = playerMovement.GetActiveSpriteRenderer();

        if (currentSprite != null)
        {
            // 2. Remember the normal color (usually white)
            Color originalColor = Color.white;

            // 3. Change to flash color
            currentSprite.color = flashColor;

            // 4. Wait for a fraction of a second
            yield return new WaitForSeconds(flashDuration);

            // 5. Change back to normal
            currentSprite.color = originalColor;
        }
    }

    void UpdateUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth;
        }
    }

    [Header("Death Settings")]
    [SerializeField] private float reloadDelay = 2f;
    [Header("UI Settings")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject gameOverText; // Drag your Text object here

    public void Die()
    {
        Debug.Log("Player has died!");

        // 1. Spawn the explosion at the player's position
        if (explosionPrefab != null)
        {
            // Use this to ensure the explosion is at Z = -1 (closer to the camera)
            Instantiate(explosionPrefab, new Vector3(transform.position.x, transform.position.y, -1f), Quaternion.identity);
        }

        // 2. Make the character "disappear"
        // Disable the movement logic
        GetComponent<PlayerMovement>().EnableMovementAndJump(false);

        // Turn off all child objects (SlimeHitbox, HumanHitbox, etc.)
        // This makes the player invisible and non-interactive immediately
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }

        // 3. Disable the main collider and physics so enemies stop hitting a "ghost"
        if (GetComponent<Rigidbody2D>()) GetComponent<Rigidbody2D>().simulated = false;

        // 4. Reload the level after the explosion finishes
        Invoke(nameof(ReloadLevel), reloadDelay);

        // This is the line that triggers the enemy to stop!
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
        // Spawn explosion at current position
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // This is the key: Deactivating the parent triggers the enemy's stop logic
        this.gameObject.SetActive(false);

        Invoke(nameof(ReloadLevel), 2f);
    }
   
    void ReloadLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}