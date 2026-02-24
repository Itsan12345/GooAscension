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

    private bool dead = false;
    private bool phase2Triggered = false;
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

        SetupWorldHealthBar();
    }

    private void Start()
    {
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

        currentHealth -= damage;
        UpdateHealthBars();
        Flash();

        Debug.Log($"[MechBoss] Took {damage} damage. HP: {currentHealth}/{maxHealth}");

        if (!phase2Triggered && currentHealth / maxHealth <= phase2Threshold)
        {
            phase2Triggered = true;
            if (bossAI != null) bossAI.EnterPhase2();
        }

        if (currentHealth <= 0f)
            Die();
    }

    private void Flash()
    {
        if (isFlashing) return;
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
            worldHealthBarCanvas = GetComponentInChildren<Canvas>();

        if (worldHealthBarSlider == null && worldHealthBarCanvas != null)
            worldHealthBarSlider = worldHealthBarCanvas.GetComponentInChildren<Slider>();

        if (worldHealthBarCanvas != null)
        {
            worldHealthBarCanvas.renderMode = RenderMode.WorldSpace;
            worldHealthBarCanvas.transform.localScale = Vector3.one * 0.01f;
            UpdateWorldHealthBarPosition();
            worldHealthBarCanvas.gameObject.SetActive(false);
        }
    }

    private void UpdateWorldHealthBarPosition()
    {
        if (worldHealthBarCanvas == null) return;

        worldHealthBarCanvas.transform.position = transform.position + Vector3.up * healthBarOffset;
        if (Camera.main != null)
        {
            worldHealthBarCanvas.transform.LookAt(Camera.main.transform);
            worldHealthBarCanvas.transform.Rotate(0, 180, 0);
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

    public float GetHealthPercent() => currentHealth / maxHealth;
    public bool IsDead() => dead;
}
