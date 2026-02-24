using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerEnergy playerEnergy;

    private Animator anim;
    private bool facingRight = true;

    // Public accessor for PlayerAnimationEvents
    public PlayerMovement PlayerMovementRef => playerMovement;

    [Header("Weapon System")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootCooldown = 0.3f;
    private bool canShoot = true;

    [Header("Sound Effect Settings")]
    [SerializeField] private SoundEffectLibrary soundEffectLibrary;
    [SerializeField] private AudioSource attackAudioSource;
    [SerializeField] private string swordAttackSoundGroupName = "SwordAttacl";
    [SerializeField] private int swordAttackSoundElementIndex = 0;

    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float attackDamage = 10f;


    [Header("Charged Sword Attack")]
    [SerializeField] private float maxChargeTime = 1.5f;
    [SerializeField] private GameObject swordArcPrefab;

    private float chargeTimer;
    private bool isCharging;
    private bool isGunCharging; // Separate charging state for gun
    private bool usingSword = true;
    private float storedChargeLevel; // Store charge for animation event
    private float storedGunChargeLevel; // Store gun charge for animation event
    
    [Header("Quick Attack Settings")]
    [SerializeField] private float quickClickThreshold = 0.2f; // Time threshold for quick vs hold
    private float mouseDownTimer;
    private bool mouseWasPressed;

    [Header("Weapon State")]
    [SerializeField] private bool usingGun = false;

    [Header("Gun Settings")]
    [SerializeField] private float gunDamage = 20f;
    [SerializeField] private GameObject chargedShotPrefab;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        
        if (playerEnergy == null)
            playerEnergy = GetComponent<PlayerEnergy>();
    }

    private void Update()
    {
        if (playerMovement == null) return;

        // Keep these synced from movement
        anim = playerMovement.CurrentAnimator;
        facingRight = playerMovement.FacingRight;

        // Handle charged attack for both sword and gun when human
        if (playerMovement.IsHuman && (usingSword || usingGun))
        {
            HandleChargedAttack();
        }
        else
        {
            // Reset both charging states if not human or no weapon
            if (isCharging)
            {
                isCharging = false;
                anim.SetBool("isCharging", false);
            }
            if (isGunCharging)
            {
                isGunCharging = false;
                anim.SetBool("isGunCharging", false);
            }
        }

        HandleCombatInput();
    }

    void HandleChargedAttack()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            mouseWasPressed = true;
            mouseDownTimer = 0f;
        }

        if (Mouse.current.leftButton.isPressed && mouseWasPressed)
        {
            mouseDownTimer += Time.deltaTime;
            
            // Start charging only after threshold is exceeded
            if (mouseDownTimer >= quickClickThreshold)
            {
                if (usingGun && !isGunCharging)
                {
                    StartGunCharging();
                }
                else if (!usingGun && !isCharging)
                {
                    StartSwordCharging();
                }
            }
            
            // Continue charging for appropriate weapon
            if (usingGun && isGunCharging)
            {
                ChargeGun();
            }
            else if (!usingGun && isCharging)
            {
                ChargeSword();
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (usingGun && isGunCharging)
            {
                ReleaseGunCharge();
            }
            else if (!usingGun && isCharging)
            {
                ReleaseSwordCharge();
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

void StartSwordCharging()
{
    if (isCharging) return;

    // Check if player has at least half energy before allowing sword charging
    if (playerEnergy == null || playerEnergy.currentEnergy < (playerEnergy.maxEnergy * 0.5f))
    {
        Debug.Log("⚔️ SWORD CHARGING BLOCKED: Energy less than half! Need at least 50% energy.");
        return;
    }

    isCharging = true;
    chargeTimer = 0f;
    anim.SetBool("isCharging", true);
    playerMovement.SetAttackingOrCharging(true);
    Debug.Log("Started sword charging");
}

void StartGunCharging()
{
    if (isGunCharging) return;

    // Check if player has full energy before allowing gun charging
    if (playerEnergy == null || playerEnergy.currentEnergy < playerEnergy.maxEnergy)
    {
        Debug.Log("⚡ GUN CHARGING BLOCKED: Energy not full! Cannot charge gun.");
        return;
    }

    isGunCharging = true;
    chargeTimer = 0f;
    anim.SetBool("isGunCharging", true);
    playerMovement.SetAttackingOrCharging(true);
    Debug.Log("Started gun charging");
}

void ChargeSword()
{
    if (!isCharging) return;

    // Stop charging if energy is less than half
    if (playerEnergy == null || playerEnergy.currentEnergy < (playerEnergy.maxEnergy * 0.5f))
    {
        Debug.Log("⚔️ SWORD CHARGING STOPPED: Energy below half during charging!");
        isCharging = false;
        anim.SetBool("isCharging", false);
        return;
    }

    chargeTimer += Time.deltaTime;
    chargeTimer = Mathf.Min(chargeTimer, maxChargeTime);

    float charge01 = chargeTimer / maxChargeTime;
    anim.SetFloat("chargeLevel", charge01);
}

void ChargeGun()
{
    if (!isGunCharging) return;

    // Stop charging if energy is no longer full
    if (playerEnergy == null || playerEnergy.currentEnergy < playerEnergy.maxEnergy)
    {
        Debug.Log("⚡ GUN CHARGING STOPPED: Energy depleted during charging!");
        isGunCharging = false;
        anim.SetBool("isGunCharging", false);
        return;
    }

    chargeTimer += Time.deltaTime;
    chargeTimer = Mathf.Min(chargeTimer, maxChargeTime);

    float charge01 = chargeTimer / maxChargeTime;
    anim.SetFloat("chargeLevel", charge01);
}

void ReleaseSwordCharge()
{
    if (!isCharging) return;

    // Check if player has at least half energy for charged sword attack
    if (playerEnergy == null || playerEnergy.currentEnergy < (playerEnergy.maxEnergy * 0.5f))
    {
        Debug.Log("⚔️ CHARGED SWORD BLOCKED: Not enough energy! Need at least half energy bar.");
        isCharging = false;
        anim.SetBool("isCharging", false);
        playerMovement.SetAttackingOrCharging(false);
        return;
    }

    isCharging = false;
    anim.SetBool("isCharging", false);
    anim.SetTrigger("chargedAttack");
    playerMovement.SetAttackingOrCharging(true);
    
    // Store charge level for animation event
    storedChargeLevel = chargeTimer / maxChargeTime;
    Debug.Log($"⚔️ CHARGED SWORD READY: Released sword charge: {storedChargeLevel:F2}");
}

void ReleaseGunCharge()
{
    if (!isGunCharging) return;

    // Check if player has full energy for charged shot
    if (playerEnergy == null || playerEnergy.currentEnergy < playerEnergy.maxEnergy)
    {
        Debug.Log("⚡ CHARGED SHOT BLOCKED: Not enough energy! Need full energy bar.");
        isGunCharging = false;
        anim.SetBool("isGunCharging", false);
        playerMovement.SetAttackingOrCharging(false);
        return;
    }

    isGunCharging = false;
    anim.SetBool("isGunCharging", false);
    anim.SetTrigger("chargedShot");
    playerMovement.SetAttackingOrCharging(true);
    
    // Store charge level for animation event
    storedGunChargeLevel = chargeTimer / maxChargeTime;
    Debug.Log($"⚡ CHARGED SHOT READY: Released gun charge: {storedGunChargeLevel:F2}");
}


public void ActivateSwordArc(float charge01)
{
    if (swordArcPrefab == null)
    {
        Debug.LogError("SwordArcPrefab is not assigned!");
        return;
    }

    // Double-check energy before firing (safety check)
    if (playerEnergy != null && playerEnergy.currentEnergy < (playerEnergy.maxEnergy * 0.5f))
    {
        Debug.LogWarning("⚔️ CHARGED SWORD CANCELLED: Insufficient energy during firing!");
        return;
    }

    // Play sword attack sound for charged attack
    if (soundEffectLibrary != null && attackAudioSource != null)
    {
        soundEffectLibrary.PlaySoundEffect(attackAudioSource, swordAttackSoundGroupName, swordAttackSoundElementIndex);
        Debug.Log("Playing charged sword attack sound effect");
    }

    // Consume energy to half
    if (playerEnergy != null)
    {
        float halfEnergy = playerEnergy.maxEnergy * 0.5f;
        playerEnergy.SpendEnergy(playerEnergy.currentEnergy - halfEnergy);
        Debug.Log($"⚔️ ENERGY CONSUMED: Energy reduced to half for charged sword! Energy: {playerEnergy.currentEnergy}/{playerEnergy.maxEnergy}");
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
    
    Debug.Log($"⚔️ CHARGED SWORD FIRED: Charge level: {charge01:F2}, Energy reduced to half!");
}

// Method to be called from animation events
public void ActivateSwordArcFromAnimation()
{
    ActivateSwordArc(storedChargeLevel);
}

public void ActivateChargedShot(float charge01)
{
    Debug.Log($"ActivateChargedShot called on {gameObject.name}, chargedShotPrefab = {chargedShotPrefab}");
    
    if (chargedShotPrefab == null)
    {
        Debug.LogError($"ChargedShotPrefab is not assigned on {gameObject.name}! Check the PlayerCombat component.");
        return;
    }

    // Double-check energy before firing (safety check)
    if (playerEnergy != null && playerEnergy.currentEnergy < playerEnergy.maxEnergy)
    {
        Debug.LogWarning("⚡ CHARGED SHOT CANCELLED: Insufficient energy during firing!");
        return;
    }

    // Consume all player energy
    if (playerEnergy != null)
    {
        playerEnergy.SpendEnergy(playerEnergy.maxEnergy);
        Debug.Log($"⚡ ENERGY CONSUMED: All energy spent for charged shot! Energy: {playerEnergy.currentEnergy}/{playerEnergy.maxEnergy}");
    }

    // Instantiate the charged shot at the fire point
    GameObject shotInstance = Instantiate(chargedShotPrefab, firePoint.position, Quaternion.identity);
    
    // Set the direction based on facing direction
    float dir = facingRight ? 1 : -1;
    
    // Set up the charged shot
    ChargedShot chargedShot = shotInstance.GetComponent<ChargedShot>();
    if (chargedShot != null)
    {
        chargedShot.SetDirection(dir);
        chargedShot.SetCharge(charge01);
    }

    Debug.Log($"🔥 CHARGED SHOT FIRED: Charge level: {charge01:F2}, Energy depleted!");
}

// Method to be called from animation events
public void ActivateChargedShotFromAnimation()
{
    ActivateChargedShot(storedGunChargeLevel);
}

/// <summary>
/// Called by animation events when attack/charged attack animations finish
/// </summary>
public void OnAttackAnimationEnd()
{
    if (playerMovement != null)
        playerMovement.SetAttackingOrCharging(false);
}


    private void HandleCombatInput()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame)
            SwitchWeapon();

        // Handle regular gun shooting only for quick clicks (not charging)
        // The charging system now handles both sword and gun charged attacks
        // Regular gun shooting is now handled by quick clicks in the charging system
        
        // Note: Both sword and gun attacks (regular and charged) are now handled in HandleChargedAttack()
    }

    private void TryToAttack()
    {
        if (!playerMovement.IsHuman) return;
        
        if (usingGun)
        {
            // Quick gun shot
            TryToShoot();
        }
        else
        {
            // Sword attack only when grounded
            if (!playerMovement.IsGrounded) return;
            
            if (anim != null)
            {
                anim.SetTrigger("attack");
                playerMovement.SetAttackingOrCharging(true);
            }
        }
    }

    public void PlayAttackSound()
    {
        // Play sword attack sound from animation event
        if (soundEffectLibrary != null && attackAudioSource != null)
        {
            soundEffectLibrary.PlaySoundEffect(attackAudioSource, swordAttackSoundGroupName, swordAttackSoundElementIndex);
            Debug.Log("Playing sword attack sound effect from animation");
        }
    }

    public void DamageTarget()
    {
        if (usingGun) return;
        if (!playerMovement.IsGrounded) return;

        // Use OverlapCircleAll without layer filter so it can hit both enemies and boss
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackPoint.position, attackRange);

        foreach (Collider2D col in hitColliders)
        {
            // Skip anything on the player's own object
            if (col.GetComponentInParent<PlayerMovement>() != null) continue;

            IDamageable target = col.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(10f);
                Debug.Log("Dealt 10 QUICK damage to " + col.name);
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
