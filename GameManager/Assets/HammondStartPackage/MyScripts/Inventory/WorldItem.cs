using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class WorldItem : MonoBehaviour, ISaveable
{
    [Header("item")]
    public InventoryItemData itemData;
    public int quantity = 1;

    [Header("pickup")]
    public string playerTag = "Player";
    public bool autoPickup = false;
    public bool requireInteract = true;
    public string interactActionName = "Interact";

    [Header("save")]
    public string uniqueID = "";

    bool _playerInRange;
    bool _pickedUp;
    bool _isInWorld;
    bool _isForeignDrop;

    InputAction _interactAction;
    Vector3 _originalPosition;
    Quaternion _originalRotation;
    string _homeScene;
    string _sceneName;
    string _cachedSaveID;

    [System.NonSerialized] public bool spawnedAsForeignDrop = false;

    public string SaveID
    {
        get
        {
            if (_cachedSaveID != null) return _cachedSaveID;
            _cachedSaveID = _isForeignDrop
                ? $"ForeignDrop/WorldItem/{uniqueID}"
                : $"{_homeScene}/WorldItem/{uniqueID}";
            return _cachedSaveID;
        }
    }

    void RefreshSaveID() => _cachedSaveID = null;

    public bool IsHeldByPlayer => _pickedUp;


    public void ForcePickedUp(string homeScene)
    {
        _homeScene = homeScene;
        _sceneName = homeScene;
        _pickedUp = true;
        _isInWorld = false;
        RefreshSaveID();
      
    }

 
    void Awake()
    {
        if (_pickedUp) return;

        _homeScene = gameObject.scene.name;
        _sceneName = _homeScene;
        _originalPosition = transform.position;
        _originalRotation = transform.rotation;
        _isInWorld = false;

        if (spawnedAsForeignDrop)
            _isForeignDrop = true;

        if (string.IsNullOrEmpty(uniqueID))
            GenerateUniqueID();

    }

    void OnDestroy()
    {

        UnsubscribeInteract();
    }

    void OnEnable() => SubscribeInteract();
    void OnDisable() => UnsubscribeInteract();

    void GenerateUniqueID()
    {
        string baseID = $"{gameObject.name}_{_homeScene}_{_originalPosition.x:F2}_{_originalPosition.y:F2}_{_originalPosition.z:F2}";
        string candidate = baseID;
        int suffix = 0;

        foreach (WorldItem other in Resources.FindObjectsOfTypeAll<WorldItem>())
        {
            if (other == this || other == null) continue;

            if (other.gameObject.scene.name == "DontDestroyOnLoad" && !other.IsHeldByPlayer)
                continue;

            if (other.uniqueID == candidate)
            {
                suffix++;
                candidate = $"{baseID}_{suffix}";
            }
        }

        uniqueID = candidate;
    }

    void SubscribeInteract()
    {
        if (!requireInteract || InputManager.Instance == null) return;
        _interactAction = InputManager.Instance.FindAction(interactActionName);
        if (_interactAction != null)
            _interactAction.performed += OnInteract;
    }

    void UnsubscribeInteract()
    {
        if (_interactAction != null)
        {
            _interactAction.performed -= OnInteract;
            _interactAction = null;
        }
    }

    void OnInteract(InputAction.CallbackContext ctx)
    {
        if (_playerInRange && !_pickedUp) TryPickup();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerInRange = true;
        if (autoPickup && !_pickedUp) TryPickup();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag)) _playerInRange = false;
    }


    void TryPickup()
    {
        if (InventoryManager.Instance == null || itemData == null) return;

        bool success = InventoryManager.Instance.AddItem(itemData, quantity);
        if (!success) return;

        _pickedUp = true;
        _isInWorld = false;

        if (_isForeignDrop)
            ForeignDropRegistry.Remove(uniqueID);

        InventoryManager.Instance.TrackWorldItemInSlot(itemData, uniqueID);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            player.GetComponent<InventoryCarrier>()?.RegisterPickedUpItem(itemData, this);

        DontDestroyOnLoad(gameObject);
        gameObject.SetActive(false);
        SaveManager.ForceSave();
    }



    public void DropTo(Vector3 position, Vector3 force)
    {
        string activeScene = SceneManager.GetActiveScene().name;

        InventoryManager.Instance?.UntrackWorldItemFromSlot(itemData, uniqueID);

        _pickedUp = false;
        _isInWorld = true;

        bool nowForeign = (activeScene != _homeScene);
        if (nowForeign != _isForeignDrop)
        {
            SaveManager.DeleteByPrefix(_cachedSaveID ?? "");
            _isForeignDrop = nowForeign;
            RefreshSaveID();
        }

        _sceneName = activeScene;
        transform.position = position;
        transform.rotation = Quaternion.identity;

        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (var col in GetComponentsInChildren<Collider>(true))
            col.enabled = true;

        gameObject.SetActive(true);

        if (rb != null && force != Vector3.zero)
            rb.AddForce(force, ForceMode.VelocityChange);

        SaveManager.ForceSave();
    }

    public void ResetToWorld()
    {
        if (!_pickedUp) return;

        _pickedUp = false;
        _isForeignDrop = false;
        spawnedAsForeignDrop = false;
        RefreshSaveID();
        _isInWorld = true;
        _sceneName = _homeScene;

        if (gameObject.scene.name == "DontDestroyOnLoad")
        {
            Scene home = SceneManager.GetSceneByName(_homeScene);
            if (home.IsValid() && home.isLoaded)
                SceneManager.MoveGameObjectToScene(gameObject, home);
            else
                SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
        }

        transform.position = _originalPosition;
        transform.rotation = _originalRotation;
        gameObject.SetActive(false);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = false; rb.WakeUp(); }

        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = true;
    }



    public void OnSave()
    {
        if (_pickedUp)
        {
            SaveManager.SetBool($"{SaveID}/PickedUp", true);
            SaveManager.Set($"{SaveID}/HomeScene", _homeScene);
            SaveManager.Set($"WorldItemHome/{uniqueID}", _homeScene);
            if (itemData != null)
                SaveManager.Set($"{SaveID}/ItemName", itemData.name);
            return;
        }

        SaveManager.SetBool($"{SaveID}/PickedUp", false);
        SaveManager.SetBool($"{SaveID}/IsInWorld", _isInWorld);
        SaveManager.SetBool($"{SaveID}/IsForeign", _isForeignDrop);
        SaveManager.SetVector3($"{SaveID}/Position", transform.position);
        SaveManager.SetVector3($"{SaveID}/Rotation", transform.eulerAngles);
        SaveManager.SetInt($"{SaveID}/Quantity", quantity);
        SaveManager.Set($"{SaveID}/Scene", _sceneName);

        if (_isForeignDrop && itemData != null)
        {
            SaveManager.Set($"{SaveID}/ItemName", itemData.name);
            ForeignDropRegistry.Add(uniqueID);
        }
    }

    public void OnLoad()
    {
        if (this == null || gameObject == null) return;
        if (spawnedAsForeignDrop) { spawnedAsForeignDrop = false; return; }

        if (gameObject.scene.name == "DontDestroyOnLoad")
        {
            _pickedUp = true;
            _isInWorld = false;
            gameObject.SetActive(false);
            return;
        }

        if (_pickedUp) return;

        string nativeKey = $"{_homeScene}/WorldItem/{uniqueID}";
        string foreignKey = $"ForeignDrop/WorldItem/{uniqueID}";

        bool hasForeignData = SaveManager.HasKey($"{foreignKey}/PickedUp");
        bool hasNativeData = SaveManager.HasKey($"{nativeKey}/PickedUp");

        if (hasForeignData && !_isForeignDrop)
        {
            bool pickedUp = SaveManager.GetBool($"{foreignKey}/PickedUp", false);
            if (pickedUp)
            {
                _pickedUp = true;
                DontDestroyOnLoad(gameObject);
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
            return;
        }

        string loadKey = hasForeignData ? foreignKey : nativeKey;
        if (hasForeignData) { _isForeignDrop = true; RefreshSaveID(); }
        else { _isForeignDrop = false; RefreshSaveID(); }

        if (!hasForeignData && !hasNativeData)
        {
            if (gameObject.scene.name == "DontDestroyOnLoad")
            {
                Destroy(gameObject);
                return;
            }

            string activeScene = SceneManager.GetActiveScene().name;
            if (activeScene == _homeScene)
            {
                _isInWorld = true;
                _pickedUp = false;
                transform.position = _originalPosition;
                transform.rotation = _originalRotation;
                gameObject.SetActive(true);

                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null) { rb.isKinematic = false; rb.WakeUp(); }
                foreach (var col in GetComponentsInChildren<Collider>())
                    col.enabled = true;
            }
            else
            {
                gameObject.SetActive(false);
            }
            return;
        }

        bool pickedUpVal = SaveManager.GetBool($"{loadKey}/PickedUp", false);

        if (pickedUpVal)
        {
            if (gameObject.scene.name != "DontDestroyOnLoad")
            {
                gameObject.SetActive(false);
                return;
            }

            _pickedUp = true;
            _isInWorld = false;
            return;
        }


        if (gameObject.scene.name == "DontDestroyOnLoad")
        {
        
            Destroy(gameObject);
            return;
        }

        bool isInWorld = SaveManager.GetBool($"{loadKey}/IsInWorld", true);
        bool isForeign = SaveManager.GetBool($"{loadKey}/IsForeign", false);
        string savedScene = SaveManager.Get($"{loadKey}/Scene", _homeScene);

        _pickedUp = false;
        _isInWorld = isInWorld;
        _isForeignDrop = isForeign;
        _sceneName = savedScene;

        if (!_isInWorld)
        {
            gameObject.SetActive(false);
            return;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        if (_sceneName != currentScene)
        {
            gameObject.SetActive(false);
            return;
        }

        transform.position = SaveManager.GetVector3($"{loadKey}/Position", _originalPosition);
        transform.rotation = Quaternion.Euler(SaveManager.GetVector3($"{loadKey}/Rotation", _originalRotation.eulerAngles));
        quantity = SaveManager.GetInt($"{loadKey}/Quantity", quantity);

        gameObject.SetActive(true);

        Rigidbody rb2 = GetComponent<Rigidbody>();
        if (rb2 != null) { rb2.isKinematic = false; rb2.WakeUp(); }

        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = true;
    }
}