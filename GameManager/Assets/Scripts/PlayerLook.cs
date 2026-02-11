using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonLook_NIS : MonoBehaviour
{

    public float mouseSensitivity = 2f;
    public float gamepadSensitivity = 100f;

    public float minVerticalAngle = -90f;
    public float maxVerticalAngle = 90f;


    public Transform playerBody;
    public InputActionReference lookAction;

    public bool invertY = false;
    public bool lockAndHideCursor = true;

    private float _verticalRotation = 0f;
    private float _mouseXAccumulated = 0f;
    private float _mouseYAccumulated = 0f;

    void Awake()
    {
        if (playerBody == null && transform.parent != null)
            playerBody = transform.parent;

        _verticalRotation = transform.localEulerAngles.x;
        if (_verticalRotation > 180f) _verticalRotation -= 360f;
    }

    void OnEnable()
    {
        if (lookAction != null) lookAction.action.Enable();

        if (lockAndHideCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void OnDisable()
    {
        if (lookAction != null) lookAction.action.Disable();

        if (lockAndHideCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        Vector2 lookDelta = Vector2.zero;
        if (lookAction != null)
            lookDelta = lookAction.action.ReadValue<Vector2>();

        float sensitivity = mouseSensitivity;
        bool isGamepad = false;

        if (Gamepad.current != null)
        {
            Vector2 gamepadLook = Gamepad.current.rightStick.ReadValue();
            if (Mathf.Abs(gamepadLook.x) > 0.01f || Mathf.Abs(gamepadLook.y) > 0.01f)
            {
                sensitivity = gamepadSensitivity;
                isGamepad = true;
            }
        }

        if (isGamepad)
        {
            _mouseXAccumulated += lookDelta.x * sensitivity * Time.deltaTime;
            _mouseYAccumulated += lookDelta.y * sensitivity * (invertY ? 1f : -1f) * Time.deltaTime;
        }
        else
        {
            _mouseXAccumulated += lookDelta.x * sensitivity;
            _mouseYAccumulated += lookDelta.y * sensitivity * (invertY ? 1f : -1f);
        }
    }

    void LateUpdate()
    {
        if (Time.timeScale == 0f) return;
        if (playerBody == null) return;

        playerBody.Rotate(Vector3.up * _mouseXAccumulated, Space.Self);

        _verticalRotation += _mouseYAccumulated;

        transform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);

        _mouseXAccumulated = 0f;
        _mouseYAccumulated = 0f;
    }
}