using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to a "CodeFragmentPanel" GameObject in your scene (root level, disabled by default).
/// Supports two flows:
///   1. ShowPanel()            — called by CodeFragment on collection (unpauses on Got It)
///   2. ShowPanelFromSettings()— called by the Help button in Settings
///                               (hides Settings, restores it on Got It, keeps game paused)
/// </summary>
public class CodeFragmentPanel : MonoBehaviour
{
    [Header("Keybind Text")]
    [Tooltip("Assign a TMP Text — the keybind list is written automatically at runtime.")]
    [SerializeField] private TMP_Text keybindText;

    [Header("Settings Panel Reference")]
    [Tooltip("Drag the SettingsPanel GameObject here so it can be hidden/restored when using the Help button.")]
    [SerializeField] private GameObject settingsPanelGameObject;

    private bool openedFromSettings = false;

    private const string KEYBIND_CONTENT =
        "<b>— CONTROLS —</b>\n\n" +
        "<b>A / D</b>          Move Left / Right\n" +
        "<b>Space</b>       Jump  <size=70%>(double jump in Human form)</size>\n" +
        "<b>Left Shift</b>  Dash\n" +
        "<b>E</b>               Transform  <size=70%>(Slime ↔ Human)</size>\n\n" +
        "<b>— COMBAT —</b>\n\n" +
        "<b>Q</b>               Switch Weapon  <size=70%>(Sword / Gun)</size>\n" +
        "<b>Left Click</b>  Attack / Shoot\n" +
        "<b>Hold Click</b> Charge Attack / Charged Shot\n\n" +
        "<b>— MENU —</b>\n\n" +
        "<b>Escape</b>      Open Settings";

    private void OnEnable()
    {
        if (keybindText != null)
            keybindText.text = KEYBIND_CONTENT;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    // -------------------------------------------------------------------------
    // Flow 1 — CodeFragment collection
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by CodeFragment when the player picks it up.
    /// Pauses the game. Got It will unpause.
    /// </summary>
    public void ShowPanel()
    {
        openedFromSettings = false;
        PauseController.SetPause(true);
        gameObject.SetActive(true);
    }

    // -------------------------------------------------------------------------
    // Flow 2 — Help button inside Settings
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by the Help (?) button in the Settings menu.
    /// Hides Settings, shows this panel. Got It restores Settings (game stays paused).
    /// </summary>
    public void ShowPanelFromSettings()
    {
        openedFromSettings = true;

        if (settingsPanelGameObject != null)
            settingsPanelGameObject.SetActive(false);

        gameObject.SetActive(true);
    }

    // -------------------------------------------------------------------------
    // Got It
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by the "Got It!" button.
    /// </summary>
    public void OnGotItClicked()
    {
        gameObject.SetActive(false);

        if (openedFromSettings)
        {
            // Restore Settings — game stays paused, cursor stays visible
            if (settingsPanelGameObject != null)
                settingsPanelGameObject.SetActive(true);
        }
        else
        {
            // Opened from CodeFragment — resume normally
            PauseController.SetPause(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }
}
