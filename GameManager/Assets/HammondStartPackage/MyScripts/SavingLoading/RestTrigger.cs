using UnityEngine;
using System.Collections;

public class RestTrigger : MonoBehaviour
{
    [Header("settings")]
    public string playerTag = "Player";
    public bool requireInteract = true;
    public string interactMapName = "";
    public string interactActionName = "Interact";
    public string restSceneName = ""; 

    bool _playerInRange;
    bool _resting;
    GameObject _playerInTrigger;
    UnityEngine.InputSystem.InputAction _interactAction;
    HealthSystem healthSystem;

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

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerInRange = true;
        _playerInTrigger = other.gameObject;
        if (!requireInteract) TriggerRest(_playerInTrigger);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            _playerInRange = false;
            _playerInTrigger = null;
        }
    }

    void OnInteractPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (!_playerInRange || _resting || _playerInTrigger == null) return;
        TriggerRest(_playerInTrigger);
    }

    public void TriggerRest(GameObject player)
    {
        if (_resting) return;
        StartCoroutine(DoRest(player));
    }

    IEnumerator DoRest(GameObject player)
    {
        _resting = true;


        foreach (EnemyHealthBar bar in FindObjectsByType<EnemyHealthBar>(FindObjectsSortMode.None))
        {
            SaveManager.DeleteKey($"{bar.SaveID}/Dead");
            if (bar.healthSystem != null)
            {
                bar.healthSystem.gameObject.SetActive(true);
                bar.healthSystem.GetComponent<EnemyAITemp>()?.Revive();
                healthSystem.Revive();
            }
        }

        SaveManager.DeleteKeysEndingWith("/Dead");
        SaveManager.ForceSave();

        string scene = string.IsNullOrEmpty(restSceneName)
            ? UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            : restSceneName;


        HealthSystem playerHealth = player.GetComponentInChildren<HealthSystem>();
        if (playerHealth != null)
            playerHealth.FullHeal();

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadSceneWithSave(scene, true);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene);

        yield return null;
    }
}