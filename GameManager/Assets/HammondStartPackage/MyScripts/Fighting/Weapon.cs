using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    public enum WeaponMode { Ranged, Melee }
    public enum FireMode { SemiAuto, FullAuto, Burst }
    public enum MeleeShape { Sphere, Box }

    [Header("mode")]
    public WeaponMode weaponMode = WeaponMode.Ranged;

    [Header("input — action names (must match your InputActionAsset)")]
    public string primaryActionName = "Fire";
    public string reloadActionName = "Reload";
    [Tooltip("leave blank to disable alt fire")]
    public string altFireActionName = "";

    [Header("ranged — mode")]
    public bool isHitscan = false;
    public FireMode fireMode = FireMode.SemiAuto;

    [Header("ranged — projectile")]
    public GameObject projectilePrefab;
    public Transform muzzlePoint;
    [Tooltip("projectiles per shot — e.g. 8 for shotgun")]
    public int projectilesPerShot = 1;
    [Tooltip("spread cone half-angle in degrees")]
    public float spreadAngle = 0f;

    [Header("damage tags")]
    public string[] damageTags = { "Enemy", "Destructible" };

    [Header("ranged — hitscan")]
    public float hitscanRange = 100f;
    public int hitscanDamage = 20;
    public bool useDamageTypes = false;
    public DamageType damageType = DamageType.Physical;
    public LayerMask hitscanLayers = ~0;
    public GameObject hitscanImpactVFXPrefab;
    public float hitscanImpactVFXDestroyTime = 2f;
    [Tooltip("optional tracer line renderer — leave null to skip")]
    public LineRenderer tracerLineRenderer;
    public float tracerDuration = 0.05f;

    [Header("ranged — burst")]
    public int burstCount = 3;
    public float burstDelay = 0.1f;

    [Header("ranged — stats")]
    public float fireRate = 0.15f;
    public int maxAmmo = 30;
    public int currentAmmo;
    public bool infiniteAmmo = false;
    public float reloadTime = 1.5f;
    public bool autoReloadWhenEmpty = true;

    [Header("ranged — knockback")]
    public bool applyKnockback = false;
    public float knockbackForce = 5f;

    [Header("melee — hitbox")]
    public MeleeShape meleeShape = MeleeShape.Sphere;
    public float meleeRadius = 1f;
    public Vector3 meleeBoxHalfExtents = new Vector3(0.5f, 0.5f, 1f);
    public Transform meleeOrigin;
    public LayerMask meleeHitLayers = ~0;
    public int meleeDamage = 25;
    public bool meleeDamageTypes = false;
    public DamageType meleeDamageType = DamageType.Physical;

    [Header("melee — timing")]
    public float attackCooldown = 0.5f;
    [Tooltip("delay after swing starts before hitbox fires — sync with your animation")]
    public float meleeHitDelay = 0.15f;

    [Header("melee — knockback")]
    public bool meleeKnockback = false;
    public float meleeKnockbackForce = 8f;

    [Header("VFX — fire / swing")]
    public GameObject[] fireFXPrefabs;
    public Transform fireFXPoint;
    public float fireFXDestroyTime = 1f;

    [Header("animator")]
    public Animator weaponAnimator;
    public string fireAnimTrigger = "Fire";
    public string reloadAnimTrigger = "Reload";
    public string meleeAnimTrigger = "Slash";

    [Header("audio")]
    public AudioSource audioSource;
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    public AudioClip meleeSound;

    InputAction _primaryAction;
    InputAction _reloadAction;
    InputAction _altFireAction;

    Rigidbody _playerRb;
    float _nextFireTime;
    float _lastMeleeTime;
    bool _isReloading;
    bool _triggerHeld;
    Coroutine _burstCoroutine;

    readonly HashSet<HealthSystem> _meleeHitThisSwing = new HashSet<HealthSystem>();


    void Awake()
    {
        currentAmmo = maxAmmo;
        _playerRb = GetComponentInParent<Rigidbody>();
        audioSource = audioSource != null ? audioSource : GetComponent<AudioSource>();
        weaponAnimator = weaponAnimator != null ? weaponAnimator : GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        _isReloading = false;
        ResolveActions();
        BindActions(true);
    }

    void OnDisable()
    {
        BindActions(false);
        _isReloading = false;
        _triggerHeld = false;
    }


    void ResolveActions()
    {
        if (InputManager.Instance == null)
        {
            StartCoroutine(RetryResolveActions());
            return;
        }

        _primaryAction = InputManager.Instance.FindAction(primaryActionName);
        _reloadAction = InputManager.Instance.FindAction(reloadActionName);

        if (!string.IsNullOrEmpty(altFireActionName))
            _altFireAction = InputManager.Instance.FindAction(altFireActionName);

        if (_primaryAction == null)
            Debug.LogWarning($"[Weapon:{name}] action '{primaryActionName}' not found in InputManager.");
        if (_reloadAction == null && !string.IsNullOrEmpty(reloadActionName))
            Debug.LogWarning($"[Weapon:{name}] action '{reloadActionName}' not found in InputManager.");
    }

    IEnumerator RetryResolveActions()
    {
        float timeout = 2f, elapsed = 0f;
        while (InputManager.Instance == null && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        ResolveActions();
        BindActions(true);
    }

    void BindActions(bool subscribe)
    {
        if (_primaryAction != null)
        {
            if (subscribe)
            {
                _primaryAction.performed += OnPrimaryPerformed;
                _primaryAction.canceled += OnPrimaryCanceled;
            }
            else
            {
                _primaryAction.performed -= OnPrimaryPerformed;
                _primaryAction.canceled -= OnPrimaryCanceled;
            }
        }

        if (_reloadAction != null)
        {
            if (subscribe) _reloadAction.performed += OnReloadPerformed;
            else _reloadAction.performed -= OnReloadPerformed;
        }
    }


    void OnPrimaryPerformed(InputAction.CallbackContext ctx)
    {
        _triggerHeld = true;
        if (weaponMode == WeaponMode.Melee)
            TryMelee();
        else if (fireMode == FireMode.SemiAuto || fireMode == FireMode.Burst)
            TryFire();
    }

    void OnPrimaryCanceled(InputAction.CallbackContext ctx) => _triggerHeld = false;

    void OnReloadPerformed(InputAction.CallbackContext ctx)
    {
        if (!_isReloading && !infiniteAmmo && currentAmmo < maxAmmo)
            StartCoroutine(ReloadRoutine());
    }


    void Update()
    {
        if (weaponMode == WeaponMode.Ranged && fireMode == FireMode.FullAuto && _triggerHeld)
            TryFire();

        if (weaponMode == WeaponMode.Ranged && autoReloadWhenEmpty
            && currentAmmo <= 0 && !_isReloading && !infiniteAmmo)
            StartCoroutine(ReloadRoutine());
    }


    void TryFire()
    {
        if (_isReloading || Time.time < _nextFireTime) return;

        if (!infiniteAmmo && currentAmmo <= 0)
        {
            PlaySound(emptySound);
            return;
        }

        if (fireMode == FireMode.Burst && _burstCoroutine == null)
            _burstCoroutine = StartCoroutine(BurstRoutine());
        else if (fireMode != FireMode.Burst)
            FireOnce();
    }

    void FireOnce()
    {
        _nextFireTime = Time.time + fireRate;
        if (!infiniteAmmo) currentAmmo--;

        TriggerAnim(fireAnimTrigger);
        PlaySound(fireSound);
        SpawnFireFX();

        for (int i = 0; i < projectilesPerShot; i++)
        {
            if (isHitscan) DoHitscan();
            else SpawnProjectile();
        }

        if (applyKnockback && _playerRb != null && muzzlePoint != null)
            _playerRb.AddForce(-muzzlePoint.forward * knockbackForce, ForceMode.Impulse);
    }

    IEnumerator BurstRoutine()
    {
        for (int i = 0; i < burstCount; i++)
        {
            if (_isReloading || (!infiniteAmmo && currentAmmo <= 0)) break;
            FireOnce();
            yield return new WaitForSeconds(burstDelay);
        }
        _nextFireTime = Time.time + fireRate;
        _burstCoroutine = null;
    }

    void SpawnProjectile()
    {
        if (projectilePrefab == null || muzzlePoint == null) return;

        Vector3 dir = ApplySpread(muzzlePoint.forward);
        GameObject proj = Instantiate(projectilePrefab, muzzlePoint.position, Quaternion.LookRotation(dir));
        proj.GetComponent<Projectile>()?.Initialize(dir, gameObject);
    }

    public void EnemyFire()
    {
        if (weaponMode == WeaponMode.Melee)
            TryMelee();
        else
            FireOnce();
    }
    void DoHitscan()
    {
        if (muzzlePoint == null) return;

        Vector3 dir = ApplySpread(muzzlePoint.forward);
        Ray ray = new Ray(muzzlePoint.position, dir);
        Vector3 endPoint = muzzlePoint.position + dir * hitscanRange;

        if (Physics.Raycast(ray, out RaycastHit hit, hitscanRange, hitscanLayers))
        {
            endPoint = hit.point;

            foreach (string tag in damageTags)
            {
                if (!hit.collider.CompareTag(tag)) continue;
                HealthSystem health = hit.collider.GetComponentInParent<HealthSystem>()
                                   ?? hit.collider.GetComponent<HealthSystem>();
                if (health == null) continue;
                health.TakeDamage(hitscanDamage, gameObject, useDamageTypes ? damageType : DamageType.Physical);
                break;
            }

            if (hitscanImpactVFXPrefab != null)
            {
                GameObject vfx = Instantiate(hitscanImpactVFXPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(vfx, hitscanImpactVFXDestroyTime);
            }
        }

        if (tracerLineRenderer != null)
            StartCoroutine(ShowTracer(muzzlePoint.position, endPoint));
    }

    Vector3 ApplySpread(Vector3 forward)
    {
        if (spreadAngle <= 0f) return forward;
        return Quaternion.Euler(
            Random.Range(-spreadAngle, spreadAngle),
            Random.Range(-spreadAngle, spreadAngle), 0f) * forward;
    }

    IEnumerator ShowTracer(Vector3 start, Vector3 end)
    {
        tracerLineRenderer.enabled = true;
        tracerLineRenderer.SetPosition(0, start);
        tracerLineRenderer.SetPosition(1, end);
        yield return new WaitForSeconds(tracerDuration);
        tracerLineRenderer.enabled = false;
    }


    IEnumerator ReloadRoutine()
    {
        _isReloading = true;
        TriggerAnim(reloadAnimTrigger);
        PlaySound(reloadSound);
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
        _isReloading = false;
    }


    void TryMelee()
    {
        if (Time.time < _lastMeleeTime + attackCooldown) return;
        _lastMeleeTime = Time.time;

        TriggerAnim(meleeAnimTrigger);
        PlaySound(meleeSound);
        SpawnFireFX();
        StartCoroutine(MeleeHitRoutine());
    }

    IEnumerator MeleeHitRoutine()
    {
        _meleeHitThisSwing.Clear();
        yield return new WaitForSeconds(meleeHitDelay);

        Transform origin = meleeOrigin != null ? meleeOrigin : transform;
        Collider[] hits = meleeShape == MeleeShape.Sphere
            ? Physics.OverlapSphere(origin.position, meleeRadius, meleeHitLayers)
            : Physics.OverlapBox(origin.position, meleeBoxHalfExtents, origin.rotation, meleeHitLayers);

        foreach (Collider col in hits)
        {
            bool tagMatch = false;
            foreach (string tag in damageTags)
                if (col.CompareTag(tag)) { tagMatch = true; break; }
            if (!tagMatch) continue;

            HealthSystem health = col.GetComponentInParent<HealthSystem>() ?? col.GetComponent<HealthSystem>();
            if (health == null || _meleeHitThisSwing.Contains(health)) continue;

            _meleeHitThisSwing.Add(health);
            health.TakeDamage(meleeDamage, gameObject, meleeDamageTypes ? meleeDamageType : DamageType.Physical);

            if (meleeKnockback && _playerRb != null)
                _playerRb.AddForce(origin.forward * meleeKnockbackForce, ForceMode.Impulse);
        }
    }


    void SpawnFireFX()
    {
        if (fireFXPrefabs == null) return;
        Transform point = fireFXPoint != null ? fireFXPoint : transform;
        foreach (GameObject fx in fireFXPrefabs)
        {
            if (fx == null) continue;
            GameObject spawned = Instantiate(fx, point.position, point.rotation);
            Destroy(spawned, fireFXDestroyTime);
        }
    }

    void TriggerAnim(string trigger)
    {
        if (weaponAnimator != null && !string.IsNullOrEmpty(trigger))
            weaponAnimator.SetTrigger(trigger);
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    void OnDrawGizmosSelected()
    {
        if (weaponMode != WeaponMode.Melee) return;
        Transform origin = meleeOrigin != null ? meleeOrigin : transform;
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        if (meleeShape == MeleeShape.Sphere)
            Gizmos.DrawWireSphere(origin.position, meleeRadius);
        else
        {
            Gizmos.matrix = Matrix4x4.TRS(origin.position, origin.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, meleeBoxHalfExtents * 2f);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}