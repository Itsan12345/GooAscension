using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video; // 1. ADD THIS NAMESPACE

public class MainMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject loadingScreen;
    public Slider slider;

    [Header("Video Settings")]
    public VideoPlayer VideoRenderTexture; // 2. Assign the Video Player here in Inspector

    public void PlayGame()
    {
        // Assuming the next level is index + 1
        StartCoroutine(LoadLevelAsync(SceneManager.GetActiveScene().buildIndex + 1));
    }

    IEnumerator LoadLevelAsync(int sceneIndex)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);

        // Stop scene from activating immediately
        operation.allowSceneActivation = false;

        // Activate the Loading Screen UI
        loadingScreen.SetActive(true);

        // 3. PLAY THE VIDEO
        if (VideoRenderTexture != null)
        {
            VideoRenderTexture.Prepare(); // Optional, but good for stability
            VideoRenderTexture.Play();
        }

        float currentProgress = 0f;

        while (!operation.isDone)
        {
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);

            // Artificial Delay Logic
            currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, Time.deltaTime * 0.5f);

            slider.value = currentProgress;

            // Check if loading is finished AND the artificial bar is full
            if (operation.progress >= 0.9f && currentProgress >= 0.99f)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    public void GoToSettingsMenu()
    {
        SceneManager.LoadScene("SettingsMenu");
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Debug.Log("Quit!");
        Application.Quit();
    }
}