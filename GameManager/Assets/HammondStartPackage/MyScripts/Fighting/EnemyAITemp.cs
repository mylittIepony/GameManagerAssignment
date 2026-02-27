using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HealthSystem))]
public class EnemyAITemp : MonoBehaviour
{
    public enum State { Idle, Chase, Attack, Dead }

    [Header("detection")]
    public float detectionRange = 12f;
    public float attackRange = 6f;
    public float losePlayerRange = 18f;
    public string playerTag = "Player";

    [Header("combat")]
    public float attackCooldown = 1.5f;
    public Weapon weapon;

    [Header("movement")]
    public float chaseSpeed = 4f;
    public float stoppingDistance = 2f;
    public float stepHeight = 0.6f;
    public float groundCheckDist = 1.5f;
    public LayerMask groundLayers = ~0;

    [Header("rotation")]
    public float rotateSpeed = 8f;

    [Header("death ragdoll")]
    public float deathForce = 6f;
    public float deathTorque = 8f;

    Rigidbody _rb;
    HealthSystem _health;
    Transform _player;
    State _state = State.Idle;
    float _lastAttackTime;

    Vector3 _dbgNextPos;
    Vector3 _dbgGroundHit;
    bool _dbgGroundFound;
    Vector3 _dbgRayOrigin;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;         
        _rb.isKinematic = false;           
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _health = GetComponent<HealthSystem>();
    }

    void OnEnable()
    {
        _health.OnDeath += OnDeath;
        _health.OnDamaged += OnDamaged;
    }

    void OnDisable()
    {
        _health.OnDeath -= OnDeath;
        _health.OnDamaged -= OnDamaged;
    }

    void Start()
    {
        GameObject playerGO = GameObject.FindGameObjectWithTag(playerTag);
        if (playerGO != null) _player = playerGO.transform;
    }

    void Update()
    {
        if (_state == State.Dead || _player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);

        switch (_state)
        {
            case State.Idle:
                if (dist <= detectionRange) SetState(State.Chase);
                break;

            case State.Chase:
                if (dist > losePlayerRange) { SetState(State.Idle); break; }
                if (dist <= attackRange) { SetState(State.Attack); break; }
                FacePlayer();
                break;

            case State.Attack:
                if (dist > attackRange * 1.2f) { SetState(State.Chase); break; }
                FacePlayer();
                if (Time.time >= _lastAttackTime + attackCooldown)
                {
                    _lastAttackTime = Time.time;
                    weapon?.EnemyFire();
                }
                break;
        }
    }

    void FixedUpdate()
    {
        if (_state == State.Dead) return;

        if (_state == State.Chase)
        {
            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist > stoppingDistance)
                MoveTowardPlayer();
        }
    }

    void MoveTowardPlayer()
    {
        Vector3 dir = (_player.position - transform.position).normalized;
        dir.y = 0f;
        dir.Normalize();

        Vector3 nextPos = transform.position + dir * chaseSpeed * Time.fixedDeltaTime;
        nextPos.y = transform.position.y; 

        transform.position = nextPos;

        _dbgNextPos = nextPos;
    }

 

    void StopMoving()
    {
        _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
    }

    void FacePlayer()
    {
        Vector3 dir = (_player.position - transform.position).normalized;
        dir.y = 0f;
        if (dir == Vector3.zero) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            rotateSpeed * Time.deltaTime);
    }

    void SetState(State newState)
    {
        _state = newState;
        if (newState == State.Idle || newState == State.Attack)
            StopMoving();
    }

    void OnDamaged(float amount, GameObject source)
    {
        if (_state == State.Idle) SetState(State.Chase);
    }

    void OnDeath(GameObject killer)
    {
        _state = State.Dead;
        enabled = false;

        _rb.isKinematic = false;
        _rb.freezeRotation = false;
        _rb.linearVelocity = Vector3.zero;

        Vector3 knockDir = killer != null
            ? (transform.position - killer.transform.position).normalized
            : new Vector3(Random.Range(-1f, 1f), 0.4f, Random.Range(-1f, 1f)).normalized;

        knockDir.y = 0.4f;
        _rb.AddForce(knockDir * deathForce, ForceMode.Impulse);
        _rb.AddTorque(new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-0.3f, 0.3f),
            Random.Range(-1f, 1f)) * deathTorque, ForceMode.Impulse);
    }
    void OnDrawGizmos()
    {
        if (_player != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, _player.position);
        }

        Gizmos.color = _dbgGroundFound ? Color.green : Color.red;
        Gizmos.DrawLine(_dbgRayOrigin, _dbgRayOrigin + Vector3.down * (groundCheckDist + stepHeight));

        if (_dbgGroundFound)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_dbgGroundHit, 0.1f);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_dbgNextPos, 0.15f);
    }

    void OnDrawGizmosSelected()
    {

        Gizmos.color = new Color(1f, 1f, 0f, 0.12f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(1f, 0f, 0f, 0.12f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.08f);
        Gizmos.DrawWireSphere(transform.position, losePlayerRange);

        Gizmos.color = new Color(0f, 1f, 1f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);

        Vector3 rayOrigin = transform.position + Vector3.up * stepHeight;
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * (groundCheckDist + stepHeight));

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * stepHeight, 0.15f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * stepHeight);
    }
    public void DieSilently()
    {
        _state = State.Dead;
        _rb.isKinematic = true;
        enabled = false;
    }

    public void Revive()
    {
        _state = State.Idle;
        _rb.isKinematic = false;
        _rb.freezeRotation = true;
        enabled = true;
    }
}