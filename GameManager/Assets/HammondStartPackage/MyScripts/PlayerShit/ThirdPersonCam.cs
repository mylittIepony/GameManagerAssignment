using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{

    public Transform target;
    public InputActionReference lookAction;
    public InputActionReference scrollAction; 
    public InputActionReference rotateButton; 

    public float rotationSpeed = 100f;
    public float minVerticalAngle = -40f;
    public float maxVerticalAngle = 80f;
    public Vector3 targetOffset = new Vector3(0f, 1f, 0f); 

    public float zoomSpeed = 5f;
    public float minZoom = 2f;
    public float maxZoom = 10f;
    public float defaultZoom = 5f;

    float _currentZoom;
    float _verticalAngle;
    float _horizontalAngle;

    void Awake()
    {

        _currentZoom = defaultZoom;


        _horizontalAngle = transform.eulerAngles.y;
        _verticalAngle = 20f;

    }

    void OnEnable()
    {

        if (lookAction != null) lookAction.action.Enable();
        if (scrollAction != null) scrollAction.action.Enable();
        if (rotateButton != null) rotateButton.action.Enable();

    }

    void OnDisable()
    {

        if (lookAction != null) lookAction.action.Disable();
        if (scrollAction != null) scrollAction.action.Disable();
        if (rotateButton != null) rotateButton.action.Disable();

    }

    void LateUpdate()
    {
        if (PauseManager.IsPaused) return;
        if (target == null) return;


        bool isRotating = rotateButton != null && rotateButton.action.IsPressed();


        if (isRotating && lookAction != null)
        {

            Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

            _horizontalAngle += lookInput.x * rotationSpeed * Time.deltaTime;
            _verticalAngle -= lookInput.y * rotationSpeed * Time.deltaTime;

            _verticalAngle = Mathf.Clamp(_verticalAngle, minVerticalAngle, maxVerticalAngle);

        }


        if (scrollAction != null)
        {

            Vector2 scrollInput = scrollAction.action.ReadValue<Vector2>();

            float scrollDelta = scrollInput.y;

            if (Mathf.Abs(scrollDelta) > 0.01f)
            {

                _currentZoom -= Mathf.Sign(scrollDelta) * zoomSpeed * 0.5f; 
                _currentZoom = Mathf.Clamp(_currentZoom, minZoom, maxZoom);

            }

        }


        Vector3 lookAtPoint = target.position + targetOffset;

        Quaternion rotation = Quaternion.Euler(_verticalAngle, _horizontalAngle, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -_currentZoom);

        transform.position = lookAtPoint + offset;
        transform.LookAt(lookAtPoint);

    }

}