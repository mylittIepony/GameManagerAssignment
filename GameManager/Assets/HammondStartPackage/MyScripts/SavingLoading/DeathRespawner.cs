using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(HealthSystem))]
public class DeathRespawner : MonoBehaviour
{
    HealthSystem _health;

    void Awake() => _health = GetComponent<HealthSystem>();

    void OnEnable() => _health.OnDeath += OnDeath;
    void OnDisable() => _health.OnDeath -= OnDeath;

    void OnDeath(GameObject killer)
    {

        _health.FullHeal();

        string targetScene = SaveManager.Get("Player/RespawnPoint/Scene", "");
        if (string.IsNullOrEmpty(targetScene))
            targetScene = "Place1";

        SaveManager.ForceSave();

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(targetScene, withLoadingScreen: true);
        else
            SceneManager.LoadScene(targetScene);
    }
}