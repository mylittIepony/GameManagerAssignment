using UnityEngine;
using Unity.Cinemachine;


public class CameraRegistrar : MonoBehaviour
{
    public string cameraID;
    public int basePriority;
    public CinemachineCamera _vcam;


    void Awake()
    {
        _vcam = GetComponent<CinemachineCamera>();


        if (string.IsNullOrEmpty(cameraID))
            cameraID = gameObject.name;

    }

    void Start()
    {
        if (CameraManager.Instance != null && _vcam != null)
            CameraManager.Instance.RegisterCamera(_vcam, cameraID, basePriority);
    }

    void OnDestroy()
    {
        if (CameraManager.Instance != null && _vcam != null)
            CameraManager.Instance.UnregisterCamera(_vcam);
    }
}