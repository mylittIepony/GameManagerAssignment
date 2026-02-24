using System.Collections;
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


    public string SaveID => _isForeignDrop
        ? $"ForeignDrop/WorldItem/{uniqueID}"
        : $"{_homeScene}/WorldItem/{uniqueID}";

    public bool IsHeldByPlayer => _pickedUp;

    void Awake()
    {
        _homeScene = gameObject.scene.name;
        _sceneName = _homeScene;
        _originalPosition = transform.position;
        _originalRotation = transform.rotation;
        _isInWorld = false; 

        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = gameObject.name;

        SaveManager.Register(this);
    }

    void OnDestroy()
    {
        SaveManager.Unregister(this);
        UnsubscribeInteract();
    }

    void OnEnable() => SubscribeInteract();
    void OnDisable() => UnsubscribeInteract();

    public void DropTo(Vector3 position, Vector3 force)
    {
        string activeScene = SceneManager.GetActiveScene().name;

        _sceneName = activeScene;
        _isForeignDrop = (activeScene != _homeScene);

        transform.position = position;
        transform.rotation = Quaternion.identity;
        _pickedUp = false;
        _isInWorld = true;

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
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
      //  Debug.Log($"worlditem{} dropto() complete. scene={gameObject.scene.name} active={gameObject.activeSelf} isforeign={_isForeignDrop}");

        if (rb != null && force != Vector3.zero)
            rb.AddForce(force, ForceMode.VelocityChange);

        SaveManager.ForceSave();
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

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            player.GetComponent<InventoryCarrier>()?.RegisterPickedUpItem(itemData, this);

        DontDestroyOnLoad(gameObject);
        gameObject.SetActive(false);
        SaveManager.ForceSave();
    }


    void SetAsForeignDrop()
    {
        _isForeignDrop = true;
        _sceneName = gameObject.scene.name;
    }

    IEnumerator RegisterWithCarrierNextFrame()
    {
        yield return null;
        if (this == null || gameObject == null) yield break;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            player.GetComponent<InventoryCarrier>()?.RegisterPickedUpItem(itemData, this);
    }

    public void OnSave()
    {

        if (_pickedUp)
        {
            SaveManager.SetBool($"{SaveID}/PickedUp", true);
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

        

        if (_pickedUp) return; 

        string activeScene = SceneManager.GetActiveScene().name;
        if (_sceneName != activeScene)  return; 

        if (!SaveManager.HasKey($"{SaveID}/PickedUp"))
        {
            _isInWorld = true;
            gameObject.SetActive(true);
            return;
        }

        _pickedUp = SaveManager.GetBool($"{SaveID}/PickedUp", false);
        _isInWorld = SaveManager.GetBool($"{SaveID}/IsInWorld", true);
        _isForeignDrop = SaveManager.GetBool($"{SaveID}/IsForeign", false);

        if (_pickedUp)
        {
            DontDestroyOnLoad(gameObject);
            gameObject.SetActive(false);
            SaveManager.Instance?.StartCoroutine(RegisterWithCarrierNextFrame());
            return;
        }

        if (!_isInWorld)
        {
            gameObject.SetActive(false);
            return;
        }

        transform.position = SaveManager.GetVector3($"{SaveID}/Position", _originalPosition);
        transform.rotation = Quaternion.Euler(SaveManager.GetVector3($"{SaveID}/Rotation", _originalRotation.eulerAngles));
        quantity = SaveManager.GetInt($"{SaveID}/Quantity", quantity);

        gameObject.SetActive(true);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.WakeUp();
        }

        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = true;
    }
}