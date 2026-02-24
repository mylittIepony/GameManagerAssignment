using UnityEngine;
using UnityEngine.SceneManagement;


public class ButtonActions : MonoBehaviour
{
    [Header("audio")]
    public AudioClip clickSound;
    [Range(0f, 1f)] public float clickVolume = 1f;

    [Header("scenes")]
    public string playSceneName = "SampleScene";
    public string homeSceneName = "Title";

    public void PlayClick()
    {
        if (AudioManager.Instance != null && clickSound != null)
            AudioManager.Instance.PlaySFX(clickSound, clickVolume);
    }


    public void Play()
    {
        PlayClick();
        EnsureUnpaused();
        SceneManager.LoadScene(playSceneName);
    }

    public void Home()
    {
        PlayClick();
        EnsureUnpaused();
        SceneManager.LoadScene(homeSceneName);
    }

    public void Exit()
    {
        PlayClick();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void LoadScene(string sceneName)
    {
        PlayClick();
        EnsureUnpaused();
        SceneManager.LoadScene(sceneName);
    }

    void EnsureUnpaused()
    {

        if (PauseManager.Instance != null)
            PauseManager.Instance.ForceResume();
        else
            Time.timeScale = 1f;
    }
}