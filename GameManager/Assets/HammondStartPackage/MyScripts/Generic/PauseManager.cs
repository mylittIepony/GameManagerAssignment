using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;


public class PauseManager : MonoBehaviour
{

    public static PauseManager Instance { get; private set; }
    public static bool IsPaused { get; private set; }

    [Header("input")]
    public InputActionReference pauseAction;

    [Header("UI")]
    public GameObject gameplayCanvas;
    public GameObject pauseCanvas;

    [Header("behaviour")]
    public bool freezeTime = true;
    public bool pauseAudio = true;
    public bool manageCursor = true;
    public bool allowPause = true;

    [Header("specific component control")]
    public MonoBehaviour[] disableWhilePaused;

    [Header("events")]
    public UnityEvent onPause;
    public UnityEvent onResume;


    InputAction _fallbackAction;
    CursorLockMode _savedLockMode;
    bool _savedCursorVisible;
    float _savedTimeScale;
    readonly HashSet<Object> _pauseLocks = new HashSet<Object>();



    void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        IsPaused = false;
        EnsureInputAction();
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

        if (IsPaused)
            ForceResume();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }


    void EnsureInputAction()
    {
        if (pauseAction != null) return;

        _fallbackAction = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
    }

    InputAction GetPauseAction()
    {
        return pauseAction != null ? pauseAction.action : _fallbackAction;
    }

    void OnPauseInput(InputAction.CallbackContext ctx)
    {
        Toggle();
    }


    public void Toggle()
    {
        if (IsPaused)
            Resume();
        else
            Pause();
    }


    public void Pause()
    {
        if (IsPaused) return;
        if (!allowPause) return;
        if (_pauseLocks.Count > 0) return;

        ApplyPause(true);
    }


    public void Resume()
    {
        if (!IsPaused) return;
        ApplyPause(false);
    }


    public void AcquirePauseLock(Object key)
    {
        _pauseLocks.Add(key);
        if (IsPaused) Resume();
    }

    public void ReleasePauseLock(Object key)
    {
        _pauseLocks.Remove(key);
    }

    public void ForceResume()
    {
        if (!IsPaused) return;
        ApplyPause(false);
    }



    void ApplyPause(bool pause)
    {
        IsPaused = pause;

        if (freezeTime)
        {
            if (pause)
            {
                _savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = _savedTimeScale > 0f ? _savedTimeScale : 1f;
            }
        }


        if (pauseAudio)
            AudioListener.pause = pause;

        if (gameplayCanvas != null) gameplayCanvas.SetActive(!pause);
        if (pauseCanvas != null) pauseCanvas.SetActive(pause);



        if (disableWhilePaused != null)
        {
            foreach (MonoBehaviour mb in disableWhilePaused)
            {
                if (mb != null)
                    mb.enabled = !pause;
            }
        }

        if (manageCursor)
        {
            if (pause)
            {
                _savedLockMode = Cursor.lockState;
                _savedCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = _savedLockMode;
                Cursor.visible = _savedCursorVisible;
            }
        }

        if (pause)
            onPause?.Invoke();
        else
            onResume?.Invoke();
    }
}