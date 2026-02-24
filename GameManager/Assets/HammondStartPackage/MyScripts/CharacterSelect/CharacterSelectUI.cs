using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour
{
    [Header("manager")]
    public CharacterSelectManager manager;

    [Header("character display")]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI characterDescText;
    public Image characterPortrait;
    public TextMeshProUGUI characterIndexText;

    [Header("character navigation buttons")]
    public Button prevCharButton;
    public Button nextCharButton;

    [Header("weapon picker")]
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI weaponDescText;
    public Image weaponPreviewIcon;
    public TextMeshProUGUI weaponIndexText;
    public Button prevWeaponButton;
    public Button nextWeaponButton;
    public Button selectWeaponButton;

    [Header("weapon slots — one image per max slot, starts inactive")]
    public Image[] weaponSlotIcons;

    [Header("customization panel")]
    public GameObject customizationPanel;
    public Transform accessoryRowParent;
    public GameObject accessoryRowPrefab;
    public Transform colourRowParent;
    public GameObject colourRowPrefab;

    [Header("play button")]
    public Button playButton;
    public TextMeshProUGUI playButtonText;

    [Header("nav axis buttons (optional)")]
    [Tooltip("0=character, 1=accessory")]
    public Button[] axisButtons;

    readonly List<AccessoryRowUI> _accessoryRows = new List<AccessoryRowUI>();
    readonly List<ColourRowUI> _colourRows = new List<ColourRowUI>();

    int _currentPreviewWeaponIndex = 0;

    void Awake()
    {
        if (manager == null)
            manager = FindObjectOfType<CharacterSelectManager>();

        if (manager == null)
        {
            Debug.LogError("[characterSelectUI] no characterSelectManager found.");
            return;
        }

        manager.OnCharacterChanged += HandleCharacterChanged;
        manager.OnWeaponLoadoutChanged += HandleWeaponLoadoutChanged;
        manager.OnAccessoryChanged += HandleAccessoryChanged;
        manager.OnCustomizationLoaded += HandleCustomizationLoaded;

        prevCharButton?.onClick.AddListener(() => manager.CycleCharacter(-1));
        nextCharButton?.onClick.AddListener(() => manager.CycleCharacter(1));
        prevWeaponButton?.onClick.AddListener(() => StepWeaponPreview(-1));
        nextWeaponButton?.onClick.AddListener(() => StepWeaponPreview(1));
        selectWeaponButton?.onClick.AddListener(SelectCurrentPreviewWeapon);
        playButton?.onClick.AddListener(() => manager.StartGame());

        for (int i = 0; i < (axisButtons?.Length ?? 0); i++)
        {
            int captured = i;
            axisButtons[i]?.onClick.AddListener(() => manager.SetNavAxis(captured));
        }

        ClearWeaponSlotIcons();
    }

    void OnDestroy()
    {
        if (manager == null) return;
        manager.OnCharacterChanged -= HandleCharacterChanged;
        manager.OnWeaponLoadoutChanged -= HandleWeaponLoadoutChanged;
        manager.OnAccessoryChanged -= HandleAccessoryChanged;
        manager.OnCustomizationLoaded -= HandleCustomizationLoaded;
    }

    void HandleCharacterChanged(CharacterData data, int index, int total)
    {
        if (characterNameText) characterNameText.text = data?.characterName ?? "";
        if (characterDescText) characterDescText.text = data?.description ?? "";
        if (characterPortrait) { characterPortrait.sprite = data?.portrait; characterPortrait.enabled = data?.portrait != null; }
        if (characterIndexText) characterIndexText.text = $"{index + 1} / {total}";

        bool showCustom = data != null && data.useCustomization;
        if (customizationPanel) customizationPanel.SetActive(showCustom);

        if (showCustom)
        {
            RebuildAccessoryRows(data);
            RebuildColourRows();
        }

        _currentPreviewWeaponIndex = 0;
        ClearWeaponSlotIcons();
        RefreshWeaponPreviewDisplay();
    }

    void HandleWeaponLoadoutChanged(WeaponData[] loadout, int count)
    {
        RefreshWeaponSlotIcons(loadout);
        RefreshWeaponPreviewDisplay();
        RefreshPlayButton();
    }

    void RefreshPlayButton()
    {
        if (playButton != null)
            playButton.interactable = manager.CanStartGame();
    }

    void HandleAccessoryChanged(string slotName, AccessoryData data, int index)
    {
        AccessoryRowUI row = _accessoryRows.Find(r => r.slotName == slotName);
        row?.UpdateDisplay(data, index);
    }

    void HandleCustomizationLoaded(CharacterCustomizationSave save)
    {
        CharacterData data = manager.SelectedCharacter;
        if (data != null && data.useCustomization)
            RebuildAccessoryRows(data);

        RefreshWeaponSlotIcons(manager.GetSelectedWeaponData());
        RefreshWeaponPreviewDisplay();
    }

    void StepWeaponPreview(int dir)
    {
        WeaponData[] active = manager.GetActiveWeaponsPublic();
        if (active == null || active.Length == 0) return;

        int total = active.Length;
        _currentPreviewWeaponIndex = (_currentPreviewWeaponIndex + dir + total) % total;

        while (manager.SelectedWeaponIndices.Contains(_currentPreviewWeaponIndex) && manager.SelectedWeaponIndices.Count < total)
        {
            _currentPreviewWeaponIndex = (_currentPreviewWeaponIndex + dir + total) % total;
        }

        RefreshWeaponPreviewDisplay();
    }

    void SelectCurrentPreviewWeapon()
    {
        manager.ToggleWeapon(_currentPreviewWeaponIndex);
    }

    void RefreshWeaponPreviewDisplay()
    {
        WeaponData[] active = manager.GetActiveWeaponsPublic();
        if (active == null || active.Length == 0)
        {
            if (weaponNameText) weaponNameText.text = "";
            if (weaponDescText) weaponDescText.text = "";
            if (weaponPreviewIcon) weaponPreviewIcon.enabled = false;
            if (weaponIndexText) weaponIndexText.text = "";
            return;
        }

        int total = active.Length;
        _currentPreviewWeaponIndex = Mathf.Clamp(_currentPreviewWeaponIndex, 0, total - 1);
        WeaponData current = active[_currentPreviewWeaponIndex];

        bool alreadySelected = manager.SelectedWeaponIndices.Contains(_currentPreviewWeaponIndex);
        bool slotsFull = manager.SelectedWeaponIndices.Count >= manager.MaxWeaponSlots;

        if (weaponNameText) weaponNameText.text = current?.weaponName ?? "";
        if (weaponDescText) weaponDescText.text = current?.description ?? "";
        if (weaponPreviewIcon) { weaponPreviewIcon.sprite = current?.icon; weaponPreviewIcon.enabled = current?.icon != null; }
        if (weaponIndexText) weaponIndexText.text = $"{_currentPreviewWeaponIndex + 1} / {total}";
        if (selectWeaponButton) selectWeaponButton.interactable = alreadySelected || !slotsFull;
    }

    void RefreshWeaponSlotIcons(WeaponData[] loadout)
    {
        if (weaponSlotIcons == null) return;

        for (int i = 0; i < weaponSlotIcons.Length; i++)
        {
            if (weaponSlotIcons[i] == null) continue;

            if (i < loadout.Length && loadout[i] != null)
            {
                weaponSlotIcons[i].sprite = loadout[i].icon;
                weaponSlotIcons[i].enabled = true;
            }
            else
            {
                weaponSlotIcons[i].enabled = false;
            }
        }
    }

    void ClearWeaponSlotIcons()
    {
        if (weaponSlotIcons == null) return;
        foreach (Image slot in weaponSlotIcons)
            if (slot != null) slot.enabled = false;
    }

    void RebuildAccessoryRows(CharacterData data)
    {
        if (accessoryRowParent != null)
            foreach (Transform child in accessoryRowParent)
                Destroy(child.gameObject);
        _accessoryRows.Clear();

        if (data?.accessorySlots == null || accessoryRowPrefab == null) return;

        for (int i = 0; i < data.accessorySlots.Length; i++)
        {
            var slot = data.accessorySlots[i];
            GameObject row = Instantiate(accessoryRowPrefab, accessoryRowParent);
            var rowUI = row.GetComponent<AccessoryRowUI>();

            if (rowUI == null)
            {
                Debug.LogWarning("[characterSelectUI] accessoryRowPrefab is missing accessoryRowUI component.");
                continue;
            }

            int capturedIndex = i;
            rowUI.Setup(
                slot.slotName,
                slot.options,
                0,
                () => manager.CycleAccessory(capturedIndex, -1),
                () => manager.CycleAccessory(capturedIndex, 1),
                () => manager.SetAccessorySlotFocus(capturedIndex)
            );

            _accessoryRows.Add(rowUI);
        }
    }

    void RebuildColourRows()
    {
        if (colourRowParent != null)
            foreach (Transform child in colourRowParent)
                Destroy(child.gameObject);
        _colourRows.Clear();

        if (colourRowPrefab == null || colourRowParent == null) return;

        PlayerCustomization previewCustom = FindObjectOfType<PlayerCustomization>();
        if (previewCustom == null) return;

        foreach (var (path, matCount) in previewCustom.GetRendererInfo())
        {
            for (int m = 0; m < matCount; m++)
            {
                GameObject row = Instantiate(colourRowPrefab, colourRowParent);
                ColourRowUI rowUI = row.GetComponent<ColourRowUI>();
                if (rowUI == null) continue;

                string capturedPath = path;
                int capturedMat = m;

                rowUI.Setup($"{path} [{m}]", Color.white,
                    colour => manager.SetColour(capturedPath, capturedMat, colour));

                _colourRows.Add(rowUI);
            }
        }
    }
}

