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

        HandleCombatInput();
    }

    private void HandleCombatInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            SwitchWeapon();

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (playerMovement.IsHuman && usingGun)
                TryToShoot();
            else
                TryToAttack();
        }
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
