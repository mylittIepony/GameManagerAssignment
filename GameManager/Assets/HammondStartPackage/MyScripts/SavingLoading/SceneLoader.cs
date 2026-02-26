using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using TMPro;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }
    public static bool IsTransitioning { get; private set; }

    [Header("panels")]
    public CanvasGroup blackOverlay;
    public CanvasGroup blackBackground;

    [Header("fade")]
    public float fadeDuration = 0.5f;

    [Header("loading screen")]
    public GameObject loadingScreen;

    [Header("pony")]
    public CanvasGroup comicPony;
    public CanvasGroup colourPony;
    public RectTransform ponyParent;
    public Vector2 ponyRestingPosition;
    public float ponyRestingRotation = 0f;
    public float spinSpeed = 90f;
    public float wobbleStrength = 22f;
    public float wobbleDuration = 0.45f;
    public int wobbleVibrato = 12;
    public float crossfadeDuration = 0.6f;

    [Header("loading text")]
    public TMP_Text loadingText;
    public float dotInterval = 0.4f;
    public Color textTopColour = Color.white;
    public Color textBottomStart = Color.black;
    public Color textBottomEnd = new Color(1f, 0.678f, 0.94f);

    [Header("scene name")]
    public bool showSceneName = false;
    public TMP_Text sceneNameText;

    [Header("timings")]
    public float minimumLoadTime = 1.5f;
    public float revealDelay = 0.3f;

    bool _isLoading;
    Tween _spinTween;
    Coroutine _dotRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        blackOverlay.alpha = 0f;
        blackOverlay.blocksRaycasts = false;
        if (blackBackground != null) { blackBackground.alpha = 0f; blackBackground.blocksRaycasts = false; }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void LoadScene(string sceneName, bool withLoadingScreen = false)
    {
        if (_isLoading) return;
        StartCoroutine(LoadRoutine(sceneName, withLoadingScreen));
    }

    public void LoadSceneWithSave(string sceneName, bool withLoadingScreen = false)
    {
        if (_isLoading) return;
        SaveManager.SaveBeforeSceneChange();
        StartCoroutine(LoadRoutine(sceneName, withLoadingScreen));
    }

    IEnumerator LoadRoutine(string sceneName, bool withLoadingScreen)
    {
        _isLoading = true;
        IsTransitioning = true;

        if (PauseManager.Instance != null)
            PauseManager.Instance.AcquirePauseLock(this);

        blackOverlay.blocksRaycasts = true;
        yield return FadeCanvasGroup(blackOverlay, 0f, 1f, fadeDuration);

        if (withLoadingScreen)
        {
            if (blackBackground != null) { blackBackground.alpha = 1f; blackBackground.blocksRaycasts = true; }
            SetupLoadingScreen(sceneName);
            loadingScreen.SetActive(true);

            StartSpin();
            StartDotRoutine();
            StartCoroutine(CrossfadeAfterDelay(minimumLoadTime * 0.4f));

            yield return FadeCanvasGroup(blackOverlay, 1f, 0f, fadeDuration);
            blackOverlay.blocksRaycasts = false;
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        float elapsed = 0f;
        while (op.progress < 0.9f || (withLoadingScreen && elapsed < minimumLoadTime))
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (withLoadingScreen)
            yield return FinishLoadingAnimation();

        blackOverlay.alpha = 1f;
        blackOverlay.blocksRaycasts = true;
        if (blackBackground != null) blackBackground.alpha = 1f;

        if (withLoadingScreen)
        {
            if (comicPony != null) comicPony.alpha = 0f;
            if (colourPony != null) colourPony.alpha = 0f;
            if (loadingText != null) loadingText.gameObject.SetActive(false);
        }

        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        if (withLoadingScreen && blackBackground != null)
            blackBackground.blocksRaycasts = false;

        yield return new WaitForSecondsRealtime(revealDelay);

        if (withLoadingScreen && blackBackground != null)
        {
            StartCoroutine(FadeCanvasGroup(blackBackground, 1f, 0f, fadeDuration));
            yield return FadeCanvasGroup(blackOverlay, 1f, 0f, fadeDuration);
            blackBackground.blocksRaycasts = false;
        }
        else
        {
            yield return FadeCanvasGroup(blackOverlay, 1f, 0f, fadeDuration);
        }

        blackOverlay.blocksRaycasts = false;

        if (withLoadingScreen)
            loadingScreen.SetActive(false);

        if (PauseManager.Instance != null)
            PauseManager.Instance.ReleasePauseLock(this);

        IsTransitioning = false;
        _isLoading = false;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        cg.alpha = from;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    IEnumerator CrossfadeAfterDelay(float delay)
    {
        float elapsed = 0f;
        while (elapsed < delay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        float t = 0f;
        float comicStart = comicPony != null ? comicPony.alpha : 0f;
        float colourStart = colourPony != null ? colourPony.alpha : 0f;

        while (t < crossfadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / crossfadeDuration);
            if (comicPony != null) comicPony.alpha = Mathf.Lerp(comicStart, 0f, p);
            if (colourPony != null) colourPony.alpha = Mathf.Lerp(colourStart, 1f, p);
            if (loadingText != null)
            {
                loadingText.colorGradient = new VertexGradient(
                    textTopColour, textTopColour,
                    Color.Lerp(textBottomStart, textBottomEnd, p),
                    Color.Lerp(textBottomStart, textBottomEnd, p)
                );
            }
            yield return null;
        }

        if (comicPony != null) comicPony.alpha = 0f;
        if (colourPony != null) colourPony.alpha = 1f;
        if (loadingText != null)
        {
            loadingText.colorGradient = new VertexGradient(
                textTopColour, textTopColour,
                textBottomEnd, textBottomEnd
            );
        }
    }

    void SetupLoadingScreen(string sceneName)
    {
        if (comicPony != null) comicPony.alpha = 1f;
        if (colourPony != null) colourPony.alpha = 0f;

        if (ponyParent != null)
        {
            ponyParent.anchoredPosition = ponyRestingPosition;
            ponyParent.localRotation = Quaternion.identity;
        }

        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(true);
            loadingText.text = "loading.";
            loadingText.colorGradient = new VertexGradient(
                textTopColour, textTopColour,
                textBottomStart, textBottomStart
            );
        }

        if (sceneNameText != null)
        {
            sceneNameText.gameObject.SetActive(showSceneName);
            if (showSceneName) sceneNameText.text = sceneName;
        }
    }

    void StartSpin()
    {
        _spinTween?.Kill();
        if (ponyParent == null) return;
        _spinTween = ponyParent
            .DORotate(new Vector3(0f, 0f, -36000f), 36000f / spinSpeed, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetUpdate(true);
    }

    void StartDotRoutine()
    {
        if (_dotRoutine != null) StopCoroutine(_dotRoutine);
        _dotRoutine = StartCoroutine(DotRoutine());
    }

    IEnumerator FinishLoadingAnimation()
    {
        if (_dotRoutine != null) { StopCoroutine(_dotRoutine); _dotRoutine = null; }
        if (loadingText != null) loadingText.text = "loading complete!";
        _spinTween?.Kill();

        if (ponyParent != null)
        {
            bool snapDone = false;
            ponyParent.DOLocalRotate(new Vector3(0f, 0f, ponyRestingRotation), 0.2f)
                .SetEase(Ease.OutCubic).SetUpdate(true)
                .OnComplete(() => snapDone = true);
            yield return new WaitUntil(() => snapDone);

            bool wobbleDone = false;
            ponyParent.DOShakeAnchorPos(wobbleDuration, new Vector2(0f, wobbleStrength), wobbleVibrato, 90f, false, true)
                .SetUpdate(true).OnComplete(() => wobbleDone = true);
            yield return new WaitUntil(() => wobbleDone);

            yield return new WaitForSecondsRealtime(0.4f);
        }
    }

    IEnumerator DotRoutine()
    {
        string[] states = { "loading.", "loading..", "loading..." };
        int i = 0;
        while (true)
        {
            if (loadingText != null) loadingText.text = states[i % 3];
            i++;
            yield return new WaitForSecondsRealtime(dotInterval);
        }
    }
}