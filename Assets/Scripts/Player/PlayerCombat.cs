using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;

    private Animator anim;
    private bool facingRight = true;

    [Header("Weapon System")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootCooldown = 0.3f;
    private bool canShoot = true;

    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float attackDamage = 10f;


    [Header("Charged Sword Attack")]
    [SerializeField] private float maxChargeTime = 1.5f;
    [SerializeField] private float chargedattackDamage = 20f;
    [SerializeField] private GameObject swordArcPrefab;

    private float chargeTimer;
    private bool isCharging;
    private bool usingSword = true;
    private float storedChargeLevel; // Store charge for animation event
    
    [Header("Quick Attack Settings")]
    [SerializeField] private float quickClickThreshold = 0.2f; // Time threshold for quick vs hold
    private float mouseDownTimer;
    private bool mouseWasPressed;

    [Header("Weapon State")]
    [SerializeField] private bool usingGun = false;

    [Header("Gun Settings")]
    [SerializeField] private float gunDamage = 10f;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (playerMovement == null) return;

        // Keep these synced from movement
        anim = playerMovement.CurrentAnimator;
        facingRight = playerMovement.FacingRight;

        // Handle charged attack only when using sword and is human
        if (usingSword && !usingGun && playerMovement.IsHuman)
        {
            HandleChargedAttack();
        }
        else
        {
            // Reset charging state if not using sword
            if (isCharging)
            {
                isCharging = false;
                anim.SetBool("isCharging", false);
            }
        }

        HandleCombatInput();
    }

    void HandleChargedAttack()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mouseWasPressed = true;
            mouseDownTimer = 0f;
        }

        if (Input.GetMouseButton(0) && mouseWasPressed)
        {
            mouseDownTimer += Time.deltaTime;
            
            // Start charging only after threshold is exceeded
            if (mouseDownTimer >= quickClickThreshold && !isCharging)
            {
                StartCharging();
            }
            
            if (isCharging)
            {
                Charge();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isCharging)
            {
                ReleaseCharge();
            }
            else if (mouseWasPressed && mouseDownTimer < quickClickThreshold)
            {
                // Quick click - perform regular attack
                TryToAttack();
            }
            
            mouseWasPressed = false;
            mouseDownTimer = 0f;
        }
    }

void StartCharging()
{
    if (isCharging) return;

    isCharging = true;
    chargeTimer = 0f;
    anim.SetBool("isCharging", true);
}

void Charge()
{
    if (!isCharging) return;

    chargeTimer += Time.deltaTime;
    chargeTimer = Mathf.Min(chargeTimer, maxChargeTime);

    float charge01 = chargeTimer / maxChargeTime;
    anim.SetFloat("chargeLevel", charge01);
}

void ReleaseCharge()
{
    if (!isCharging) return;

    isCharging = false;
    anim.SetBool("isCharging", false);
    anim.SetTrigger("chargedAttack");

    // Store charge level for animation event
    storedChargeLevel = chargeTimer / maxChargeTime;
}


public void ActivateSwordArc(float charge01)
{
    if (swordArcPrefab == null)
    {
        Debug.LogError("SwordArcPrefab is not assigned!");
        return;
    }

    // Instantiate the sword arc at the attack point
    GameObject arcInstance = Instantiate(swordArcPrefab, attackPoint.position, Quaternion.identity);
    
    // Set the direction based on facing direction
    float dir = facingRight ? 1 : -1;
    arcInstance.transform.localScale = new Vector3(dir, 1, 1);

    // Set the charge level for damage
    SwordArcDamage arc = arcInstance.GetComponent<SwordArcDamage>();
    if (arc != null)
    {
        arc.SetCharge(charge01);
    }

    // Destroy the arc after a short duration
    Destroy(arcInstance, 1f);
}

// Method to be called from animation events
public void ActivateSwordArcFromAnimation()
{
    ActivateSwordArc(storedChargeLevel);
}




    private void HandleCombatInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            SwitchWeapon();

        // Handle gun shooting separately
        if (Input.GetKeyDown(KeyCode.Mouse0) && usingGun && playerMovement.IsHuman)
        {
            TryToShoot();
        }
        
        // Note: Sword attacks (both regular and charged) are now handled in HandleChargedAttack()
    }

    private void TryToAttack()
    {
        // Keep your original rule: sword attack only when grounded, and not using gun
        if (usingGun) return;
        if (!playerMovement.IsGrounded) return;

        if (anim != null)
            anim.SetTrigger("attack");
    }

    public void DamageTarget()
    {
        // Only damage when using sword (not gun) and when grounded
        if (usingGun) return;
        if (!playerMovement.IsGrounded) return;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange,
            enemyLayers
        );

        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyHealth enemyHealth = enemy.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);
                Debug.Log("Dealt " + attackDamage + " damage to " + enemy.name);
            }
        }
    }

    public void DamageTargetCharged()
    {
        // Only damage when using sword (not gun) and when grounded
        if (usingGun) return;
        if (!playerMovement.IsGrounded) return;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange,
            enemyLayers
        );

        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyHealth enemyHealth = enemy.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(chargedattackDamage);
                Debug.Log("Dealt " + chargedattackDamage + " CHARGED damage to " + enemy.name);
            }
        }
    }

    private void TryToShoot()
    {
        if (!canShoot) return;
        if (!playerMovement.IsHuman) return;

        if (firePoint == null || bulletPrefab == null)
        {
            Debug.LogError("FirePoint or BulletPrefab is missing!");
            return;
        }

        canShoot = false;

        if (anim != null)
            anim.SetTrigger("shoot");

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        // Keep your z/sorting behavior
        Vector3 bulletPos = bullet.transform.position;
        bulletPos.z = -1f;
        bullet.transform.position = bulletPos;

        SpriteRenderer bulletSprite = bullet.GetComponent<SpriteRenderer>();
        if (bulletSprite != null)
            bulletSprite.sortingOrder = 10;

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            float dir = facingRight ? 1f : -1f;
            bulletScript.SetDirection(dir);
            bulletScript.SetDamage(gunDamage);
        }


        Invoke(nameof(ResetShoot), shootCooldown);
    }

    private void ResetShoot()
    {
        canShoot = true;
    }

    private void SwitchWeapon()
    {
        if (!playerMovement.IsHuman)
            return;

        usingGun = !usingGun;
        usingSword = !usingGun; // Keep these in sync

        if (anim != null)
            anim.SetTrigger("switchWeapon");

        Debug.Log(usingGun ? "Switched to GUN" : "Switched to SWORD");
    }

    // ---------- Damage API ----------
    public float GetAttackDamage() => attackDamage;

    public void SetAttackDamage(float newDamage)
    {
        attackDamage = Mathf.Max(0f, newDamage);
    }

    public void AddAttackDamage(float amount)
    {
        attackDamage = Mathf.Max(0f, attackDamage + amount);
    }

    // ---------- Gizmos ----------
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
