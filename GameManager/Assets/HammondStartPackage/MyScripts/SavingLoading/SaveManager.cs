using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public interface ISaveable
{
    string SaveID { get; }
    void OnSave();
    void OnLoad();
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public int maxSaveSlots = 3;
    public int defaultSlot = 0;

    public bool autoSaveOnQuit = true;
    public bool autoSaveOnSceneChange = true;
    public bool debugLogging = true;

    public Button saveButton;
    public float saveCooldown = 4f;
    public TextMeshProUGUI feedbackText;
    public float feedbackFadeDelay = 1.5f;
    public float feedbackFadeDuration = 0.5f;

    public static int ActiveSlot { get; private set; }
    static string SlotPath(int slot) =>
        System.IO.Path.Combine(Application.persistentDataPath, $"SaveSlot_{slot}.json");
    static string ActiveSlotPath => SlotPath(ActiveSlot);

    static Dictionary<string, string> _data = new Dictionary<string, string>();
    static List<ISaveable> _saveables = new List<ISaveable>();
    static bool _isLoadingScene = false;

    Coroutine _feedbackRoutine;
    bool _saveCooldownActive;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ActiveSlot = defaultSlot;
        Load();

        SceneManager.sceneLoaded += OnSceneLoaded;

        if (feedbackText != null)
            feedbackText.text = "";

        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveButtonPressed);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    void OnApplicationQuit()
    {
        if (autoSaveOnQuit)
            CollectAndSave("quit");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Title") return;
        StartCoroutine(LoadSaveablesNextFrame(scene.name));
    }

    IEnumerator LoadSaveablesNextFrame(string sceneName)
    {
        _isLoadingScene = true;
        yield return null;

        ISaveable[] snapshot = _saveables.ToArray();

        foreach (ISaveable saveable in snapshot)
        {
            if (saveable == null) continue;
            if (!(saveable is MonoBehaviour mb) || mb == null || mb.gameObject == null) continue;
            try { saveable.OnLoad(); }
            catch (System.Exception e) { Debug.LogError($"onload threw on '{saveable.SaveID}': {e}"); }
        }

        _isLoadingScene = false;

        if (debugLogging)
            Debug.Log($"loaded scene '{sceneName}', slot {ActiveSlot}, {snapshot.Length} saveables");
    }

    public static void SetActiveSlot(int slot)
    {
        if (slot < 0 || (Instance != null && slot >= Instance.maxSaveSlots))
        {
            Debug.LogWarning($"Slot {slot} out of range");
            return;
        }

        if (Instance != null)
            Instance.CollectAndSave("pre slot-switch");

        ResetRuntime();

        ActiveSlot = slot;
        Instance.Load();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        if (Instance != null && Instance.debugLogging)
            Debug.Log($"switched to slot {slot}. {_data.Count} keys");
    }

    public static bool SlotHasData(int slot) =>
        System.IO.File.Exists(SlotPath(slot));

    public static string GetSlotInfo(int slot)
    {
        if (!System.IO.File.Exists(SlotPath(slot))) return "Empty";

        string json = System.IO.File.ReadAllText(SlotPath(slot));
        if (string.IsNullOrEmpty(json)) return "Empty";

        var wrapper = JsonUtility.FromJson<SaveWrapper>(json);
        var dict = wrapper.ToDictionary();

        if (dict.TryGetValue("_meta/timestamp", out string timestamp))
            return $"Slot {slot + 1}  {timestamp}";

        return $"Slot {slot + 1} {dict.Count} entries";
    }

    public static void Set(string key, string value) => _data[key] = value;
    public static void SetInt(string key, int value) => _data[key] = value.ToString();
    public static void SetFloat(string key, float value) => _data[key] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public static void SetBool(string key, bool value) => _data[key] = value ? "1" : "0";
    public static void SetVector3(string key, Vector3 v) => _data[key] =
        $"{v.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},{v.y.ToString(System.Globalization.CultureInfo.InvariantCulture)},{v.z.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

    public static string Get(string key, string def = "") =>
        _data.TryGetValue(key, out string val) ? val : def;

    public static int GetInt(string key, int def = 0)
    {
        if (_data.TryGetValue(key, out string val) && int.TryParse(val, out int r)) return r;
        return def;
    }

    public static float GetFloat(string key, float def = 0f)
    {
        if (_data.TryGetValue(key, out string val) &&
            float.TryParse(val, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float r)) return r;
        return def;
    }

    public static bool GetBool(string key, bool def = false)
    {
        if (_data.TryGetValue(key, out string val)) return val == "1";
        return def;
    }

    public static Vector3 GetVector3(string key, Vector3 def = default)
    {
        if (!_data.TryGetValue(key, out string val)) return def;
        string[] p = val.Split(',');
        if (p.Length != 3) return def;
        if (float.TryParse(p[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
            float.TryParse(p[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y) &&
            float.TryParse(p[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z))
            return new Vector3(x, y, z);
        return def;
    }

    public static void DeleteKeysEndingWith(string suffix)
    {
        var toRemove = new List<string>();
        foreach (string key in _data.Keys)
            if (key.EndsWith(suffix)) toRemove.Add(key);
        foreach (string key in toRemove)
            _data.Remove(key);
    }

    public static bool HasKey(string key) => _data.ContainsKey(key);
    public static void DeleteKey(string key) => _data.Remove(key);

    public static void DeleteByPrefix(string prefix)
    {
        if (string.IsNullOrEmpty(prefix)) return;
        var toRemove = new List<string>();
        foreach (string key in _data.Keys)
            if (key.StartsWith(prefix)) toRemove.Add(key);
        foreach (string key in toRemove)
            _data.Remove(key);
    }

    public static void DeleteAll()
    {
        _data.Clear();
        if (Instance != null) Instance.Save();
    }

    public static void SaveBeforeSceneChange()
    {
        if (Instance != null && Instance.autoSaveOnSceneChange)
            Instance.CollectAndSaveForced("scene change");
    }

    public static void ForceSave()
    {
        if (Instance != null)
            Instance.CollectAndSaveForced("force");
    }

    public static void Register(ISaveable saveable)
    {
        if (!_saveables.Contains(saveable))
        {
            _saveables.Add(saveable);
            if (Instance != null && Instance.debugLogging)
                Debug.Log($"registered: {saveable.SaveID}");
        }
    }

    public static void Unregister(ISaveable saveable)
    {
        _saveables.Remove(saveable);
    }

    void OnSaveButtonPressed()
    {
        if (_saveCooldownActive) return;
        CollectAndSaveForced("manual");
        ShowFeedback("game saved");
        StartCoroutine(SaveCooldownRoutine());
    }

    IEnumerator SaveCooldownRoutine()
    {
        _saveCooldownActive = true;
        if (saveButton != null) saveButton.interactable = false;

        yield return new WaitForSecondsRealtime(saveCooldown);

        _saveCooldownActive = false;
        if (saveButton != null) saveButton.interactable = true;
    }

    public void ShowFeedback(string message)
    {
        if (feedbackText == null) return;
        if (_feedbackRoutine != null) StopCoroutine(_feedbackRoutine);
        _feedbackRoutine = StartCoroutine(FeedbackRoutine(message));
    }

    IEnumerator FeedbackRoutine(string message)
    {
        feedbackText.text = message;
        feedbackText.alpha = 1f;

        yield return new WaitForSecondsRealtime(feedbackFadeDelay);

        float elapsed = 0f;
        while (elapsed < feedbackFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            feedbackText.alpha = Mathf.Lerp(1f, 0f, elapsed / feedbackFadeDuration);
            yield return null;
        }

        feedbackText.alpha = 0f;
        feedbackText.text = "";
        _feedbackRoutine = null;
    }

    void CollectAndSave(string reason)
    {
        if (IsMenuScene()) return;
        if (_isLoadingScene) return;
        DoSave(reason);
    }

    void CollectAndSaveForced(string reason)
    {
        if (IsMenuScene()) return;
        DoSave(reason);
    }

    void DoSave(string reason)
    {
        _data["_meta/timestamp"] = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        _data["_meta/scene"] = SceneManager.GetActiveScene().name;

        ISaveable[] snapshot = _saveables.ToArray();

        int saved = 0;
        foreach (ISaveable saveable in snapshot)
        {
            if (saveable == null) continue;
            if (!(saveable is MonoBehaviour mb) || mb == null || mb.gameObject == null) continue;
            try { saveable.OnSave(); saved++; }
            catch (System.Exception e) { Debug.LogError($"onsave threw on '{saveable.SaveID}': {e}"); }
        }

        Save();

        if (debugLogging)
            Debug.Log($"saved {saved} saveables to slot {ActiveSlot}. reason: {reason}. keys: {_data.Count}");
    }

    void Save()
    {
        string json = JsonUtility.ToJson(new SaveWrapper(_data), true);
        System.IO.File.WriteAllText(ActiveSlotPath, json);
    }

    void Load()
    {
        if (!System.IO.File.Exists(ActiveSlotPath))
        {
            _data = new Dictionary<string, string>();
            return;
        }

        string json = System.IO.File.ReadAllText(ActiveSlotPath);
        if (string.IsNullOrEmpty(json)) { _data = new Dictionary<string, string>(); return; }

        SaveWrapper wrapper = JsonUtility.FromJson<SaveWrapper>(json);
        _data = wrapper.ToDictionary();

        if (debugLogging)
            Debug.Log($"loaded slot {ActiveSlot} from {ActiveSlotPath}: {_data.Count} keys");
    }

    bool IsMenuScene()
    {
        string current = SceneManager.GetActiveScene().name;
        return current == "Title";
    }

    [System.Serializable]
    class SaveWrapper
    {
        public string[] keys;
        public string[] values;

        public SaveWrapper() { }

        public SaveWrapper(Dictionary<string, string> dict)
        {
            keys = new string[dict.Count];
            values = new string[dict.Count];
            int i = 0;
            foreach (var kvp in dict) { keys[i] = kvp.Key; values[i] = kvp.Value; i++; }
        }

        public Dictionary<string, string> ToDictionary()
        {
            var dict = new Dictionary<string, string>();
            if (keys == null || values == null) return dict;
            for (int i = 0; i < keys.Length && i < values.Length; i++)
                dict[keys[i]] = values[i];
            return dict;
        }
    }

    public static void DeleteSlot(int slot)
    {
        string path = SlotPath(slot);
        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);

        if (slot == ActiveSlot)
        {
            ResetRuntime();
            _data.Clear();
            Instance?.Save();
        }

        if (Instance != null && Instance.debugLogging)
            Debug.Log($"deleted slot {slot}");
    }

    public static void ResetRuntime()
    {
        InventoryManager.Instance?.ResetInventory();

        foreach (WorldItem wi in Resources.FindObjectsOfTypeAll<WorldItem>())
        {
            if (wi == null) continue;
            if (wi.gameObject.scene.name == "DontDestroyOnLoad")
                Object.Destroy(wi.gameObject);
        }

        foreach (WorldItem wi in Resources.FindObjectsOfTypeAll<WorldItem>())
        {
            if (wi == null) continue;
            wi.ResetToWorld();
        }

        string activeScene = SceneManager.GetActiveScene().name;
        foreach (ISaveable saveable in _saveables.ToArray())
        {
            if (saveable == null) continue;
            if (saveable is WorldItem) continue;
            if (!(saveable is MonoBehaviour mb) || mb == null || mb.gameObject == null) continue;
            if (mb.gameObject.scene.name != activeScene) continue;
            try { saveable.OnLoad(); }
            catch (System.Exception e) { Debug.LogError($"resetruntime onload threw on '{saveable.SaveID}': {e}"); }
        }
    }
}