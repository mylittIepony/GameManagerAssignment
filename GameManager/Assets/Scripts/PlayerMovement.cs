using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement_NIS : MonoBehaviour
{
    public float moveSpeed = 5f;
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public float jumpForce = 5f;
    public float jumpCooldown = 0.5f;
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    float _lastJumpTime = -999f;
    bool _jumpRequested = false;
    Rigidbody _rb;
    Vector2 _moveInput;

    public AudioClip walkingClip;
    [Range(0f, 1f)] public float walkingVolume = 0.6f;
    public float moveEpsilon = 0.02f;
    AudioSource _audio;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;
        _audio = GetComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.loop = true;
        _audio.spatialBlend = 0f;
        _audio.volume = walkingVolume;
        _audio.clip = walkingClip;
    }

    void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (jumpAction != null) jumpAction.action.Enable();
    }

    void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (jumpAction != null) jumpAction.action.Disable();
        if (_audio != null && _audio.isPlaying) _audio.Stop();
    }

    void Update()
    {
        _moveInput = Vector2.zero;
        if (moveAction != null)
            _moveInput = moveAction.action.ReadValue<Vector2>();
        if (_moveInput.sqrMagnitude < 0.0004f) _moveInput = Vector2.zero;

        bool isMoving = _moveInput.magnitude >= moveEpsilon;
        if (isMoving)
        {
            if (_audio.clip != null && !_audio.isPlaying)
                _audio.Play();
        }
        else
        {
            if (_audio.isPlaying)
                _audio.Stop();
        }

        if (jumpAction != null && jumpAction.action.WasPressedThisFrame() && Time.time >= _lastJumpTime + jumpCooldown)
        {
            _jumpRequested = true;
        }
    }

    void FixedUpdate()
    {
        Vector3 inputDir = new Vector3(_moveInput.x, 0f, _moveInput.y);
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();
        Vector3 moveWorld = transform.TransformDirection(inputDir) * moveSpeed * Time.fixedDeltaTime;
        _rb.MovePosition(_rb.position + moveWorld);

        if (_jumpRequested)
        {
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            _jumpRequested = false;
            _lastJumpTime = Time.time;
        }
    }
}