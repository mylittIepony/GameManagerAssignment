using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InventoryManager : MonoBehaviour, ISaveable
{
    public static InventoryManager Instance { get; private set; }
    public static bool IsHoldingInventoryItem { get; private set; }

    [Header("settings")]
    public int maxSlots = 8;
    public bool showEmptySlots = true;
    public bool alwaysShowInventory = false;
    public bool pauseTimeWhenOpen = false;
    public bool allowScrolling = true;

    [Header("ui")]
    public RectTransform slotContainer;
    public GameObject slotPrefab;
    public GameObject inventoryCanvas;

    [Header("input")]
    public string toggleActionName = "ToggleInventory";

    List<InventorySlot> _slots = new List<InventorySlot>();
    List<InventorySlotUI> _slotUIElements = new List<InventorySlotUI>();

    int _activeSlotIndex = -1;
    bool _inventoryOpen;
    float _previousTimeScale = 1f;

    InputAction _toggleInventoryAction;

    InventoryCarrier _carrier;

    public string SaveID => "Inventory/Manager";


    void Awake()
    {

        SceneManager.sceneLoaded += OnSceneLoaded;
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSlots();
        SetupInput();
        SaveManager.Register(this);
    }

    void Start()
    {
        PopulateSlotUI();
        UpdateSlotVisuals();

        if (inventoryCanvas != null)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            bool isMenuScene = sceneName == "Title" || sceneName == "CharacterSelect";

            if (isMenuScene)
                inventoryCanvas.SetActive(false);
            else
            {
                _inventoryOpen = alwaysShowInventory;
                inventoryCanvas.SetActive(_inventoryOpen);
            }
        }

        FindCarrier();
    }

    void OnDestroy()
    {
        SaveManager.Unregister(this);
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (inventoryCanvas == null) return;

        bool isMenuScene = scene.name == "Title" || scene.name == "CharacterSelect";
        if (isMenuScene) { inventoryCanvas.SetActive(false); return; }

        if (alwaysShowInventory)
            inventoryCanvas.SetActive(true);
        else
            inventoryCanvas.SetActive(_inventoryOpen);
    }



    void FindCarrier()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        InventoryCarrier newCarrier = player.GetComponent<InventoryCarrier>();
        if (newCarrier == null || newCarrier == _carrier) return;

        _carrier = newCarrier;

        foreach (WorldItem wi in Resources.FindObjectsOfTypeAll<WorldItem>())
        {
            if (wi.IsHeldByPlayer)
                _carrier.RegisterPickedUpItem(wi.itemData, wi);
        }
    }

    void InitializeSlots()
    {
        _slots.Clear();
        for (int i = 0; i < maxSlots; i++)
            _slots.Add(new InventorySlot());
    }

    void SetupInput()
    {
        if (InputManager.Instance == null) return;

        _toggleInventoryAction = InputManager.Instance.FindAction(toggleActionName);

        if (_toggleInventoryAction == null)
        {
            var map = InputManager.Instance.inputActions?.FindActionMap("Gameplay");
            if (map != null)
            {
                _toggleInventoryAction = map.FindAction(toggleActionName)
                    ?? map.AddAction(toggleActionName, binding: "<Keyboard>/tab");
                map.Enable();
            }
        }
    }

    void Update()
    {
        if (_carrier == null) FindCarrier();
        if (PauseManager.IsPaused) return;
        HandleInput();
    }

    void HandleInput()
    {
        if (_toggleInventoryAction != null && _toggleInventoryAction.WasPressedThisFrame())
            ToggleInventory();

        if (allowScrolling)
        {
            float scroll = Mouse.current?.scroll.ReadValue().y ?? 0f;
            if (scroll > 0.1f) CycleSlot(-1);
            else if (scroll < -0.1f) CycleSlot(1);
        }

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            for (int i = 0; i < Mathf.Min(10, maxSlots); i++)
            {
                int keyIndex = (i + 1) % 10;
                if (keyboard[GetNumberKey(keyIndex)].wasPressedThisFrame)
                {
                    SelectSlot(i);
                    break;
                }
            }
        }
    }

    Key GetNumberKey(int n) => n switch
    {
        0 => Key.Digit0,
        1 => Key.Digit1,
        2 => Key.Digit2,
        3 => Key.Digit3,
        4 => Key.Digit4,
        5 => Key.Digit5,
        6 => Key.Digit6,
        7 => Key.Digit7,
        8 => Key.Digit8,
        9 => Key.Digit9,
        _ => Key.Digit1
    };

    void ToggleInventory()
    {
        if (alwaysShowInventory) return;

        _inventoryOpen = !_inventoryOpen;
        if (inventoryCanvas != null) inventoryCanvas.SetActive(_inventoryOpen);

        if (pauseTimeWhenOpen)
        {
            if (_inventoryOpen) { _previousTimeScale = Time.timeScale; Time.timeScale = 0f; }
            else Time.timeScale = _previousTimeScale;
        }
    }

    void CycleSlot(int direction)
    {
        if (GetFilledSlotCount() == 0) return;

        int attempts = 0;
        do
        {
            _activeSlotIndex = (_activeSlotIndex + direction + maxSlots) % maxSlots;
            attempts++;
            if (!showEmptySlots && !_slots[_activeSlotIndex].IsEmpty) break;
            if (showEmptySlots) break;
        }
        while (attempts < maxSlots);

        UpdateActiveSlot();
    }

    void SelectSlot(int index)
    {
        if (!showEmptySlots)
        {
            var filledIndices = new List<int>();
            for (int i = 0; i < _slots.Count; i++)
                if (!_slots[i].IsEmpty) filledIndices.Add(i);

            if (index >= filledIndices.Count) return;
            int target = filledIndices[index];
            _activeSlotIndex = (_activeSlotIndex == target) ? -1 : target;
        }
        else
        {
            if (index >= maxSlots) return;
            _activeSlotIndex = (_activeSlotIndex == index) ? -1 : index;
        }

        UpdateActiveSlot();
    }

    void UpdateActiveSlot()
    {
        UpdateSlotVisuals();
        UpdateHeldItem();
    }

    void UpdateHeldItem()
    {
        if (_carrier == null) { FindCarrier(); if (_carrier == null) { IsHoldingInventoryItem = false; return; } }

        if (_activeSlotIndex < 0 || _activeSlotIndex >= _slots.Count)
        {
            _carrier.ClearHeldItem();
            IsHoldingInventoryItem = false;
            return;
        }

        InventorySlot slot = _slots[_activeSlotIndex];
        if (slot.IsEmpty || slot.itemData.worldPrefab == null)
        {
            _carrier.ClearHeldItem();
            IsHoldingInventoryItem = false;
            return;
        }

        _carrier.ShowItem(slot.itemData.worldPrefab);
        IsHoldingInventoryItem = true;
    }

    public bool AddItem(InventoryItemData itemData, int quantity = 1)
    {
        if (itemData == null) return false;

        if (itemData.isStackable)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (!_slots[i].IsEmpty && _slots[i].itemData == itemData)
                {
                    int remainder = _slots[i].AddStack(quantity);
                    if (remainder == 0) { UpdateSlotVisuals(); return true; }
                    quantity = remainder;
                }
            }
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].IsEmpty)
            {
                _slots[i].itemData = itemData;
                _slots[i].quantity = Mathf.Min(quantity, itemData.isStackable ? itemData.maxStackSize : 1);
                UpdateSlotVisuals();
                if (i == _activeSlotIndex) UpdateHeldItem();
                return true;
            }
        }

        return false;
    }

    public void RemoveItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Count) return;
        _slots[slotIndex].Clear();
        UpdateSlotVisuals();
        if (_activeSlotIndex == slotIndex) UpdateHeldItem();
    }

    public void DropItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Count) return;

        InventorySlot slot = _slots[slotIndex];
        if (slot.IsEmpty) return;

        bool isLast = slot.quantity <= 1;
        _carrier?.DropHeldItem(slot.itemData, isLast);

        slot.quantity--;
        if (slot.quantity <= 0) slot.Clear();

        UpdateSlotVisuals();
        if (_activeSlotIndex == slotIndex) UpdateHeldItem();
    }

    public void DropActiveItem() => DropItem(_activeSlotIndex);

    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= _slots.Count) return null;
        return _slots[index];
    }

    public int GetActiveSlotIndex() => _activeSlotIndex;

    void PopulateSlotUI()
    {
        if (slotContainer == null || slotPrefab == null) return;

        foreach (var ui in _slotUIElements)
            if (ui != null) Destroy(ui.gameObject);
        _slotUIElements.Clear();

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                slotUI.Initialize(i, this);
                _slotUIElements.Add(slotUI);
            }
        }
    }

    void UpdateSlotVisuals()
    {
        if (showEmptySlots)
        {
            for (int i = 0; i < _slotUIElements.Count && i < _slots.Count; i++)
            {
                _slotUIElements[i].gameObject.SetActive(true);
                _slotUIElements[i].UpdateSlot(_slots[i], i == _activeSlotIndex, i);
            }
        }
        else
        {
            int display = 0;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (!_slots[i].IsEmpty && display < _slotUIElements.Count)
                {
                    _slotUIElements[display].gameObject.SetActive(true);
                    _slotUIElements[display].UpdateSlot(_slots[i], i == _activeSlotIndex, display);
                    display++;
                }
            }
            for (int i = display; i < _slotUIElements.Count; i++)
                _slotUIElements[i].gameObject.SetActive(false);
        }
    }

    int GetFilledSlotCount()
    {
        int count = 0;
        foreach (var slot in _slots) if (!slot.IsEmpty) count++;
        return count;
    }

    public void OnSave()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            InventorySlot slot = _slots[i];
            if (!slot.IsEmpty && slot.itemData != null)
            {
                SaveManager.Set($"{SaveID}/Slot{i}/ItemName", slot.itemData.name);
                SaveManager.SetInt($"{SaveID}/Slot{i}/Quantity", slot.quantity);
            }
            else
            {
                SaveManager.DeleteKey($"{SaveID}/Slot{i}/ItemName");
                SaveManager.DeleteKey($"{SaveID}/Slot{i}/Quantity");
            }
        }

        SaveManager.SetInt($"{SaveID}/ActiveSlot", _activeSlotIndex);
    }

    public void OnLoad()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            string itemName = SaveManager.Get($"{SaveID}/Slot{i}/ItemName", "");

            if (!string.IsNullOrEmpty(itemName))
            {
                InventoryItemData data = Resources.Load<InventoryItemData>($"Items/{itemName}");
                if (data != null)
                {
                    _slots[i].itemData = data;
                    _slots[i].quantity = SaveManager.GetInt($"{SaveID}/Slot{i}/Quantity", 1);
                }
                else
                {
                    _slots[i].Clear();
                }
            }
            else
            {
                _slots[i].Clear();
            }
        }

        _activeSlotIndex = SaveManager.HasKey($"{SaveID}/ActiveSlot") ? SaveManager.GetInt($"{SaveID}/ActiveSlot", -1) : -1;
        UpdateSlotVisuals();
        StartCoroutine(UpdateHeldItemDeferred());
    }

    IEnumerator UpdateHeldItemDeferred()
    {

        yield return null;
        FindCarrier();
        UpdateHeldItem();
    }

public void ResetInventory()
{
    _carrier?.ClearHeldItem();
    _activeSlotIndex = -1;
    IsHoldingInventoryItem = false;
    InitializeSlots();
    UpdateSlotVisuals();

    foreach (WorldItem wi in Resources.FindObjectsOfTypeAll<WorldItem>())
    {
        if (wi.gameObject.scene.name == "DontDestroyOnLoad")
            Destroy(wi.gameObject);
    }
}
}