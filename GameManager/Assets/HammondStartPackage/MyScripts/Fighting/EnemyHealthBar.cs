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

        if (saveHealth)
            SaveManager.SetInt($"{SaveID}/DeadAtRest", SaveManager.GetInt("_meta/restCount", 0));
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

        if (isDead && !SaveManager.HasKey($"{SaveID}/DeadAtRest"))
            SaveManager.SetInt($"{SaveID}/DeadAtRest", SaveManager.GetInt("_meta/restCount", 0));

        Transform root = healthSystem.transform;
        SaveManager.SetVector3($"{SaveID}/Position", root.position);
        SaveManager.SetVector3($"{SaveID}/Rotation", root.eulerAngles);
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

        if (!SaveManager.HasKey($"{SaveID}/Health"))
        {
            healthSystem.gameObject.SetActive(true);
            healthSystem.GetComponent<EnemyAITemp>()?.Revive();
            healthSystem.Revive();
            _lastHealth = healthSystem.MaxHealth;
            yield return null;
            SnapBar(healthBar, 1f);
            _isLoading = false;
            yield break;
        }

        bool dead = SaveManager.GetBool($"{SaveID}/Dead", false);

        if (dead)
        {
            int deadAtRest = SaveManager.GetInt($"{SaveID}/DeadAtRest", 0);
            int currentRest = SaveManager.GetInt("_meta/restCount", 0);
            Debug.Log($"{SaveID} dead:{dead} deadAtRest:{SaveManager.GetInt($"{SaveID}/DeadAtRest", -999)} currentRest:{SaveManager.GetInt("_meta/restCount", 0)} slot:{SaveManager.ActiveSlot}");

            if (deadAtRest < currentRest)
                dead = false;
        }

        if (dead)
        {
            healthSystem.GetComponent<EnemyAITemp>()?.DieSilently();
            healthSystem.gameObject.SetActive(false);
            _isLoading = false;
        }
        else
        {
            healthSystem.gameObject.SetActive(true);
            healthSystem.GetComponent<EnemyAITemp>()?.Revive();
            _isLoading = true;
            healthSystem.SetHealthDirectly(healthSystem.MaxHealth);
            _lastHealth = healthSystem.MaxHealth;
            yield return null;
            SnapBar(healthBar, 1f);
            _isLoading = false;
        }
    }
}