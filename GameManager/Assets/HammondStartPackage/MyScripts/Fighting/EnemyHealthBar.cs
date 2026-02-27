using UnityEngine;
using System.Collections;
using INab.UI;

public class EnemyHealthBar : MonoBehaviour, ISaveable
{
    [Header("references")]
    public HealthSystem healthSystem;
    public ProceduralProgressBar healthBar;
    public CanvasGroup canvasGroup;

    [Header("settings")]
    public float fadeDelay = 3f;
    public float fadeDuration = 0.5f;
    public bool faceCamera = true;

    [Header("save")]
    public bool saveHealth = true;
    public string enemyID = "";

    Coroutine _fadeRoutine;
    float _lastHealth;
    bool _isLoading;

    public string SaveID => $"{gameObject.scene.name}/Enemy/{enemyID}";

    void Awake()
    {
        _isLoading = true;
        if (healthSystem == null)
            healthSystem = GetComponentInParent<HealthSystem>();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;

        _lastHealth = healthSystem != null ? healthSystem.CurrentHealth : 0f;

        if (string.IsNullOrEmpty(enemyID))
        {
            Transform root = healthSystem != null ? healthSystem.transform : transform;
            enemyID = $"{root.gameObject.name}_{root.position.x:F0}_{root.position.z:F0}";
        }

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
        healthSystem.OnDamaged += OnDamaged;
        healthSystem.OnHealthChanged += OnHealthChanged;
        healthSystem.OnDeath += OnDeath;
    }

    void OnDisable()
    {
        if (healthSystem == null) return;
        healthSystem.OnDamaged -= OnDamaged;
        healthSystem.OnHealthChanged -= OnHealthChanged;
        healthSystem.OnDeath -= OnDeath;
    }

    void LateUpdate()
    {
        if (faceCamera && Camera.main != null)
            transform.forward = Camera.main.transform.forward;
    }

    void OnDamaged(float amount, GameObject source) => ShowBar();

    void OnDeath(GameObject killer)
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        canvasGroup.alpha = 0f;
    }

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

    void ShowBar()
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        canvasGroup.alpha = 1f;
        _fadeRoutine = StartCoroutine(FadeAfterDelay());
    }

    void SnapBar(ProceduralProgressBar bar, float value)
    {
        if (bar == null) return;
        bar.FillAmount = value;
        bar.MainBarFillAmount = value;
        bar.TargetFillAmount = value;
        bar.UpdateBarFillAmount(value);
    }

    IEnumerator FadeAfterDelay()
    {
        yield return new WaitForSeconds(fadeDelay);
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        _fadeRoutine = null;
    }

    public void OnSave()
    {
        if (!saveHealth || healthSystem == null) return;

        bool isDead = healthSystem.IsDead || !healthSystem.gameObject.activeSelf;

        SaveManager.SetFloat($"{SaveID}/Health", healthSystem.CurrentHealth);
        SaveManager.SetBool($"{SaveID}/Dead", isDead);

        Transform root = healthSystem.transform;
        SaveManager.SetVector3($"{SaveID}/Position", root.position);
        SaveManager.SetVector3($"{SaveID}/Rotation", root.eulerAngles);
    }

    public void OnLoad()
    {
        if (!saveHealth || healthSystem == null) return;

        if (!SaveManager.HasKey($"{SaveID}/Health"))
            healthSystem.gameObject.SetActive(true);

        StartCoroutine(ApplyLoadNextFrame());
    }

    IEnumerator ApplyLoadNextFrame()
    {
        yield return null;
        _isLoading = true;

        yield return null;
        _isLoading = true;

        bool hasKey = SaveManager.HasKey($"{SaveID}/Health");
        bool dead2 = SaveManager.GetBool($"{SaveID}/Dead", false);
        Debug.Log($"{SaveID} haskey: {hasKey}, dead: {dead2}");

        if (!SaveManager.HasKey($"{SaveID}/Health"))
        {
            healthSystem.gameObject.SetActive(true);
            EnemyAITemp ai = healthSystem.GetComponent<EnemyAITemp>();
            ai?.Revive();
            healthSystem.Revive();
            _lastHealth = healthSystem.MaxHealth;
            yield return null; 
            SnapBar(healthBar, 1f);
            _isLoading = false;
            yield break;
        }

        float health = SaveManager.GetFloat($"{SaveID}/Health", healthSystem.MaxHealth);
        bool dead = SaveManager.GetBool($"{SaveID}/Dead", false);

        Transform root = healthSystem.transform;
        root.position = SaveManager.GetVector3($"{SaveID}/Position", root.position);
        root.rotation = Quaternion.Euler(SaveManager.GetVector3($"{SaveID}/Rotation", root.eulerAngles));

        if (dead)
        {
            EnemyAITemp ai = healthSystem.GetComponent<EnemyAITemp>();
            ai?.DieSilently();
            healthSystem.gameObject.SetActive(false);
        }
        else
        {
            _isLoading = true;
            healthSystem.SetHealthDirectly(health);
            _lastHealth = health;
            yield return null; 
            SnapBar(healthBar, health / healthSystem.MaxHealth);
            _isLoading = false;
        }
    }
}