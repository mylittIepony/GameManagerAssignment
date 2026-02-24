using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("movement")]
    public float maxSpeed = 8f;
    public float groundAcceleration = 60f;
    public float groundDeceleration = 45f;
    public float airAcceleration = 25f;
    public float airDeceleration = 10f;
    [Range(0f, 1f)] public float airControlFactor = 0.6f;

    [Header("rotation")]
    public float rotationSpeed = 1080f;
    public bool firstPersonMode = false;

    [Header("jump")]
    public float jumpForce = 10f;
    public float doubleJumpForce = 9f;
    public int maxJumps = 2;
    public float jumpCooldown = 0.1f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.15f;

    [Header("gravity")]
    public float fallGravityMultiplier = 2.5f;
    public float lowJumpGravityMultiplier = 4f;
    public float maxFallSpeed = 30f;

    [Header("ground")]
    public LayerMask groundMask;
    public float groundCheckRadius = 0.3f;
    public Vector3 groundCheckOffset = new Vector3(0f, 0.05f, 0f);
    public float groundSnapForce = 20f;
    public float maxSlopeAngle = 50f;

    [Header("drag")]
    public float groundDrag = 6f;
    public float airDrag = 0.5f;

    [Header("input")]
    public string moveActionName = "Move";
    public string jumpActionName = "Jump";

    [Header("camera")]
    public Transform cameraTransform;

    [Header("anim")]
    public Animator animator;

    [Header("audio")]
    public AudioClip walkingClip;
    public AudioClip jumpClip;
    public AudioClip landClip;
    [Range(0f, 1f)] public float walkingVolume = 0.6f;

    [Header("fx")]
    public GameObject[] jumpFX;
    public GameObject[] landFX;
    public Transform fxPoint;
    public float fxLifetime = 1.5f;

    Rigidbody _rb;
    InputAction _moveAction;
    InputAction _jumpAction;
    AudioSource _audioLoop;
    AudioSource _audioOneShot;

    Vector2 _moveInput;
    Vector3 _groundNormal = Vector3.up;

    bool _isGrounded;
    bool _wasGrounded;
    float _lastGroundedTime;
    float _lastJumpTime = -999f;
    float _jumpBufferTimer;
    int _jumpsRemaining;
    bool _jumpHeld;
    bool _hasJumpedThisPress;

    int _hashSpeed, _hashGrounded, _hashJump, _hashFalling, _hashLand, _hashYVel;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        SetupAudio();

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        _hashSpeed = Animator.StringToHash("Speed");
        _hashGrounded = Animator.StringToHash("IsGrounded");
        _hashJump = Animator.StringToHash("Jump");
        _hashFalling = Animator.StringToHash("IsFalling");
        _hashLand = Animator.StringToHash("Land");
        _hashYVel = Animator.StringToHash("YVelocity");

        FindActions();
    }

    void OnEnable()
    {
        FindActions();
        if (_moveAction == null || _jumpAction == null)
            StartCoroutine(RetryFind());
    }

    void OnDisable()
    {
        if (_audioLoop != null && _audioLoop.isPlaying) _audioLoop.Stop();
    }

    void SetupAudio()
    {
        var sources = GetComponents<AudioSource>();
        _audioLoop = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        _audioOneShot = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();

        _audioLoop.loop = true;
        _audioLoop.playOnAwake = false;
        _audioLoop.spatialBlend = 0f;
        _audioLoop.volume = walkingVolume;
        _audioLoop.clip = walkingClip;

        _audioOneShot.loop = false;
        _audioOneShot.playOnAwake = false;
        _audioOneShot.spatialBlend = 0f;
    }

    void FindActions()
    {
        if (InputManager.Instance == null) return;
        _moveAction = InputManager.Instance.FindAction(moveActionName);
        _jumpAction = InputManager.Instance.FindAction(jumpActionName);
    }

    IEnumerator RetryFind()
    {
        float t = 0f;
        while ((_moveAction == null || _jumpAction == null) && t < 2f)
        {
            FindActions();
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (_moveAction == null || _jumpAction == null)
            Debug.LogError("could not find input actions ");
    }

    void Update()
    {
        GatherInput();
        UpdateTimers();
        CheckGround();
        CheckLanding();
        HandleJumpInput();
        HandleAudio();
        UpdateAnimator();


    }

    void FixedUpdate()
    {
        ApplyMovement();
        TryJump();
        ApplyGravity();
        ApplyDrag();
        ClampFallSpeed();
        SnapToGround();
        if (!firstPersonMode) RotateTowardMovement();
    }

    void GatherInput()
    {
        _moveInput = Vector2.zero;
        if (_moveAction != null)
            _moveInput = _moveAction.ReadValue<Vector2>();
        if (_moveInput.sqrMagnitude < 0.001f)
            _moveInput = Vector2.zero;
    }

    void UpdateTimers()
    {
        if (_jumpBufferTimer > 0f) _jumpBufferTimer -= Time.deltaTime;
    }

    void CheckGround()
    {
        _wasGrounded = _isGrounded;
        _isGrounded = Physics.CheckSphere(transform.position + groundCheckOffset, groundCheckRadius, groundMask);

        if (_isGrounded)
        {
            _jumpsRemaining = maxJumps;
            _lastGroundedTime = Time.time;

            if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 0.6f, groundMask))
                _groundNormal = hit.normal;
            else
                _groundNormal = Vector3.up;
        }
    }

    void CheckLanding()
    {
        if (_isGrounded && !_wasGrounded) OnLand();
    }

    void OnLand()
    {
        if (landClip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(landClip);
        if (animator != null) animator.SetTrigger(_hashLand);
        SpawnFX(landFX);
    }

    void HandleJumpInput()
    {
        if (_jumpAction == null) return;
        _jumpHeld = _jumpAction.IsPressed();
        if (_jumpAction.WasPressedThisFrame())
        {
            _jumpBufferTimer = jumpBufferTime;
            _hasJumpedThisPress = false;
        }
    }

    void ApplyMovement()
    {
        if (_moveInput.sqrMagnitude < 0.001f)
        {

            Vector3 flat = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            float decel = _isGrounded ? groundDeceleration : airDeceleration;
            if (flat.magnitude > 0.1f)
            {
                _rb.AddForce(-flat.normalized * decel, ForceMode.Acceleration);
                Vector3 newFlat = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
                if (Vector3.Dot(newFlat, flat) < 0f)
                    _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
            }
            return;
        }

        Vector3 dir = GetMoveDirection();
        float accel = _isGrounded ? groundAcceleration : airAcceleration * airControlFactor;
        Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

        if (flatVel.magnitude < maxSpeed || Vector3.Dot(flatVel.normalized, dir) < 0.5f)
            _rb.AddForce(dir * accel * Mathf.Clamp01(_moveInput.magnitude), ForceMode.Acceleration);

        if (flatVel.magnitude > maxSpeed)
        {
            Vector3 clamped = flatVel.normalized * maxSpeed;
            _rb.linearVelocity = new Vector3(clamped.x, _rb.linearVelocity.y, clamped.z);
        }
    }

    Vector3 GetMoveDirection()
    {
        Vector3 forward, right;

        if (cameraTransform != null)
        {
            forward = cameraTransform.forward;
            right = cameraTransform.right;
        }
        else
        {
            forward = transform.forward;
            right = transform.right;
        }

        forward.y = 0f; forward.Normalize();
        right.y = 0f; right.Normalize();

        Vector3 dir = (forward * _moveInput.y + right * _moveInput.x).normalized;

        if (_isGrounded)
            dir = Vector3.ProjectOnPlane(dir, _groundNormal).normalized;

        return dir;
    }

    void RotateTowardMovement()
    {
        if (_moveInput.sqrMagnitude < 0.001f) return;

        Vector3 dir = GetMoveDirection();
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
        _rb.MoveRotation(Quaternion.RotateTowards(_rb.rotation, target, rotationSpeed * Time.fixedDeltaTime));
    }

    void TryJump()
    {
        if (_jumpBufferTimer <= 0f || _hasJumpedThisPress) return;
        if (Time.time < _lastJumpTime + jumpCooldown) return;

        bool coyote = (Time.time - _lastGroundedTime) < coyoteTime && _jumpsRemaining == maxJumps;
        if (!_isGrounded && !coyote && _jumpsRemaining <= 0) return;

        bool isDouble = !_isGrounded && !coyote;
        float force = isDouble ? doubleJumpForce : jumpForce;

        Vector3 vel = _rb.linearVelocity;
        vel.y = 0f;
        _rb.linearVelocity = vel;
        _rb.AddForce(Vector3.up * force, ForceMode.VelocityChange);

        _jumpsRemaining--;
        _jumpBufferTimer = 0f;
        _lastJumpTime = Time.time;
        _hasJumpedThisPress = true;

        if (animator != null) animator.SetTrigger(_hashJump);
        if (jumpClip != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(jumpClip);
        SpawnFX(jumpFX);
    }

    void ApplyGravity()
    {
        if (_isGrounded) return;
        float mult = _rb.linearVelocity.y < 0f ? fallGravityMultiplier :
                     _rb.linearVelocity.y > 0f && !_jumpHeld ? lowJumpGravityMultiplier : 1f;
        float extra = mult - 1f;
        if (extra > 0f) _rb.AddForce(Physics.gravity * extra, ForceMode.Acceleration);
    }

    void ApplyDrag() => _rb.linearDamping = _isGrounded ? groundDrag : airDrag;

    void ClampFallSpeed()
    {
        if (_rb.linearVelocity.y < -maxFallSpeed)
        {
            var v = _rb.linearVelocity;
            v.y = -maxFallSpeed;
            _rb.linearVelocity = v;
        }
    }

    void SnapToGround()
    {
        if (!_isGrounded) return;
        if (Time.time < _lastJumpTime + 0.2f) return;
        if (_rb.linearVelocity.y > 0.5f) return;
        if (Vector3.Angle(Vector3.up, _groundNormal) > maxSlopeAngle) return;

        if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 0.5f, groundMask))
            if (hit.distance - 0.2f > 0.02f)
                _rb.AddForce(Vector3.down * groundSnapForce, ForceMode.Acceleration);
    }

    void HandleAudio()
    {
        bool moving = _moveInput.sqrMagnitude > 0.001f && _isGrounded && _rb.linearVelocity.magnitude > 0.5f;
        if (AudioManager.Instance != null)
            _audioLoop.volume = walkingVolume * AudioManager.Instance.SFXVolume * AudioManager.Instance.MasterVolume;
        if (moving && !_audioLoop.isPlaying) _audioLoop.Play();
        else if (!moving && _audioLoop.isPlaying) _audioLoop.Stop();
    }

    void UpdateAnimator()
    {
        if (animator == null) return;
        float spd = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z).magnitude;
        animator.SetFloat(_hashSpeed, Mathf.Clamp01(spd / maxSpeed), 0.1f, Time.deltaTime);
        animator.SetBool(_hashGrounded, _isGrounded);
        animator.SetBool(_hashFalling, !_isGrounded && _rb.linearVelocity.y < -0.5f);
        animator.SetFloat(_hashYVel, _rb.linearVelocity.y);
    }

    void SpawnFX(GameObject[] fxArray)
    {
        if (fxArray == null || fxPoint == null) return;
        foreach (var fx in fxArray)
        {
            if (fx == null) continue;
            var obj = Instantiate(fx, fxPoint.position, fxPoint.rotation);
            Destroy(obj, fxLifetime);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + groundCheckOffset, groundCheckRadius);
    }
}