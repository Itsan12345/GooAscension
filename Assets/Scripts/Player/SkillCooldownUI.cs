using UnityEngine;
using UnityEngine.UI;

public class SkillCooldownUI : MonoBehaviour
{
    [Header("Gameplay References")]
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Cooldown Fill Overlays")]
    [Tooltip("Overlay image for sword slash cooldown (filled while cooling down).")]
    [SerializeField] private Image slashCooldownFill;
    [Tooltip("Overlay image for charged shot cooldown (filled while cooling down).")]
    [SerializeField] private Image chargedShotCooldownFill;
    [Tooltip("Overlay image for dash cooldown (filled while cooling down).")]
    [SerializeField] private Image dashCooldownFill;

    [Header("Weapon Swap Icons")]
    [Tooltip("Gun icon image for switch/swap UI state.")]
    [SerializeField] private Image switchToGunIcon;
    [Tooltip("Sword icon image for switch/swap UI state.")]
    [SerializeField] private Image switchToSwordIcon;

    [Header("Transformation Icons")]
    [Tooltip("Human icon image for transformation UI state.")]
    [SerializeField] private Image transformToHumanIcon;
    [Tooltip("Slime icon image for transformation UI state.")]
    [SerializeField] private Image transformToSlimeIcon;

    [Header("Swap Effect Settings")]
    [SerializeField] private bool bringActiveIconToFront = true;
    [SerializeField] private float activeIconAlpha = 1f;
    [SerializeField] private float inactiveIconAlpha = 0.35f;
    [SerializeField] private float swapPulseDuration = 0.18f;
    [SerializeField] private float swapPulseScale = 1.2f;

    private bool initializedWeaponState;
    private bool initializedFormState;
    private bool previousUsingGun;
    private bool previousIsHuman;

    private Coroutine weaponPulseRoutine;
    private Coroutine formPulseRoutine;

    private void Awake()
    {
        if (playerCombat == null)
            playerCombat = FindFirstObjectByType<PlayerCombat>();

        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();
    }

    private void Start()
    {
        SetFill(slashCooldownFill, 0f, 1f);
        SetFill(chargedShotCooldownFill, 0f, 1f);
        SetFill(dashCooldownFill, 0f, 1f);

        if (playerCombat != null)
        {
            previousUsingGun = playerCombat.IsUsingGun;
            initializedWeaponState = true;
            ApplyWeaponState(previousUsingGun, false);
        }

        if (playerMovement != null)
        {
            previousIsHuman = playerMovement.IsHuman;
            initializedFormState = true;
            ApplyFormState(previousIsHuman, false);
        }
    }

    private void Update()
    {
        if (playerCombat != null)
        {
            SetFill(
                slashCooldownFill,
                playerCombat.GetChargedSwordCooldownRemaining(),
                playerCombat.GetChargedSwordCooldownDuration());

            SetFill(
                chargedShotCooldownFill,
                playerCombat.GetChargedShotCooldownRemaining(),
                playerCombat.GetChargedShotCooldownDuration());
        }

        if (playerMovement != null)
        {
            SetFill(
                dashCooldownFill,
                playerMovement.GetDashCooldownRemaining(),
                playerMovement.GetDashCooldownDuration());
        }

        UpdateSwapStateVisuals();
    }

    private static void SetFill(Image fillImage, float remaining, float total)
    {
        if (fillImage == null)
            return;

        if (total <= 0f)
        {
            fillImage.fillAmount = 0f;
            return;
        }

        fillImage.fillAmount = Mathf.Clamp01(remaining / total);
    }

    private void UpdateSwapStateVisuals()
    {
        if (playerCombat != null)
        {
            bool usingGun = playerCombat.IsUsingGun;
            if (!initializedWeaponState || usingGun != previousUsingGun)
            {
                ApplyWeaponState(usingGun, initializedWeaponState);
                previousUsingGun = usingGun;
                initializedWeaponState = true;
            }
        }

        if (playerMovement != null)
        {
            bool isHuman = playerMovement.IsHuman;
            if (!initializedFormState || isHuman != previousIsHuman)
            {
                ApplyFormState(isHuman, initializedFormState);
                previousIsHuman = isHuman;
                initializedFormState = true;
            }
        }
    }

    private void ApplyWeaponState(bool usingGun, bool playPulse)
    {
        Image active = usingGun ? switchToGunIcon : switchToSwordIcon;
        Image inactive = usingGun ? switchToSwordIcon : switchToGunIcon;

        ApplyActiveInactiveVisuals(active, inactive);

        if (playPulse)
        {
            if (weaponPulseRoutine != null)
                StopCoroutine(weaponPulseRoutine);

            weaponPulseRoutine = StartCoroutine(PulseIcon(active));
        }
    }

    private void ApplyFormState(bool isHuman, bool playPulse)
    {
        Image active = isHuman ? transformToHumanIcon : transformToSlimeIcon;
        Image inactive = isHuman ? transformToSlimeIcon : transformToHumanIcon;

        ApplyActiveInactiveVisuals(active, inactive);

        if (playPulse)
        {
            if (formPulseRoutine != null)
                StopCoroutine(formPulseRoutine);

            formPulseRoutine = StartCoroutine(PulseIcon(active));
        }
    }

    private void ApplyActiveInactiveVisuals(Image active, Image inactive)
    {
        SetAlpha(active, activeIconAlpha);
        SetAlpha(inactive, inactiveIconAlpha);

        if (bringActiveIconToFront && active != null)
            active.rectTransform.SetAsLastSibling();
    }

    private static void SetAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color c = image.color;
        c.a = Mathf.Clamp01(alpha);
        image.color = c;
    }

    private System.Collections.IEnumerator PulseIcon(Image image)
    {
        if (image == null || image.rectTransform == null)
            yield break;

        RectTransform iconTransform = image.rectTransform;
        Vector3 startScale = Vector3.one;
        Vector3 targetScale = Vector3.one * Mathf.Max(1f, swapPulseScale);

        float halfDuration = Mathf.Max(0.01f, swapPulseDuration * 0.5f);
        float t = 0f;

        while (t < halfDuration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(t / halfDuration);
            iconTransform.localScale = Vector3.Lerp(startScale, targetScale, normalized);
            yield return null;
        }

        t = 0f;
        while (t < halfDuration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(t / halfDuration);
            iconTransform.localScale = Vector3.Lerp(targetScale, startScale, normalized);
            yield return null;
        }

        iconTransform.localScale = startScale;
    }
}