public class AccessoryRowUI : MonoBehaviour
{
    public string slotName { get; private set; }

    [Header("references")]
    public TextMeshProUGUI slotLabel;
    public Image accessoryIcon;
    public TextMeshProUGUI accessoryName;
    public Button prevButton;
    public Button nextButton;

    public void Setup(string slot, AccessoryData[] options, int currentIndex,
                      System.Action onPrev, System.Action onNext, System.Action onFocus)
    {
        slotName = slot;
        if (slotLabel) slotLabel.text = slot;

        prevButton?.onClick.AddListener(() => { onFocus?.Invoke(); onPrev?.Invoke(); });
        nextButton?.onClick.AddListener(() => { onFocus?.Invoke(); onNext?.Invoke(); });

        UpdateDisplay(currentIndex < (options?.Length ?? 0) ? options[currentIndex] : null, currentIndex);
    }

    public void UpdateDisplay(AccessoryData data, int index)
    {
        if (accessoryIcon) { accessoryIcon.sprite = data?.icon; accessoryIcon.enabled = data?.icon != null; }
        if (accessoryName) accessoryName.text = data != null ? data.accessoryName : "none";
    }
}

public class ColourRowUI : MonoBehaviour
{
    [Header("references")]
    public TextMeshProUGUI label;
    public Image colourPreview;
    public Button pickButton;

    System.Action<Color> _onColourChosen;
    Color _currentColour;

    public void Setup(string displayName, Color initialColour, System.Action<Color> onColourChosen)
    {
        _onColourChosen = onColourChosen;
        _currentColour = initialColour;
        if (label) label.text = displayName;
        UpdatePreview(initialColour);
        pickButton?.onClick.AddListener(OnPickPressed);
    }

    public void ApplyColour(Color colour)
    {
        _currentColour = colour;
        UpdatePreview(colour);
        _onColourChosen?.Invoke(colour);
    }

    void OnPickPressed()
    {
        Color[] presets = { Color.white, Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta, Color.black };
        int next = (System.Array.IndexOf(presets, _currentColour) + 1) % presets.Length;
        ApplyColour(presets[next]);
    }

    void UpdatePreview(Color colour)
    {
        if (colourPreview) colourPreview.color = colour;
    }
}