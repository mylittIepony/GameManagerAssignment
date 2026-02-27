using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using INab.UI;

public class PlayerHealthBar : MonoBehaviour, ISaveable
{
    [Header("references")]
    public HealthSystem healthSystem;
    public ProceduralProgressBar healthBar;
    public ProceduralProgressBar shieldBar;

    [Header("damage flash")]
    public Image screenFlashImage;
    public float flashDuration = 0.2f;
    public Color flashColour = new Color(1f, 0f, 0f, 0.3f);

    [Header("save")]
    public bool saveHealth = true;

    public string SaveID => "Player/Health";

    Coroutine _flashRoutine;
    float _lastHealth;
    float _lastShield;
    bool _isLoading;

    void Awake()
    {
        if (healthSystem == null)
            healthSystem = FindFirstObjectByType<HealthSystem>();

        if (healthSystem != null)
        {
            _lastHealth = healthSystem.CurrentHealth;
            _lastShield = healthSystem.CurrentShield;
        }

        if (screenFlashImage != null)
        {
            var c = flashColour;
            c.a = 0f;
            screenFlashImage.color = c;
        }

        if (shieldBar != null)
            shieldBar.gameObject.SetActive(healthSystem != null && healthSystem.useShield);

        if (saveHealth)
            SaveManager.Register(this);
    }

    void OnDestroy()
    {
        if (saveHealth)
            SaveManager.Unregister(this);
    }

    void OnEnable()
    {
        if (healthSystem == null) return;
        healthSystem.OnHealthChanged += OnHealthChanged;
        healthSystem.OnShieldChanged += OnShieldChanged;
        healthSystem.OnDamaged += OnDamaged;
    }

    void OnDisable()
    {
        if (healthSystem == null) return;
        healthSystem.OnHealthChanged -= OnHealthChanged;
        healthSystem.OnShieldChanged -= OnShieldChanged;
        healthSystem.OnDamaged -= OnDamaged;
    }

    void Start() { }

    void OnHealthChanged(float current, float max)
    {
        if (_isLoading || healthBar == null) return;

        float newFill = current / max;
        float oldFill = _lastHealth / max;
        float delta = Mathf.Abs(newFill - oldFill);

        if (current < _lastHealth)
            healthBar.BarLoss(delta);
        else
            healthBar.BarFill(delta);

        _lastHealth = current;
    }

    void OnShieldChanged(float current, float max)
    {
        if (_isLoading || shieldBar == null) return;

        float newFill = current / max;
        float oldFill = _lastShield / max;
        float delta = Mathf.Abs(newFill - oldFill);

        if (current < _lastShield)
            shieldBar.BarLoss(delta);
        else
            shieldBar.BarFill(delta);

        _lastShield = current;
    }

    void OnDamaged(float amount, GameObject source) => TriggerFlash();

    void TriggerFlash()
    {
        if (screenFlashImage == null) return;
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        screenFlashImage.color = flashColour;
        float elapsed = 0f;
        Color end = flashColour;
        end.a = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            screenFlashImage.color = Color.Lerp(flashColour, end, elapsed / flashDuration);
            yield return null;
        }
        screenFlashImage.color = end;
        _flashRoutine = null;
    }

    void SnapBar(ProceduralProgressBar bar, float value)
    {
        if (bar == null) return;
        bar.FillAmount = value;
        bar.MainBarFillAmount = value;
        bar.TargetFillAmount = value;
        bar.UpdateBarFillAmount(value);
    }

    public void OnSave()
    {
        if (!saveHealth || healthSystem == null) return;
        SaveManager.SetFloat($"{SaveID}/Current", healthSystem.CurrentHealth);
        SaveManager.SetFloat($"{SaveID}/Shield", healthSystem.CurrentShield);
    }

    public void OnLoad()
    {
        if (!saveHealth || healthSystem == null) return;
        StartCoroutine(ApplyLoadNextFrame());
    }

    IEnumerator ApplyLoadNextFrame()
    {
        yield return null;

        _isLoading = true;

        if (!SaveManager.HasKey($"{SaveID}/Current"))
        {
            _isLoading = true;
            _lastHealth = healthSystem.MaxHealth;
            _lastShield = healthSystem.MaxShield;
            healthSystem.SetHealthDirectly(healthSystem.MaxHealth);
            SnapBar(healthBar, 1f);
            if (healthSystem.useShield)
            {
                healthSystem.SetShieldDirectly(healthSystem.MaxShield);
                SnapBar(shieldBar, 1f);
            }
            _isLoading = false;
            yield break;
        }

        float health = SaveManager.GetFloat($"{SaveID}/Current", healthSystem.MaxHealth);
        float shield = SaveManager.GetFloat($"{SaveID}/Shield", healthSystem.MaxShield);

        _isLoading = true;
        healthSystem.SetHealthDirectly(health);
        _lastHealth = health;
        SnapBar(healthBar, health / healthSystem.MaxHealth);

        if (healthSystem.useShield)
        {
            healthSystem.SetShieldDirectly(shield);
            _lastShield = shield;
            SnapBar(shieldBar, shield / healthSystem.MaxShield);
        }
        _isLoading = false;
    }
}