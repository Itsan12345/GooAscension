using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MechBossHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 500f;
    private float currentHealth;

    [Header("World Space Health Bar")]
    [SerializeField] private Canvas worldHealthBarCanvas;
    [SerializeField] private Slider worldHealthBarSlider;
    [SerializeField] private float healthBarOffset = 2.8f;

    [Header("Screen Space Boss Bar (optional)")]
    [Tooltip("Assign a Screen Space - Overlay Canvas with a Slider child for a cinematic boss HP bar.")]
    [SerializeField] private Canvas screenHealthBarCanvas;
    [SerializeField] private Slider screenHealthBarSlider;

    [Header("Phase 2 Threshold")]
    [SerializeField] private float phase2Threshold = 0.5f;

    [Header("Death FX")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float destroyDelay = 3f;
    [SerializeField] private float explosionZ = -1f;
    [SerializeField] private Transform animatorObj;
    [SerializeField] private float deathAnimationDuration = 2f;

    [Header("Damage Flash")]
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.12f;

    [Header("Immunity (Glow Phase)")]
    [SerializeField] private AudioSource blockedAudioSource;
    [SerializeField] private AudioClip blockedSoundClip;
    [SerializeField] private float blockedShakeDuration  = 0.15f;
    [SerializeField] private float blockedShakeMagnitude = 0.1f;

    private bool dead = false;
    private bool phase2Triggered = false;
    private bool glow25Triggered  = false;
    private bool isImmune = false;
    public bool IsImmune => isImmune;
    private Coroutine flashCoroutine;
    private bool isFlashing = false;

    private Rigidbody2D rb;
    private Collider2D[] colliders;
    private MechBossAI bossAI;
    private Animator anim;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        colliders = GetComponentsInChildren<Collider2D>(true);
        bossAI = GetComponent<MechBossAI>();

        if (animatorObj != null)
            anim = animatorObj.GetComponent<Animator>();

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers != null)
        {
            originalColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
                originalColors[i] = spriteRenderers[i].color;
        }

        Debug.Log($"[MechBossHealth] Awake on '{name}'. worldHealthBarCanvas={worldHealthBarCanvas}, screenHealthBarCanvas={screenHealthBarCanvas}");

        SetupWorldHealthBar();
    }

    private void Start()
    {
        // Always show health bars
        if (worldHealthBarCanvas != null)
            worldHealthBarCanvas.gameObject.SetActive(true);
        if (screenHealthBarCanvas != null)
            screenHealthBarCanvas.gameObject.SetActive(true);

        UpdateHealthBars();
    }

    private void Update()
    {
        UpdateWorldHealthBarPosition();
    }

    public void TakeDamage(float damage)
    {
        if (dead) return;

        // Boss is immune during the Glow phase — play blocked feedback and bail
        if (isImmune)
        {
            if (blockedAudioSource != null && blockedSoundClip != null)
                blockedAudioSource.PlayOneShot(blockedSoundClip);

            ScreenShake.Trigger(blockedShakeDuration, blockedShakeMagnitude);
            Debug.Log("[MechBoss] Hit blocked — boss is immune during Glow phase.");
            return;
        }

        currentHealth -= damage;
        UpdateHealthBars();
        Flash();

        Debug.Log($"[MechBoss] Took {damage} damage. HP: {currentHealth}/{maxHealth}");

        if (!phase2Triggered && currentHealth / maxHealth <= phase2Threshold)
        {
            phase2Triggered = true;
            if (bossAI != null) bossAI.EnterPhase2();
        }

        // Force a glow cycle at 25% HP (Phase 2 only — TriggerGlow guards this internally)
        if (!glow25Triggered && currentHealth / maxHealth <= 0.25f)
        {
            glow25Triggered = true;
            if (bossAI != null) bossAI.TriggerGlow();
        }

        if (currentHealth <= 0f)
            Die();
    }

    /// <summary>
    /// Called by MechBossAI to toggle immunity on/off during the Glow phase.
    /// </summary>
    public void SetImmune(bool immune)
    {
        isImmune = immune;
        Debug.Log($"[MechBoss] Immunity set to {immune}.");
    }

    private void Flash()
    {
        // Stop any ongoing flash so a new hit always shows the color immediately
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            RestoreOriginalColors();
        }
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) yield break;
        isFlashing = true;

        for (int i = 0; i < spriteRenderers.Length; i++)
            if (spriteRenderers[i] != null) spriteRenderers[i].color = flashColor;

        yield return new WaitForSeconds(flashDuration);
        RestoreOriginalColors();
        flashCoroutine = null;
    }

    private void RestoreOriginalColors()
    {
        if (spriteRenderers != null && originalColors != null)
            for (int i = 0; i < spriteRenderers.Length && i < originalColors.Length; i++)
                if (spriteRenderers[i] != null) spriteRenderers[i].color = originalColors[i];
        isFlashing = false;
    }

    private void Die()
    {
        dead = true;

        NotifyPlayerOfKill();

        if (flashCoroutine != null) { StopCoroutine(flashCoroutine); flashCoroutine = null; }
        RestoreOriginalColors();

        if (bossAI != null) bossAI.DisableAI();

        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }

        foreach (var c in colliders)
            if (c != null && c.gameObject != gameObject) c.enabled = false;

        if (anim != null) anim.SetTrigger("die");

        if (worldHealthBarCanvas != null) worldHealthBarCanvas.gameObject.SetActive(false);
        if (screenHealthBarCanvas != null) screenHealthBarCanvas.gameObject.SetActive(false);

        Invoke(nameof(OnDeathAnimationComplete), deathAnimationDuration);
    }

    private void OnDeathAnimationComplete()
    {
        if (explosionPrefab != null)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(0f, 2f), explosionZ);
                Instantiate(explosionPrefab, transform.position + offset, Quaternion.identity);
            }
        }

        foreach (var r in GetComponentsInChildren<SpriteRenderer>(true))
            r.enabled = false;

        Destroy(gameObject, destroyDelay);
    }

    private void SetupWorldHealthBar()
    {
        if (worldHealthBarCanvas == null)
        {
            Debug.Log("[MechBossHealth] SetupWorldHealthBar: worldHealthBarCanvas is null, skipping world-space HP bar setup.");
            return;
        }

        // Safety: if the canvas is a parent (ancestor) of any boss sprite, applying
        // localScale = 0.01 or SetActive(false) would hide/shrink the entire boss.
        // Detect this misconfiguration and bail out with a clear error.
        if (spriteRenderers != null)
        {
            foreach (SpriteRenderer sr in spriteRenderers)
            {
                if (sr != null && sr.transform.IsChildOf(worldHealthBarCanvas.transform))
                {
                    Debug.LogError(
                        "[MechBossHealth] worldHealthBarCanvas is a PARENT of the boss sprites — " +
                        "this is why the boss is invisible! " +
                        "FIX: in the Prefab, make the health bar canvas a CHILD of the boss root (not the root itself). " +
                        "Health bar setup skipped to prevent shrinking/hiding the boss.");
                    return;
                }
            }
        }

        if (worldHealthBarSlider == null)
            worldHealthBarSlider = worldHealthBarCanvas.GetComponentInChildren<Slider>();

        worldHealthBarCanvas.renderMode = RenderMode.WorldSpace;
        worldHealthBarCanvas.transform.localScale = Vector3.one * 0.01f;
        UpdateWorldHealthBarPosition();
        worldHealthBarCanvas.gameObject.SetActive(false);
    }

    private void UpdateWorldHealthBarPosition()
    {
        if (worldHealthBarCanvas == null) return;

        worldHealthBarCanvas.transform.position = transform.position + Vector3.up * healthBarOffset;
        if (Camera.main != null)
        {
            worldHealthBarCanvas.transform.rotation = Camera.main.transform.rotation;
        }
    }

    private void UpdateHealthBars()
    {
        float ratio = currentHealth / maxHealth;

        if (worldHealthBarSlider != null)
            worldHealthBarSlider.value = ratio;

        if (screenHealthBarSlider != null)
            screenHealthBarSlider.value = ratio;
    }

    private void NotifyPlayerOfKill()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerEnergy energy = playerObj.GetComponentInParent<PlayerEnergy>();
            if (energy != null) energy.OnEnemyKilled();
        }
    }

    public void ShowHealthBars()
    {
        if (worldHealthBarCanvas != null) worldHealthBarCanvas.gameObject.SetActive(true);
        if (screenHealthBarCanvas != null) screenHealthBarCanvas.gameObject.SetActive(true);
        UpdateHealthBars();
    }

    public float GetHealthPercent() => currentHealth / maxHealth;
    public bool IsDead() => dead;
}
