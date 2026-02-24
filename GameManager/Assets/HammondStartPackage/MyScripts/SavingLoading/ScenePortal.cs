using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class SceneTransitionData
{
    public static string PreviousScene;
    public static string PortalID;

    public static void Clear()
    {
        PreviousScene = null;
        PortalID = null;
    }
}

[RequireComponent(typeof(Collider))]
public class ScenePortal : MonoBehaviour
{
    [Header("destination")]
    public string targetScene;
    public string portalID;

    [Header("settings")]
    public string playerTag = "Player";
    public bool requireInteract = true;
    public string interactMapName = "";
    public string interactActionName = "Interact";
    public bool showLoadingScreen;

    bool _playerInRange;
    bool _transitioning;
    InputAction _interactAction;

    void Awake() => CacheAction();

    void OnEnable()
    {
        CacheAction();
        if (requireInteract && _interactAction != null)
            _interactAction.performed += OnInteractPerformed;
    }

    void OnDisable()
    {
        if (_interactAction != null)
            _interactAction.performed -= OnInteractPerformed;
    }

    void CacheAction()
    {
        if (!requireInteract || InputManager.Instance == null) return;

        _interactAction = string.IsNullOrEmpty(interactMapName)
            ? InputManager.Instance.FindAction(interactActionName)
            : InputManager.Instance.FindAction(interactMapName, interactActionName);
    }

    void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (!_playerInRange || _transitioning) return;
        StartTransition();
    }

    void OnTriggerEnter(Collider other)
    {
        if (_transitioning || !other.CompareTag(playerTag)) return;
        _playerInRange = true;
        if (!requireInteract) StartTransition();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag)) _playerInRange = false;
    }

    void StartTransition()
    {
        if (_transitioning) return;
        _transitioning = true;

        SceneTransitionData.PreviousScene = SceneManager.GetActiveScene().name;
        SceneTransitionData.PortalID = portalID;

        try
        {
            SaveManager.SaveBeforeSceneChange();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"scene portal threw: {e}");
        }

        Time.timeScale = 1f;

        try
        {
            SceneManager.LoadScene(targetScene);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"scene portal load scene('{targetScene}') threw: {e}");
            _transitioning = false;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.3f);
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box) Gizmos.DrawCube(transform.position + box.center, box.size);
        else if (col is SphereCollider sphere) Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
    }
}