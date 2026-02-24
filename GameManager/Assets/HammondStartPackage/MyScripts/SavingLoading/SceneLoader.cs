/*
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("fade")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 0.4f;

    [Header("loading screen")]
    public GameObject loadingScreen;
    public Slider progressBar;
    public float minimumLoadTime = 0.5f;

    bool _isLoading;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }

        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void LoadScene(string sceneName, bool showLoadingScreen = false)
    {
        if (_isLoading) return;
        StartCoroutine(LoadRoutine(sceneName, showLoadingScreen));
    }

    public void LoadSceneWithSave(string sceneName, bool showLoadingScreen = false)
    {
        if (_isLoading) return;
        SaveManager.SaveBeforeSceneChange();
        StartCoroutine(LoadRoutine(sceneName, showLoadingScreen));
    }

    IEnumerator LoadRoutine(string sceneName, bool showLoadingScreen)
    {
        _isLoading = true;

        yield return StartCoroutine(Fade(1f));

        if (showLoadingScreen && loadingScreen != null)
        {
            loadingScreen.SetActive(true);
            if (progressBar != null) progressBar.value = 0f;
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        float elapsed = 0f;

        while (op.progress < 0.9f || elapsed < minimumLoadTime)
        {
            elapsed += Time.unscaledDeltaTime;

            if (progressBar != null)
            {
                float displayProgress = Mathf.Clamp01(op.progress / 0.9f);
                progressBar.value = Mathf.MoveTowards(progressBar.value, displayProgress, Time.unscaledDeltaTime * 2f);
            }

            yield return null;
        }

        if (progressBar != null)
        {
            while (progressBar.value < 0.99f)
            {
                progressBar.value = Mathf.MoveTowards(progressBar.value, 1f, Time.unscaledDeltaTime * 3f);
                yield return null;
            }
            progressBar.value = 1f;
        }

        yield return new WaitForSecondsRealtime(0.15f);

        op.allowSceneActivation = true;

        while (!op.isDone)
            yield return null;

        if (loadingScreen != null)
            loadingScreen.SetActive(false);

        Time.timeScale = 1f;

        yield return StartCoroutine(Fade(0f));

        _isLoading = false;
    }

    IEnumerator Fade(float target)
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        fadeCanvasGroup.blocksRaycasts = true;
        float start = fadeCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = target;

        if (target <= 0f)
            fadeCanvasGroup.blocksRaycasts = false;
    }

    public void FadeIn() => StartCoroutine(Fade(0f));
    public void FadeOut() => StartCoroutine(Fade(1f));
}
*/