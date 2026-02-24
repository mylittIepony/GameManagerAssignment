using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonLook : MonoBehaviour
{
    [Header("sensitivity")]
    public float mouseSensitivity = 2f;
    public float gamepadSensitivity = 100f;

    [Header("clamp")]
    public float minVerticalAngle = -90f;
    public float maxVerticalAngle = 90f;

    [Header("references")]
    public Transform playerBody;

    [Header("options")]
    public bool invertY = false;
    public bool lockAndHideCursor = true;

    [Header("input")]
    public string lookActionName = "Look";

    [Header("smoothing")]
    public float smoothing = 10f;

    InputAction _lookAction;
    float _verticalRotation;
    bool _cursorLocked;
    Vector2 _smoothedLook;

    void Awake()
    {
        if (playerBody == null && transform.parent != null)
            playerBody = transform.parent;

        _verticalRotation = transform.localEulerAngles.x;
        if (_verticalRotation > 180f) _verticalRotation -= 360f;
    }

    void OnEnable()
    {
        TryBindAction();
        SetCursorLock(lockAndHideCursor);
    }

    void OnDisable()
    {
        SetCursorLock(false);
    }

    void TryBindAction()
    {

        if (InputManager.Instance != null)
        {
            _lookAction = InputManager.Instance.FindAction(lookActionName);
            if (_lookAction != null) return;
        }

        foreach (var map in FindObjectsByType<PlayerInput>(FindObjectsSortMode.None))
        {
            var action = map.actions.FindAction(lookActionName);
            if (action != null)
            {
                _lookAction = action;
                return;
            }
        }

        Debug.LogWarning($"could not find input action '{lookActionName}'");
    }

    void SetCursorLock(bool locked)
    {
        _cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void LateUpdate()
    {
        if (PauseManager.IsPaused) return;
        if (_lookAction == null) { TryBindAction(); return; }

        Vector2 rawLook = _lookAction.ReadValue<Vector2>();
        if (rawLook.sqrMagnitude < 0.0001f) return;

        bool isGamepad = Gamepad.current != null &&
                         Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.01f;

        float yInvert = invertY ? 1f : -1f;
        float deltaX = rawLook.x * (isGamepad ? gamepadSensitivity * Time.deltaTime : mouseSensitivity);
        float deltaY = rawLook.y * (isGamepad ? gamepadSensitivity * Time.deltaTime : mouseSensitivity) * yInvert;

        transform.Rotate(Vector3.up * deltaX, Space.World);
        _verticalRotation = Mathf.Clamp(_verticalRotation + deltaY, minVerticalAngle, maxVerticalAngle);
        transform.localRotation = Quaternion.Euler(_verticalRotation, transform.localEulerAngles.y, 0f);
    }
}