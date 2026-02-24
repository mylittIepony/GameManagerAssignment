using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class SettingsPanel : MonoBehaviour
{
    [Header("tabs")]
    public TabEntry[] tabs;

    [Header("tab colours")]
    public Color selectedColor = Color.white;
    public Color unselectedColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("roots")]
    public GameObject settingsRoot;
    public GameObject pauseRoot;

    [Header("audio controls")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("graphics controls")]
    public TMP_Dropdown qualityDropdown;
    public TMP_Dropdown fpsDropdown;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown shadowDropdown;
    public TMP_Dropdown textureDropdown;
    public TMP_Dropdown aaDropdown;
    public Slider renderScaleSlider;
    public TextMeshProUGUI renderScaleLabel;
    public Toggle vsyncToggle;
    public Toggle postProcessingToggle;
    public Toggle fullscreenToggle;

    [Header("input controls")]
    public TextMeshProUGUI duplicateSummaryText;
    public Button resetAllBindingsButton;

    int _activeTabIndex = -1;

 
    void Start()
    {
        SetupTabButtons();
        SetupAudioControls();
        SetupGraphicsControls();
        SetupInputControls();
        RefreshAudioUI();

        if (tabs != null && tabs.Length > 0)
            SwitchToTab(0);
    }

    void OnEnable()
    {

        RefreshAudioUI();
        RefreshGraphicsUI();
        RefreshInputUI();
    }


    void SetupTabButtons()
    {
        if (tabs == null) return;

        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i; 
            TabEntry tab = tabs[i];

            if (tab.button != null)
            {
                tab.button.onClick.RemoveAllListeners();
                tab.button.onClick.AddListener(() => SwitchToTab(index));
            }
        }
    }

    public void SwitchToTab(int index)
    {
        if (tabs == null || index < 0 || index >= tabs.Length) return;
        if (index == _activeTabIndex) return;

        _activeTabIndex = index;

        for (int i = 0; i < tabs.Length; i++)
        {
            bool active = (i == index);
            TabEntry tab = tabs[i];

            if (tab.panel != null)
                tab.panel.SetActive(active);

            if (tab.buttonLabel != null)
                tab.buttonLabel.color = active ? selectedColor : unselectedColor;

            if (tab.button != null && tab.buttonImage != null)
                tab.buttonImage.color = active ? selectedColor : unselectedColor;
        }
    }

    void SetupAudioControls()
    {
        if (AudioManager.Instance == null) return;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.onValueChanged.AddListener(v => AudioManager.Instance.MasterVolume = v);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.onValueChanged.AddListener(v => AudioManager.Instance.MusicVolume = v);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.onValueChanged.AddListener(v => AudioManager.Instance.SFXVolume = v);
        }
    }

    void RefreshAudioUI()
    {
        if (AudioManager.Instance == null) return;

        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
        if (musicVolumeSlider != null)
            musicVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.SFXVolume);
    }


    void SetupGraphicsControls()
    {
        if (GraphicsSettingsUser.Instance == null) return;
        GraphicsSettingsUser gfx = GraphicsSettingsUser.Instance;


        if (qualityDropdown != null) gfx.SetupQualityDropdown(qualityDropdown);
        if (fpsDropdown != null) gfx.SetupFPSDropdown(fpsDropdown);
        if (resolutionDropdown != null) gfx.SetupResolutionDropdown(resolutionDropdown);


        if (shadowDropdown != null)
        {
            shadowDropdown.ClearOptions();
            shadowDropdown.AddOptions(new List<string> { "Low", "Medium", "High" });
            shadowDropdown.SetValueWithoutNotify(gfx.CurrentShadowQuality);
            shadowDropdown.onValueChanged.AddListener(v => gfx.SetShadowQuality(v));
        }


        if (textureDropdown != null)
        {
            textureDropdown.ClearOptions();
            textureDropdown.AddOptions(new List<string> { "Low", "Medium", "High" });
            textureDropdown.SetValueWithoutNotify(gfx.CurrentTextureQuality);
            textureDropdown.onValueChanged.AddListener(v => gfx.SetTextureQuality(v));
        }

        if (aaDropdown != null)
        {
            aaDropdown.ClearOptions();
            aaDropdown.AddOptions(new List<string> { "Off", "2x MSAA", "4x MSAA" });
            aaDropdown.SetValueWithoutNotify(gfx.CurrentAntiAliasing);
            aaDropdown.onValueChanged.AddListener(v => gfx.SetAntiAliasing(v));
        }


        if (renderScaleSlider != null)
        {
            renderScaleSlider.minValue = 0.5f;
            renderScaleSlider.maxValue = 2f;
            renderScaleSlider.SetValueWithoutNotify(gfx.CurrentRenderScale);
            renderScaleSlider.onValueChanged.AddListener(v =>
            {
                gfx.SetRenderScale(v);
                if (renderScaleLabel != null)
                    renderScaleLabel.text = $"{Mathf.RoundToInt(v * 100)}%";
            });

            if (renderScaleLabel != null)
                renderScaleLabel.text = $"{Mathf.RoundToInt(gfx.CurrentRenderScale * 100)}%";
        }

        if (vsyncToggle != null)
        {
            vsyncToggle.SetIsOnWithoutNotify(gfx.CurrentVSync);
            vsyncToggle.onValueChanged.AddListener(v => gfx.SetVSync(v));
        }

        if (postProcessingToggle != null)
        {
            postProcessingToggle.SetIsOnWithoutNotify(gfx.CurrentPostProcessing);
            postProcessingToggle.onValueChanged.AddListener(v => gfx.SetPostProcessing(v));
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreenMode == FullScreenMode.FullScreenWindow);
            fullscreenToggle.onValueChanged.AddListener(v =>
            {
                FullScreenMode mode = v ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                gfx.SetResolution(Screen.width, Screen.height, mode);
            });
        }
    }

    void RefreshGraphicsUI()
    {
        if (GraphicsSettingsUser.Instance == null) return;
        GraphicsSettingsUser gfx = GraphicsSettingsUser.Instance;

        if (qualityDropdown != null) qualityDropdown.SetValueWithoutNotify(gfx.CurrentQuality);
        if (shadowDropdown != null) shadowDropdown.SetValueWithoutNotify(gfx.CurrentShadowQuality);
        if (textureDropdown != null) textureDropdown.SetValueWithoutNotify(gfx.CurrentTextureQuality);
        if (aaDropdown != null) aaDropdown.SetValueWithoutNotify(gfx.CurrentAntiAliasing);
        if (renderScaleSlider != null) renderScaleSlider.SetValueWithoutNotify(gfx.CurrentRenderScale);
        if (vsyncToggle != null) vsyncToggle.SetIsOnWithoutNotify(gfx.CurrentVSync);
        if (postProcessingToggle != null) postProcessingToggle.SetIsOnWithoutNotify(gfx.CurrentPostProcessing);
    }


    void SetupInputControls()
    {
        if (InputManager.Instance == null) return;

        if (resetAllBindingsButton != null)
        {
            resetAllBindingsButton.onClick.AddListener(() =>
            {
                InputManager.Instance.ResetAllBindings();
                RefreshInputUI();
            });
        }

        InputManager.Instance.OnBindingsChanged += RefreshInputUI;
    }

    void RefreshInputUI()
    {
        if (InputManager.Instance == null) return;

        if (duplicateSummaryText != null)
        {
            var duplicates = InputManager.Instance.GetAllDuplicates();
            if (duplicates.Count > 0)
            {
                duplicateSummaryText.gameObject.SetActive(true);
                duplicateSummaryText.text = $"{duplicates.Count} binding conflict{(duplicates.Count > 1 ? "s" : "")} detected";
                duplicateSummaryText.color = new Color(1f, 0.4f, 0.3f, 1f);
            }
            else
            {
                duplicateSummaryText.gameObject.SetActive(false);
            }
        }
    }

    void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnBindingsChanged -= RefreshInputUI;
    }



    public void OnlyHideSettingsPanel()
    {
        if (settingsRoot != null) settingsRoot.SetActive(false);
    }

    public void ShowSettings()
    {
        if (settingsRoot != null) settingsRoot.SetActive(true);
        if (pauseRoot != null) pauseRoot.SetActive(false);
    }

    public void HideSettings()
    {
        if (settingsRoot != null) settingsRoot.SetActive(false);
        if (pauseRoot != null) pauseRoot.SetActive(true);
    }




    public void ResetGraphicsToDefaults()
    {
        if (GraphicsSettingsUser.Instance != null)
        {
            GraphicsSettingsUser.Instance.ResetToDefaults();
            RefreshGraphicsUI();
        }
    }


    public void ResetInputToDefaults()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.ResetAllBindings();
            RefreshInputUI();
        }
    }

    [System.Serializable]
    public struct TabEntry
    {
        public string name;
        public GameObject panel;
        public Button button;
        public TextMeshProUGUI buttonLabel;
        public Image buttonImage;
    }
}