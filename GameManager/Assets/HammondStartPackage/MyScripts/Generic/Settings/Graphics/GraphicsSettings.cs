using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class GraphicsSettingsUser : MonoBehaviour
{
    public static GraphicsSettingsUser Instance { get; private set; }

    [Header("urp assets")]
    [SerializeField] UniversalRenderPipelineAsset lowQuality;
    [SerializeField] UniversalRenderPipelineAsset mediumQuality;
    [SerializeField] UniversalRenderPipelineAsset highQuality;

    [Header("defaults")]
    public int defaultQualityLevel = 2;
    public int defaultTargetFPS = 60;
    public bool defaultVSync = false;
    public int defaultShadowQuality = 2;
    public int defaultTextureQuality = 2;
    public int defaultAntiAliasing = 2;
    public float defaultRenderScale = 1f;
    public bool defaultPostProcessing = true;

    const string KeyQuality = "GFX_Quality";
    const string KeyFPS = "GFX_TargetFPS";
    const string KeyVSync = "GFX_VSync";
    const string KeyShadows = "GFX_Shadows";
    const string KeyTextures = "GFX_Textures";
    const string KeyAA = "GFX_AntiAliasing";
    const string KeyRenderScale = "GFX_RenderScale";
    const string KeyPostProcessing = "GFX_PostProcessing";
    const string KeyResWidth = "GFX_ResWidth";
    const string KeyResHeight = "GFX_ResHeight";
    const string KeyFullscreen = "GFX_Fullscreen";
    const string KeyInitialized = "GFX_Initialized";

    public int CurrentQuality { get; private set; }
    public int CurrentTargetFPS { get; private set; }
    public bool CurrentVSync { get; private set; }
    public int CurrentShadowQuality { get; private set; }
    public int CurrentTextureQuality { get; private set; }
    public int CurrentAntiAliasing { get; private set; }
    public float CurrentRenderScale { get; private set; }
    public bool CurrentPostProcessing { get; private set; }

    public event Action OnSettingsChanged;

    public static readonly int[] FPSOptions = { 30, 60, 90, 120, 144, 165, 240, -1 };


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSettings();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void InitializeSettings()
    {
        bool firstLaunch = PlayerPrefs.GetInt(KeyInitialized, 0) == 0;

        if (firstLaunch)
        {
            AutoDetectAndApplyAll();
            PlayerPrefs.SetInt(KeyInitialized, 1);
            PlayerPrefs.Save();
        }
        else
        {
            RestoreAllSettings();
        }
    }


    void AutoDetectAndApplyAll()
    {
        int tier = DetectHardwareTier();

        SetQualityPreset(tier);
        SetShadowQuality(tier);
        SetTextureQuality(tier);
        SetAntiAliasing(tier);
        SetRenderScale(tier == 0 ? 0.75f : tier == 1 ? 0.85f : 1f);
        SetPostProcessing(tier >= 1);
        SetTargetFramerate(defaultTargetFPS);
        SetVSync(defaultVSync);

        SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, Screen.fullScreenMode);

        PlayerPrefs.Save();
    }

    int DetectHardwareTier()
    {
        int score = 0;

        int vram = SystemInfo.graphicsMemorySize;
        if (vram >= 6000) score += 3;
        else if (vram >= 3000) score += 2;
        else if (vram >= 1500) score += 1;

        int cores = SystemInfo.processorCount;
        if (cores >= 8) score += 2;
        else if (cores >= 4) score += 1;

        int ram = SystemInfo.systemMemorySize;
        if (ram >= 16000) score += 2;
        else if (ram >= 8000) score += 1;

        if (SystemInfo.graphicsShaderLevel >= 50) score += 1;

        if (score >= 6) return 2; 
        if (score >= 3) return 1; 
        return 0;                 
    }


    void RestoreAllSettings()
    {

        ApplyQualityPreset(PlayerPrefs.GetInt(KeyQuality, defaultQualityLevel));

        ApplyShadowQuality(PlayerPrefs.GetInt(KeyShadows, defaultShadowQuality));
        ApplyTextureQuality(PlayerPrefs.GetInt(KeyTextures, defaultTextureQuality));
        ApplyAntiAliasing(PlayerPrefs.GetInt(KeyAA, defaultAntiAliasing));
        ApplyRenderScale(PlayerPrefs.GetFloat(KeyRenderScale, defaultRenderScale));
        ApplyPostProcessing(PlayerPrefs.GetInt(KeyPostProcessing, defaultPostProcessing ? 1 : 0) == 1);
        ApplyVSync(PlayerPrefs.GetInt(KeyVSync, defaultVSync ? 1 : 0) == 1);
        ApplyTargetFramerate(PlayerPrefs.GetInt(KeyFPS, defaultTargetFPS));

        int w = PlayerPrefs.GetInt(KeyResWidth, Screen.currentResolution.width);
        int h = PlayerPrefs.GetInt(KeyResHeight, Screen.currentResolution.height);
        FullScreenMode mode = (FullScreenMode)PlayerPrefs.GetInt(KeyFullscreen, (int)Screen.fullScreenMode);
        Screen.SetResolution(w, h, mode);
    }



    public void SetQualityPreset(int level)
    {
        level = Mathf.Clamp(level, 0, 2);
        ApplyQualityPreset(level);
        PlayerPrefs.SetInt(KeyQuality, level);
        Save();
    }

    public void SetShadowQuality(int quality)
    {
        quality = Mathf.Clamp(quality, 0, 2);
        ApplyShadowQuality(quality);
        PlayerPrefs.SetInt(KeyShadows, quality);
        Save();
    }

    public void SetTextureQuality(int quality)
    {
        quality = Mathf.Clamp(quality, 0, 2);
        ApplyTextureQuality(quality);
        PlayerPrefs.SetInt(KeyTextures, quality);
        Save();
    }


    public void SetAntiAliasing(int quality)
    {
        quality = Mathf.Clamp(quality, 0, 2);
        ApplyAntiAliasing(quality);
        PlayerPrefs.SetInt(KeyAA, quality);
        Save();
    }

    public void SetRenderScale(float scale)
    {
        scale = Mathf.Clamp(scale, 0.5f, 2f);
        ApplyRenderScale(scale);
        PlayerPrefs.SetFloat(KeyRenderScale, scale);
        Save();
    }

    public void SetPostProcessing(bool enabled)
    {
        ApplyPostProcessing(enabled);
        PlayerPrefs.SetInt(KeyPostProcessing, enabled ? 1 : 0);
        Save();
    }

    public void SetTargetFramerate(int fps)
    {
        ApplyTargetFramerate(fps);
        PlayerPrefs.SetInt(KeyFPS, fps);
        Save();
    }

    public void SetVSync(bool enabled)
    {
        ApplyVSync(enabled);
        PlayerPrefs.SetInt(KeyVSync, enabled ? 1 : 0);
        Save();
    }

    public void SetResolution(int width, int height, FullScreenMode mode)
    {
        Screen.SetResolution(width, height, mode);
        PlayerPrefs.SetInt(KeyResWidth, width);
        PlayerPrefs.SetInt(KeyResHeight, height);
        PlayerPrefs.SetInt(KeyFullscreen, (int)mode);
        Save();
    }

    public void OnFPSDropdownChanged(int index)
    {
        if (index >= 0 && index < FPSOptions.Length)
            SetTargetFramerate(FPSOptions[index]);
    }

    public void OnQualityDropdownChanged(int index)
    {
        SetQualityPreset(index);
    }


    public void ResetToDefaults()
    {
        PlayerPrefs.DeleteKey(KeyInitialized);
        AutoDetectAndApplyAll();
        PlayerPrefs.SetInt(KeyInitialized, 1);
        PlayerPrefs.Save();
    }


    void ApplyQualityPreset(int level)
    {
        CurrentQuality = level;

        UniversalRenderPipelineAsset asset = level switch
        {
            0 => lowQuality,
            1 => mediumQuality,
            2 => highQuality,
            _ => highQuality
        };

        if (asset != null)
            QualitySettings.renderPipeline = asset;
    }

    void ApplyShadowQuality(int quality)
    {
        CurrentShadowQuality = quality;

        var urp = GetActiveURPAsset();
        if (urp == null) return;

        switch (quality)
        {
            case 0:
                urp.shadowDistance = 25f;
                urp.shadowCascadeCount = 1;
                break;
            case 1:
                urp.shadowDistance = 60f;
                urp.shadowCascadeCount = 2;
                break;
            case 2:
                urp.shadowDistance = 120f;
                urp.shadowCascadeCount = 4;
                break;
        }
    }

    void ApplyTextureQuality(int quality)
    {
        CurrentTextureQuality = quality;
        QualitySettings.globalTextureMipmapLimit = 2 - quality;
    }

    void ApplyAntiAliasing(int quality)
    {
        CurrentAntiAliasing = quality;

        var urp = GetActiveURPAsset();
        if (urp == null) return;

        urp.msaaSampleCount = quality switch
        {
            0 => 1,
            1 => 2,
            2 => 4,
            _ => 4
        };
    }

    void ApplyRenderScale(float scale)
    {
        CurrentRenderScale = scale;

        var urp = GetActiveURPAsset();
        if (urp == null) return;

        urp.renderScale = scale;
    }

    void ApplyPostProcessing(bool enabled)
    {
        CurrentPostProcessing = enabled;

        foreach (Volume vol in FindObjectsByType<Volume>(FindObjectsSortMode.None))
            vol.enabled = enabled;
    }

    void ApplyTargetFramerate(int fps)
    {
        CurrentTargetFPS = fps;
        Application.targetFrameRate = fps;
    }

    void ApplyVSync(bool enabled)
    {
        CurrentVSync = enabled;
        QualitySettings.vSyncCount = enabled ? 1 : 0;
    }



    UniversalRenderPipelineAsset GetActiveURPAsset()
    {
        return QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
    }

    void Save()
    {
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    public static List<Resolution> GetFilteredResolutions()
    {
        List<Resolution> filtered = new List<Resolution>();
        HashSet<string> seen = new HashSet<string>();

        foreach (Resolution res in Screen.resolutions)
        {
            if (res.width < 800 || res.height < 600) continue;

            string key = $"{res.width}x{res.height}";
            if (seen.Contains(key)) continue;
            seen.Add(key);

            filtered.Add(res);
        }

        return filtered;
    }

    public void SetupFPSDropdown(TMP_Dropdown dropdown)
    {
        dropdown.ClearOptions();
        List<string> options = new List<string>();
        int selectedIndex = 0;

        for (int i = 0; i < FPSOptions.Length; i++)
        {
            options.Add(FPSOptions[i] == -1 ? "Uncapped" : $"{FPSOptions[i]} FPS");
            if (FPSOptions[i] == CurrentTargetFPS) selectedIndex = i;
        }

        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(selectedIndex);
        dropdown.onValueChanged.AddListener(OnFPSDropdownChanged);
    }

    public void SetupQualityDropdown(TMP_Dropdown dropdown)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string> { "Low", "Medium", "High" });
        dropdown.SetValueWithoutNotify(CurrentQuality);
        dropdown.onValueChanged.AddListener(OnQualityDropdownChanged);
    }

    public void SetupResolutionDropdown(TMP_Dropdown dropdown)
    {
        List<Resolution> resolutions = GetFilteredResolutions();
        dropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < resolutions.Count; i++)
        {
            Resolution res = resolutions[i];
            options.Add($"{res.width} x {res.height}");

            if (res.width == Screen.width && res.height == Screen.height)
                currentIndex = i;
        }

        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(currentIndex);

        dropdown.onValueChanged.AddListener(index =>
        {
            Resolution r = resolutions[index];
            SetResolution(r.width, r.height, Screen.fullScreenMode);
        });
    }


    public void SetupShadowDropdown(TMP_Dropdown dropdown)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string> { "Low", "Medium", "High" });
        dropdown.SetValueWithoutNotify(CurrentShadowQuality);
        dropdown.onValueChanged.AddListener(SetShadowQuality);
    }

    public void SetupTextureDropdown(TMP_Dropdown dropdown)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string> { "Low", "Medium", "High" });
        dropdown.SetValueWithoutNotify(CurrentTextureQuality);
        dropdown.onValueChanged.AddListener(SetTextureQuality);
    }

    public void SetupAADropdown(TMP_Dropdown dropdown)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string> { "Off", "2x MSAA", "4x MSAA" });
        dropdown.SetValueWithoutNotify(CurrentAntiAliasing);
        dropdown.onValueChanged.AddListener(SetAntiAliasing);
    }

    public void SetupRenderScaleSlider(UnityEngine.UI.Slider slider)
    {
        slider.minValue = 0.5f;
        slider.maxValue = 2f;
        slider.value = CurrentRenderScale;
        slider.onValueChanged.AddListener(SetRenderScale);
    }

    public void SetupPostProcessingToggle(UnityEngine.UI.Toggle toggle)
    {
        toggle.isOn = CurrentPostProcessing;
        toggle.onValueChanged.AddListener(SetPostProcessing);
    }

    public void SetupVSyncToggle(UnityEngine.UI.Toggle toggle)
    {
        toggle.isOn = CurrentVSync;
        toggle.onValueChanged.AddListener(SetVSync);
    }

    public void SetupFullscreenToggle(UnityEngine.UI.Toggle toggle)
    {
        toggle.isOn = Screen.fullScreenMode == FullScreenMode.FullScreenWindow;
        toggle.onValueChanged.AddListener(isOn =>
        {
            var mode = isOn ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            SetResolution(Screen.width, Screen.height, mode);
        });
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        ApplyRenderScale(CurrentRenderScale);
        ApplyShadowQuality(CurrentShadowQuality);
        ApplyAntiAliasing(CurrentAntiAliasing);
        ApplyPostProcessing(CurrentPostProcessing);
    }
}