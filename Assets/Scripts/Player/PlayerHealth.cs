using System.Collections; // Needed for the flash coroutine
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Needed to restart the game


public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("UI References")]
    public Image healthBarTotal;
    public Image healthBarCurrent;

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

        // I-frames: ignore damage while invulnerable (dash)
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null && pm.IsInvulnerable)
            return;


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
        if (healthBarCurrent != null)
        {
            healthBarCurrent.fillAmount = currentHealth / maxHealth;
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

        // Show game over text
        if (gameOverText != null)
            gameOverText.SetActive(true);

        // Spawn explosion once
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, new Vector3(transform.position.x, transform.position.y, -1f), Quaternion.identity);

        // Disable movement
        var pm = GetComponent<PlayerMovement>();
        if (pm != null)
            pm.EnableMovementAndJump(false);

        // Disable hitboxes / visuals (children)
        foreach (Transform child in transform)
            child.gameObject.SetActive(false);

        // Disable physics so nothing keeps pushing it around
        var body = GetComponent<Rigidbody2D>();
        if (body != null)
            body.simulated = false;

        var col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        // IMPORTANT: Do NOT deactivate the whole player object,
        // because you want UI / Invokes / logic to continue.
        Invoke(nameof(ReloadLevel), reloadDelay);
    }


    void ReloadLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}