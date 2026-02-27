using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public interface IPauseable
{
    void OnPause();
    void OnResume();
}

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    public static bool IsPaused { get; private set; }

    [Header("input")]
    public InputActionReference pauseAction;

    [Header("UI")]
    public GameObject gameplayCanvas;
    public GameObject pauseCanvas;
    public Button saveButton;
    // remove later for other shit
    public Button homeButton;

    [Header("behaviour")]
    public bool freezeTime = true;
    public bool pauseAudio = true;
    public bool allowPause = true;

    [Header("scene rules")]
    public string[] gameplayScenes;

    [Header("events")]
    public UnityEvent onPause;
    public UnityEvent onResume;

    InputAction _fallbackAction;
    float _savedTimeScale;
    readonly HashSet<Object> _pauseLocks = new HashSet<Object>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        IsPaused = false;
        EnsureInputAction();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnEnable()
    {
        GetPauseAction().Enable();
        GetPauseAction().performed += OnPauseInput;
    }

    void OnDisable()
    {
        InputAction action = GetPauseAction();
        action.performed -= OnPauseInput;
        action.Disable();

        if (IsPaused) ForceResume();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsPaused) ForceResume();
        UpdateCanvasForScene();
    }

    void EnsureInputAction()
    {
        if (pauseAction != null) return;
        _fallbackAction = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
    }

    InputAction GetPauseAction() => pauseAction != null ? pauseAction.action : _fallbackAction;

    void OnPauseInput(InputAction.CallbackContext ctx) => Toggle();

    public void Toggle()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused || !allowPause || _pauseLocks.Count > 0) return;
        ApplyPause(true);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        ApplyPause(false);
    }

    public void ForceResume()
    {
        if (!IsPaused) return;
        ApplyPause(false);
    }

    public void AcquirePauseLock(Object key)
    {
        _pauseLocks.Add(key);
        if (IsPaused) Resume();
    }

    public void ReleasePauseLock(Object key) => _pauseLocks.Remove(key);

    void ApplyPause(bool pause)
    {
        IsPaused = pause;

        if (freezeTime)
        {
            if (pause) { _savedTimeScale = Time.timeScale; Time.timeScale = 0f; }
            else Time.timeScale = _savedTimeScale > 0f ? _savedTimeScale : 1f;
        }

        if (pauseAudio) AudioListener.pause = pause;

        bool isGameplay = IsGameplayScene();
        if (gameplayCanvas != null) gameplayCanvas.SetActive(isGameplay && !pause);
        if (pauseCanvas != null) pauseCanvas.SetActive(pause);

        foreach (var p in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IPauseable>())
        {
            if (pause) p.OnPause();
            else p.OnResume();
        }

        if (pause) onPause?.Invoke();
        else onResume?.Invoke();
    }

    void UpdateCanvasForScene()
    {
        bool isGameplay = IsGameplayScene();
        if (gameplayCanvas != null) gameplayCanvas.SetActive(isGameplay && !IsPaused);
        if (pauseCanvas != null) pauseCanvas.SetActive(false);
        if (saveButton != null) saveButton.gameObject.SetActive(isGameplay);
        if (homeButton != null) homeButton.gameObject.SetActive(isGameplay);
    }

    bool IsGameplayScene() => gameplayScenes != null &&
        System.Array.IndexOf(gameplayScenes, SceneManager.GetActiveScene().name) >= 0;
}