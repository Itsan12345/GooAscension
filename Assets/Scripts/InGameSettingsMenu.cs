using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a "SettingsPanel" GameObject in the Level1 scene.
/// Press Escape to open/close. Controls music volume, game SFX volume,
/// save (PlayerPrefs), and quit to main menu.
/// </summary>
public class InGameSettingsMenu : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("The root panel GameObject to show/hide on Escape.")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Audio Sources")]
    [Tooltip("The AudioSource playing background music.")]
    [SerializeField] private AudioSource musicSource;
    [Tooltip("The AudioSource used for game sound effects. Leave empty to control AudioListener volume instead.")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Sliders")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider gameVolumeSlider;

    [Header("Main Menu Scene")]
    [Tooltip("Exact name of your main menu scene as listed in Build Settings.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    // PlayerPrefs keys (shared with MainMenuSettings so volume persists)
    private const string KEY_MUSIC_VOL = "MusicVolume";
    private const string KEY_GAME_VOL  = "GameVolume";

    private bool isOpen = false;

    private void Start()
    {
        // Load saved volumes
        float savedMusic = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 1f);
        float savedGame  = PlayerPrefs.GetFloat(KEY_GAME_VOL,  1f);

        // Apply to sources
        ApplyMusicVolume(savedMusic);
        ApplyGameVolume(savedGame);

        // Set sliders to saved values and hook up listeners
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = savedMusic;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (gameVolumeSlider != null)
        {
            gameVolumeSlider.value = savedGame;
            gameVolumeSlider.onValueChanged.AddListener(OnGameVolumeChanged);
        }

        // Always start with the panel hidden
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleMenu();
    }

    // -------------------------------------------------------------------------
    // Menu open / close
    // -------------------------------------------------------------------------

    public void ToggleMenu()
    {
        isOpen = !isOpen;
        SetMenuOpen(isOpen);
    }

    private void SetMenuOpen(bool open)
    {
        isOpen = open;

        if (settingsPanel != null)
            settingsPanel.SetActive(open);

        PauseController.SetPause(open);

        // Cursor is usable while the menu is open
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = open;
    }

    // -------------------------------------------------------------------------
    // Volume
    // -------------------------------------------------------------------------

    private void OnMusicVolumeChanged(float value)
    {
        ApplyMusicVolume(value);
        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, value);
        PlayerPrefs.Save();
    }

    private void OnGameVolumeChanged(float value)
    {
        ApplyGameVolume(value);
        PlayerPrefs.SetFloat(KEY_GAME_VOL, value);
        PlayerPrefs.Save();
    }

    private void ApplyMusicVolume(float value)
    {
        if (musicSource != null)
            musicSource.volume = value;
    }

    private void ApplyGameVolume(float value)
    {
        if (sfxSource != null)
            sfxSource.volume = value;
        else
            AudioListener.volume = value; // fallback: controls all audio
    }

    // -------------------------------------------------------------------------
    // Buttons
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by the Save button. Writes current settings to PlayerPrefs.
    /// </summary>
    public void OnSaveClicked()
    {
        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, musicVolumeSlider != null ? musicVolumeSlider.value : 1f);
        PlayerPrefs.SetFloat(KEY_GAME_VOL,  gameVolumeSlider  != null ? gameVolumeSlider.value  : 1f);
        PlayerPrefs.Save();
        Debug.Log("[InGameSettingsMenu] Settings saved.");
    }

    /// <summary>
    /// Called by the Quit button. Resumes time then loads the main menu.
    /// </summary>
    public void OnQuitClicked()
    {
        PauseController.SetPause(false);
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Called by a Resume / Close button inside the panel.
    /// </summary>
    public void OnResumeClicked()
    {
        SetMenuOpen(false);
    }
}
