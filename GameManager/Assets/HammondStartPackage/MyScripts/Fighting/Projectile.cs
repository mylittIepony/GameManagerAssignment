using System.Collections;
using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("movement")]
    public float speed = 20f;
    public float lifetime = 5f;

    [Header("gravity")]
    public bool useCustomGravity = true;
    public float gravityStrength = 9.81f;

    [Header("damage")]
    public int damage = 10;
    public bool useDamageTypes = false;
    public DamageType damageType = DamageType.Physical;
    [Tooltip("tags that this projectile can damage")]
    public string[] damageTags = { "Enemy", "Destructible" };

    [Header("bounce")]
    public bool canBounce = false;
    public int maxBounces = 3;
    [Tooltip("multiplier applied to speed after each bounce")]
    [Range(0f, 1f)] public float bounceDampen = 0.8f;
    public GameObject bounceVFXPrefab;
    public float bounceVFXDestroyTime = 1f;

    [Header("piercing")]
    public bool canPierce = false;
    public int maxPierceTargets = 3;
    [Tooltip("damage multiplier per pierce hit (e.g. 0.8 = 80% damage to each successive target)")]
    [Range(0f, 1f)] public float pierceDamageFalloff = 0.8f;

    [Header("homing")]
    public bool isHoming = false;
    public float homingStrength = 5f;
    public float homingRange = 20f;
    public string homingTargetTag = "Enemy";
    [Tooltip("if set, overrides tag based homing search")]
    public Transform homingTarget;

    [Header("explosion / AoE")]
    public bool explodeOnImpact = false;
    public float explosionRadius = 3f;
    public int explosionDamage = 30;
    [Tooltip("layers hit by explosion overlap check")]
    public LayerMask explosionLayers = ~0;
    public GameObject explosionVFXPrefab;
    public float explosionVFXDestroyTime = 3f;

    [Header("VFX")]
    public GameObject[] spawnVFXPrefabs;
    public float spawnVFXDestroyTime = 2f;
    public GameObject[] impactVFXPrefabs;
    public float impactVFXDestroyTime = 2f;
    [Tooltip("vfx parented to bullet while alive (trail, glow, etc.)")]
    public GameObject[] persistentVFXPrefabs;
    Rigidbody _rb;
    int _bounceCount;
    int _pierceCount;
    float _currentDamage;
    GameObject _owner;
    bool _isDead;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        foreach (GameObject vfx in spawnVFXPrefabs)
        {
            if (vfx == null) continue;
            GameObject spawned = Instantiate(vfx, transform.position, transform.rotation);
            Destroy(spawned, spawnVFXDestroyTime);
        }

        foreach (GameObject vfx in persistentVFXPrefabs)
        {
            if (vfx == null) continue;
            GameObject spawned = Instantiate(vfx, transform.position, transform.rotation, transform);
        }
    }

    void Start()
    {
        Destroy(gameObject, lifetime);

        if (isHoming && homingTarget == null)
            homingTarget = FindClosestHomingTarget();
    }

    public void Initialize(Vector3 direction, GameObject owner = null)
    {
        _owner = owner;
        _currentDamage = damage;
        _rb.linearVelocity = direction.normalized * speed;
    }

    void FixedUpdate()
    {
        if (_isDead) return;

        if (useCustomGravity)
            _rb.AddForce(Vector3.down * gravityStrength, ForceMode.Acceleration);

        if (isHoming && homingTarget != null)
        {
            Vector3 toTarget = (homingTarget.position - transform.position).normalized;
            _rb.linearVelocity = Vector3.RotateTowards(
                _rb.linearVelocity.normalized,
                toTarget,
                homingStrength * Time.fixedDeltaTime * Mathf.Deg2Rad,
                0f) * _rb.linearVelocity.magnitude;
        }

        if (_rb.linearVelocity.sqrMagnitude > 0.1f)
            transform.rotation = Quaternion.LookRotation(_rb.linearVelocity);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_isDead) return;

        bool hitDamageable = TryDamageObject(collision.gameObject, collision.transform.position);

        if (canPierce && hitDamageable)
        {
            _pierceCount++;
            _currentDamage *= pierceDamageFalloff;

            if (_pierceCount >= maxPierceTargets)
                Impact(collision.contacts[0].point, collision.contacts[0].normal);

            return;
        }

        if (canBounce && !hitDamageable)
        {
            _bounceCount++;
            SpawnVFX(bounceVFXPrefab, transform.position, bounceVFXDestroyTime);

            if (_bounceCount >= maxBounces)
                Impact(collision.contacts[0].point, collision.contacts[0].normal);
            else
            {

                _rb.linearVelocity *= bounceDampen;
                return;
            }
        }
        else
        {
            Impact(collision.contacts[0].point, collision.contacts[0].normal);
        }
    }

    void Impact(Vector3 point, Vector3 normal)
    {
        if (_isDead) return;
        _isDead = true;

        foreach (GameObject vfx in impactVFXPrefabs)
        {
            if (vfx == null) continue;
            GameObject spawned = Instantiate(vfx, point, Quaternion.LookRotation(normal));
            Destroy(spawned, impactVFXDestroyTime);
        }

        if (explodeOnImpact)
            Explode(point);

        Destroy(gameObject);
    }

    void Explode(Vector3 center)
    {
        SpawnVFX(explosionVFXPrefab, center, explosionVFXDestroyTime);

        Collider[] hits = Physics.OverlapSphere(center, explosionRadius, explosionLayers);
        foreach (Collider col in hits)
        {
            if (col.gameObject == _owner) continue;
            HealthSystem health = col.GetComponentInParent<HealthSystem>();
            if (health != null)
                health.TakeDamage(explosionDamage, _owner, useDamageTypes ? damageType : DamageType.Physical);
        }
    }

    bool TryDamageObject(GameObject target, Vector3 hitPoint)
    {
        foreach (string tag in damageTags)
        {
            if (!target.CompareTag(tag)) continue;

            HealthSystem health = target.GetComponentInParent<HealthSystem>();
            if (health == null) health = target.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(_currentDamage, _owner, useDamageTypes ? damageType : DamageType.Physical);
                return true;
            }
        }
        return false;
    }

    Transform FindClosestHomingTarget()
    {
        GameObject[] candidates = GameObject.FindGameObjectsWithTag(homingTargetTag);
        Transform closest = null;
        float closestDist = homingRange * homingRange;

        foreach (GameObject candidate in candidates)
        {
            float dist = (candidate.transform.position - transform.position).sqrMagnitude;
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = candidate.transform;
            }
        }

        return closest;
    }

    void SpawnVFX(GameObject prefab, Vector3 pos, float destroyTime)
    {
        if (prefab == null) return;
        GameObject vfx = Instantiate(prefab, pos, Quaternion.identity);
        Destroy(vfx, destroyTime);
    }

    void OnDrawGizmosSelected()
    {
        if (explodeOnImpact)
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }

        if (isHoming)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, homingRange);
        }
    }
}
