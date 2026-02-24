using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("input")]
    public string switchCameraMapName = "";
    public string switchCameraActionName = "SwitchCamera";

    [Header("settings")]
    public int defaultPriority = 10;
    public int activePriority = 20;

    readonly List<CameraEntry> _cameras = new List<CameraEntry>();
    int _activeIndex = -1;

    InputAction _switchCameraAction;

    [System.Serializable]
    public class CameraEntry
    {
        public string id;
        public CinemachineCamera vcam;
        public int basePriority;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CacheAction();
    }

    void OnEnable()
    {
        CacheAction();

        if (_switchCameraAction != null)
            _switchCameraAction.performed += OnSwitchPerformed;
    }

    void OnDisable()
    {
        if (_switchCameraAction != null)
            _switchCameraAction.performed -= OnSwitchPerformed;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void CacheAction()
    {
        if (InputManager.Instance == null) return;

        _switchCameraAction = string.IsNullOrEmpty(switchCameraMapName)
            ? InputManager.Instance.FindAction(switchCameraActionName)
            : InputManager.Instance.FindAction(switchCameraMapName, switchCameraActionName);
    }

    void OnSwitchPerformed(InputAction.CallbackContext ctx)
    {
        CycleNext();
    }

    public void CycleNext()
    {
        CleanNulls();
        if (_cameras.Count <= 1) return;

        int next = (_activeIndex + 1) % _cameras.Count;
        SwitchToIndex(next);
    }

    public void RegisterCamera(CinemachineCamera vcam, string id = "", int basePriority = 0)
    {
        if (vcam == null) return;

        for (int i = _cameras.Count - 1; i >= 0; i--)
        {
            if (_cameras[i].vcam == vcam)
                _cameras.RemoveAt(i);
        }

        CameraEntry entry = new CameraEntry
        {
            id = string.IsNullOrEmpty(id) ? vcam.gameObject.name : id,
            vcam = vcam,
            basePriority = basePriority
        };

        _cameras.Add(entry);

        vcam.Priority = defaultPriority + basePriority;

        if (_activeIndex < 0)
            SwitchToIndex(_cameras.Count - 1);
    }

    public void UnregisterCamera(CinemachineCamera vcam)
    {
        if (vcam == null) return;

        for (int i = _cameras.Count - 1; i >= 0; i--)
        {
            if (_cameras[i].vcam == vcam)
            {
                if (i == _activeIndex) _activeIndex = -1;
                else if (i < _activeIndex) _activeIndex--;

                _cameras.RemoveAt(i);
            }
        }

        if (_activeIndex < 0 && _cameras.Count > 0)
            SwitchToIndex(0);
    }

    public void SwitchTo(string id)
    {
        for (int i = 0; i < _cameras.Count; i++)
        {
            if (_cameras[i].id == id)
            {
                SwitchToIndex(i);
                return;
            }
        }
    }

    public void SwitchTo(CinemachineCamera vcam)
    {
        for (int i = 0; i < _cameras.Count; i++)
        {
            if (_cameras[i].vcam == vcam)
            {
                SwitchToIndex(i);
                return;
            }
        }
    }

    public void SwitchToDefault()
    {
        if (_cameras.Count > 0)
            SwitchToIndex(0);
    }

    public string GetActiveID()
    {
        if (_activeIndex >= 0 && _activeIndex < _cameras.Count)
            return _cameras[_activeIndex].id;
        return "";
    }

    public CinemachineCamera GetActiveVCam()
    {
        if (_activeIndex >= 0 && _activeIndex < _cameras.Count)
            return _cameras[_activeIndex].vcam;
        return null;
    }

    void SwitchToIndex(int index)
    {
        if (index < 0 || index >= _cameras.Count) return;

        for (int i = 0; i < _cameras.Count; i++)
        {
            if (_cameras[i].vcam == null) continue;

            int pri = defaultPriority + _cameras[i].basePriority;
            if (i == index)
                pri = activePriority + _cameras[i].basePriority;

            _cameras[i].vcam.Priority = pri;
        }

        _activeIndex = index;
    }

    void CleanNulls()
    {
        for (int i = _cameras.Count - 1; i >= 0; i--)
        {
            if (_cameras[i].vcam == null)
            {
                if (i == _activeIndex) _activeIndex = -1;
                else if (i < _activeIndex) _activeIndex--;
                _cameras.RemoveAt(i);
            }
        }

        if (_activeIndex < 0 && _cameras.Count > 0)
            _activeIndex = 0;
    }
}
