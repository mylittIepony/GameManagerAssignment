using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CharacterSelectManager : MonoBehaviour
{
    [Header("characters")]
    public CharacterData[] characters;

    [Header("weapons (free choice)")]
    public WeaponData[] weapons;
    [Tooltip("max weapons the player can equip at once")]
    public int maxWeaponSlots = 2;

    [Header("customization save")]
    public CharacterCustomizationSave customizationSave;

    [Header("scene")]
    public string gameScene = "GameScene";

    [Header("preview")]
    public Transform previewSocket;
    public int previewLayer = 0;

    [Header("switch vfx")]
    public GameObject[] characterSwitchVFX;
    public Transform characterSwitchVFXPoint;
    public float characterSwitchVFXDestroyTime = 2f;

    [Header("input — action names")]
    public string navigateActionName = "Navigate";

    int _charIndex;
    List<int> _selectedWeaponIndices = new List<int>();
    Dictionary<string, int> _accessorySelections = new Dictionary<string, int>();

    GameObject _previewInstance;
    PlayerCustomization _previewCustomization;

    InputAction _navigateAction;

    public event System.Action<CharacterData, int, int> OnCharacterChanged;
    public event System.Action<WeaponData[], int> OnWeaponLoadoutChanged;
    public event System.Action<string, AccessoryData, int> OnAccessoryChanged;
    public event System.Action<CharacterCustomizationSave> OnCustomizationLoaded;

    public CharacterData SelectedCharacter => characters != null && characters.Length > 0 ? characters[_charIndex] : null;
    public List<int> SelectedWeaponIndices => _selectedWeaponIndices;
    public int CharIndex => _charIndex;
    public int MaxWeaponSlots => GetMaxWeaponSlots();

    enum NavAxis { Character, Accessory }
    NavAxis _currentAxis = NavAxis.Character;
    int _currentAccessorySlotIndex = 0;

    void Awake()
    {
        if (customizationSave == null)
        {
            Debug.LogError("[characterSelectManager] no customizationSave assigned.");
            return;
        }

        customizationSave.LoadFromSaveManager();

        _charIndex = Mathf.Clamp(customizationSave.selectedCharacterIndex, 0, Mathf.Max(0, (characters?.Length ?? 1) - 1));

        _selectedWeaponIndices.Clear();
        foreach (int idx in customizationSave.selectedWeaponIndices)
            _selectedWeaponIndices.Add(idx);

        RebuildAccessorySelections();

        foreach (var acc in customizationSave.accessories)
            if (_accessorySelections.ContainsKey(acc.slotName))
                _accessorySelections[acc.slotName] = acc.accessoryIndex;
    }

    void Start()
    {
        ResolveActions();
        BindActions(true);
        SpawnPreview();
        ApplyAllToPreview();
        BroadcastCurrentState();
        OnCustomizationLoaded?.Invoke(customizationSave);
    }

    void OnDestroy()
    {
        BindActions(false);
    }

    void Update()
    {
        if (PauseManager.IsPaused) return;
        float scroll = Mouse.current?.scroll.ReadValue().y ?? 0f;
        if (scroll > 0f) DispatchDirection(1);
        else if (scroll < 0f) DispatchDirection(-1);
    }

    void RebuildAccessorySelections()
    {
        _accessorySelections.Clear();
        CharacterData data = SelectedCharacter;
        if (data?.accessorySlots == null) return;
        foreach (var slot in data.accessorySlots)
            _accessorySelections[slot.slotName] = 0;
    }

    void ResolveActions()
    {
        if (InputManager.Instance == null)
        {
            StartCoroutine(RetryResolveActions());
            return;
        }

        _navigateAction = InputManager.Instance.FindAction(navigateActionName);

        if (_navigateAction == null)
            Debug.LogWarning($"[characterSelectManager] action '{navigateActionName}' not found.");
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
        if (_navigateAction != null)
        {
            if (subscribe) _navigateAction.performed += OnNavigate;
            else _navigateAction.performed -= OnNavigate;
        }
    }

    void OnNavigate(InputAction.CallbackContext ctx)
    {
        Vector2 input = ctx.ReadValue<Vector2>();
        int dir = input.x > 0.5f ? 1 : (input.x < -0.5f ? -1 : 0);
        if (dir == 0) return;
        DispatchDirection(dir);
    }

    void DispatchDirection(int dir)
    {
        switch (_currentAxis)
        {
            case NavAxis.Character: CycleCharacter(dir); break;
            case NavAxis.Accessory: CycleAccessory(_currentAccessorySlotIndex, dir); break;
        }
    }

    public void SetNavAxis(int axis)
    {
        _currentAxis = (NavAxis)Mathf.Clamp(axis, 0, 1);
    }

    public void SetAccessorySlotFocus(int slotIndex)
    {
        _currentAccessorySlotIndex = slotIndex;
        _currentAxis = NavAxis.Accessory;
    }

    public void CycleCharacter(int dir)
    {
        if (characters == null || characters.Length == 0) return;
        _charIndex = (_charIndex + dir + characters.Length) % characters.Length;
        _selectedWeaponIndices.Clear();

        SpawnSwitchVFX();
        RebuildAccessorySelections();
        SpawnPreview();
        ApplyAllToPreview();
        BroadcastCurrentState();
        PersistToSave();
    }

    public void ToggleWeapon(int weaponIndex)
    {
        WeaponData[] active = GetActiveWeapons();
        if (active == null || weaponIndex < 0 || weaponIndex >= active.Length) return;

        if (_selectedWeaponIndices.Contains(weaponIndex))
        {
            _selectedWeaponIndices.Remove(weaponIndex);
        }
        else
        {
            if (_selectedWeaponIndices.Count >= GetMaxWeaponSlots()) return;
            _selectedWeaponIndices.Add(weaponIndex);
        }

        OnWeaponLoadoutChanged?.Invoke(GetSelectedWeaponData(), _selectedWeaponIndices.Count);
        PersistToSave();
    }

    public WeaponData[] GetSelectedWeaponData()
    {
        WeaponData[] active = GetActiveWeapons();
        if (active == null) return new WeaponData[0];

        var result = new List<WeaponData>();
        foreach (int idx in _selectedWeaponIndices)
            if (idx >= 0 && idx < active.Length && active[idx] != null)
                result.Add(active[idx]);
        return result.ToArray();
    }

    public void CycleAccessory(int slotIndex, int dir)
    {
        CharacterData data = SelectedCharacter;
        if (data?.accessorySlots == null || slotIndex >= data.accessorySlots.Length) return;

        var slot = data.accessorySlots[slotIndex];
        if (slot.options == null || slot.options.Length == 0) return;

        if (!_accessorySelections.ContainsKey(slot.slotName))
            _accessorySelections[slot.slotName] = 0;

        int next = (_accessorySelections[slot.slotName] + dir + slot.options.Length) % slot.options.Length;
        _accessorySelections[slot.slotName] = next;

        AccessoryData chosen = slot.options[next];
        _previewCustomization?.EquipAccessory(slot.slotName, chosen);
        OnAccessoryChanged?.Invoke(slot.slotName, chosen, next);
        PersistToSave();
    }

    public void SetColour(string rendererPath, int materialIndex, Color colour)
    {
        _previewCustomization?.SetColour(rendererPath, materialIndex, colour);

        CharacterCustomizationSave.ColourChoice existing = customizationSave.colours.Find(
            c => c.rendererPath == rendererPath && c.materialIndex == materialIndex);

        if (existing != null)
            existing.colour = colour;
        else
            customizationSave.colours.Add(new CharacterCustomizationSave.ColourChoice
            {
                rendererPath = rendererPath,
                materialIndex = materialIndex,
                colour = colour
            });

        PersistToSave();
    }

    [Header("play requirements")]
    [Tooltip("set to 0 to disable weapon requirement")]
    public int minWeaponsRequired = 1;

    public bool CanStartGame()
    {
        WeaponData[] active = GetActiveWeapons();
        bool weaponsRequired = active != null && active.Length > 0 && minWeaponsRequired > 0;
        if (weaponsRequired && _selectedWeaponIndices.Count < minWeaponsRequired) return false;
        return true;
    }

    public void StartGame()
    {
        if (!CanStartGame()) return;
        PersistToSave();
        SaveManager.SaveBeforeSceneChange();
        SceneManager.LoadScene(gameScene);
    }

    void SpawnPreview()
    {
        if (_previewInstance != null) Destroy(_previewInstance);

        CharacterData data = SelectedCharacter;
        if (data?.characterPrefab == null) return;
        if (previewSocket == null) { Debug.LogWarning("[characterSelectManager] no previewSocket assigned."); return; }

        _previewInstance = Instantiate(data.characterPrefab, previewSocket.position, previewSocket.rotation, previewSocket);
        DisableGameplayComponents(_previewInstance);
        SetLayerRecursive(_previewInstance, previewLayer);
        _previewCustomization = _previewInstance.GetComponent<PlayerCustomization>();
    }

    void ApplyAllToPreview()
    {
        if (_previewCustomization == null) return;
        CharacterData data = SelectedCharacter;
        if (data == null || !data.useCustomization) return;

        foreach (var col in customizationSave.colours)
            _previewCustomization.SetColour(col.rendererPath, col.materialIndex, col.colour);

        if (data.accessorySlots == null) return;
        foreach (var slot in data.accessorySlots)
        {
            if (!_accessorySelections.TryGetValue(slot.slotName, out int idx)) continue;
            if (slot.options == null || idx >= slot.options.Length) continue;
            _previewCustomization.EquipAccessory(slot.slotName, slot.options[idx]);
        }
    }

    void BroadcastCurrentState()
    {
        OnCharacterChanged?.Invoke(SelectedCharacter, _charIndex, characters?.Length ?? 0);
        OnWeaponLoadoutChanged?.Invoke(GetSelectedWeaponData(), _selectedWeaponIndices.Count);
    }

    void PersistToSave()
    {
        customizationSave.selectedCharacterIndex = _charIndex;
        customizationSave.selectedWeaponIndices = new List<int>(_selectedWeaponIndices);

        customizationSave.accessories.Clear();
        foreach (var kvp in _accessorySelections)
            customizationSave.accessories.Add(new CharacterCustomizationSave.AccessoryChoice
            {
                slotName = kvp.Key,
                accessoryIndex = kvp.Value
            });

        customizationSave.SaveToSaveManager();
    }

    void SpawnSwitchVFX()
    {
        if (characterSwitchVFX == null) return;
        Transform point = characterSwitchVFXPoint != null ? characterSwitchVFXPoint : transform;
        foreach (GameObject vfx in characterSwitchVFX)
        {
            if (vfx == null) continue;
            GameObject spawned = Instantiate(vfx, point.position, point.rotation);
            Destroy(spawned, characterSwitchVFXDestroyTime);
        }
    }

    int GetMaxWeaponSlots()
    {
        CharacterData data = SelectedCharacter;
        if (data != null && data.useCharacterWeapons && data.characterWeapons != null)
            return data.characterWeapons.Length;
        return maxWeaponSlots;
    }

    public WeaponData[] GetActiveWeaponsPublic() => GetActiveWeapons();

    WeaponData[] GetActiveWeapons()
    {
        CharacterData data = SelectedCharacter;
        return (data != null && data.useCharacterWeapons && data.characterWeapons != null)
            ? data.characterWeapons
            : weapons;
    }

    void DisableGameplayComponents(GameObject go)
    {
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
        foreach (var col in go.GetComponentsInChildren<Collider>()) col.enabled = false;
        foreach (var mono in go.GetComponentsInChildren<MonoBehaviour>())
        {
            if (mono is Animator || mono is PlayerCustomization) continue;
            mono.enabled = false;
        }
    }

    void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}