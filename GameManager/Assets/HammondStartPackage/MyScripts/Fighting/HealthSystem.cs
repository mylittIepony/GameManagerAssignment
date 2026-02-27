using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;


public class HealthSystem : MonoBehaviour
{
    [Header("Core")]
    public float maxHealth = 100f;
    [SerializeField] float _currentHealth;

    [Header("shield (optional)")]
    public bool useShield = false;
    public float maxShield = 50f;
    [SerializeField] float _currentShield;
    [Tooltip("seconds before shield starts recovering after taking damage")]
    public float shieldRechargeDelay = 3f;
    public float shieldRechargeRate = 10f;   

    [Header("armour (optional)")]
    public bool useArmour = false;
    [Tooltip("flat damage reduction applied before health loss")]
    public float armourValue = 10f;
    [Tooltip("percentage damage reduction 0 to 1")]
    [Range(0f, 1f)] public float armourPercent = 0f;

    [Header("damage Types (optional)")]
    public bool useDamageTypes = false;
    [Tooltip("multipliers applied per damage type. index matches the DamageType enum.")]
    public float[] damageTypeMultipliers = new float[(int)DamageType.Count] { 1f, 1f, 1f, 1f, 1f };

    [Header("regen (optional)")]
    public bool usePassiveRegen = false;
    public float regenRate = 2f;            
    public float regenDelay = 5f;          

    [Header("death")]
    public Animator animator;
    public string deathAnimTrigger = "Die";
    public GameObject deathVFXPrefab;
    public float deathVFXDestroyTime = 3f;
    [Tooltip("seconds after death VFX before OnDeath event fires (gives animation time)")]
    public float deathEventDelay = 1.5f;
    public bool destroyOnDeath = false;
    public float destroyDelay = 2f;


    public event Action<float, GameObject> OnDamaged;
    public event Action<float> OnHealed;
    public event Action<GameObject> OnDeath;
    public UnityEvent onDeathUnityEvent;
    public event Action<float, float> OnShieldChanged;  
    public event Action<float, float> OnHealthChanged;  

    public float CurrentHealth => _currentHealth;
    public float CurrentShield => _currentShield;
    public float MaxHealth => maxHealth;
    public float MaxShield => maxShield;
    public bool IsDead { get; private set; }

    float _lastDamageTime;
    float _lastShieldDamageTime;
    Coroutine _regenCoroutine;

    void Awake()
    {
        _currentHealth = maxHealth;
        _currentShield = useShield ? maxShield : 0f;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (IsDead) return;

        if (usePassiveRegen && Time.time - _lastDamageTime >= regenDelay)
            Heal(regenRate * Time.deltaTime);

        if (useShield && _currentShield < maxShield && Time.time - _lastShieldDamageTime >= shieldRechargeDelay)
        {
            _currentShield = Mathf.Min(_currentShield + shieldRechargeRate * Time.deltaTime, maxShield);
            OnShieldChanged?.Invoke(_currentShield, maxShield);
        }
    }


    public void TakeDamage(float amount, GameObject source = null, DamageType type = DamageType.Physical)
    {
        if (IsDead) return;

        float damage = amount;

        if (useDamageTypes)
        {
            int idx = (int)type;
            if (idx >= 0 && idx < damageTypeMultipliers.Length)
                damage *= damageTypeMultipliers[idx];
        }

        if (useArmour)
        {
            damage -= armourValue;
            damage *= (1f - armourPercent);
            damage = Mathf.Max(damage, 0f);
        }

        if (damage <= 0f) return;

        _lastDamageTime = Time.time;

        if (useShield && _currentShield > 0f)
        {
            _lastShieldDamageTime = Time.time;
            float shieldAbsorb = Mathf.Min(_currentShield, damage);
            _currentShield -= shieldAbsorb;
            damage -= shieldAbsorb;
            OnShieldChanged?.Invoke(_currentShield, maxShield);
        }

        if (damage <= 0f)
        {
            OnDamaged?.Invoke(amount, source);
            return;
        }

        _currentHealth = Mathf.Max(_currentHealth - damage, 0f);
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        OnDamaged?.Invoke(amount, source);

        if (_currentHealth <= 0f)
            StartCoroutine(DieRoutine());
    }

    public void Heal(float amount)
    {
        if (IsDead) return;

        float healed = Mathf.Min(amount, maxHealth - _currentHealth);
        if (healed <= 0f) return;

        _currentHealth += healed;
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        OnHealed?.Invoke(healed);
    }

    public void FullHeal()
    {
        _currentHealth = maxHealth;
        if (useShield) _currentShield = maxShield;
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        OnShieldChanged?.Invoke(_currentShield, maxShield);
    }

    public void Kill(GameObject source = null)
    {
        TakeDamage(_currentHealth + _currentShield + 9999f, source);
    }


    IEnumerator DieRoutine()
    {
        IsDead = true;

        if (animator != null && !string.IsNullOrEmpty(deathAnimTrigger))
            animator.SetTrigger(deathAnimTrigger);

        if (deathVFXPrefab != null)
        {
            GameObject vfx = Instantiate(deathVFXPrefab, transform.position, transform.rotation);
            Destroy(vfx, deathVFXDestroyTime);
        }

        yield return new WaitForSeconds(deathEventDelay);

        OnDeath?.Invoke(gameObject);
        onDeathUnityEvent?.Invoke();

        if (destroyOnDeath)
        {
            yield return new WaitForSeconds(destroyDelay);
            Destroy(gameObject);
        }
    }
    public void SetHealthDirectly(float amount)
    {
        _currentHealth = Mathf.Clamp(amount, 0f, maxHealth);
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
    }

    public void SetShieldDirectly(float amount)
    {
        _currentShield = Mathf.Clamp(amount, 0f, maxShield);
        OnShieldChanged?.Invoke(_currentShield, maxShield);
    }

    public void Revive()
    {
        IsDead = false;
        _currentHealth = maxHealth;
        _currentShield = useShield ? maxShield : 0f;
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        if (useShield) OnShieldChanged?.Invoke(_currentShield, maxShield);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (damageTypeMultipliers == null || damageTypeMultipliers.Length != (int)DamageType.Count)
        {
            float[] old = damageTypeMultipliers;
            damageTypeMultipliers = new float[(int)DamageType.Count];
            for (int i = 0; i < damageTypeMultipliers.Length; i++)
                damageTypeMultipliers[i] = (old != null && i < old.Length) ? old[i] : 1f;
        }
    }
#endif
}


public enum DamageType
{
    Physical = 0,
    Fire     = 1,
    Ice      = 2,
    Electric = 3,
    Poison   = 4,
    Count    = 5
}
