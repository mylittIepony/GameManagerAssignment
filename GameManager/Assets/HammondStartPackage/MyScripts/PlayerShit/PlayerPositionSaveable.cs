using UnityEngine;
using System.Collections;

public class PlayerPositionSaveable : MonoBehaviour, ISaveable
{
    public string SaveID => "Player/Position";

    void Awake() => SaveManager.Register(this);
    void OnDestroy() => SaveManager.Unregister(this);

    public void OnSave()
    {
        SaveManager.SetVector3($"{SaveID}/Pos", transform.position);
        SaveManager.SetVector3($"{SaveID}/Rot", transform.eulerAngles);
        SaveManager.Set($"{SaveID}/Scene", gameObject.scene.name);
    }

    public void OnLoad()
    {
        if (!SaveManager.HasKey($"{SaveID}/Pos")) return;
        string savedScene = SaveManager.Get($"{SaveID}/Scene", "");
        if (savedScene != gameObject.scene.name) return;
        StartCoroutine(ApplyNextFrame());
    }

    IEnumerator ApplyNextFrame()
    {
        yield return null;
        if (!SaveManager.HasKey($"{SaveID}/Pos")) yield break;
        string savedScene = SaveManager.Get($"{SaveID}/Scene", "");
        if (savedScene != gameObject.scene.name) yield break;
        transform.position = SaveManager.GetVector3($"{SaveID}/Pos", transform.position);
        transform.eulerAngles = SaveManager.GetVector3($"{SaveID}/Rot", transform.eulerAngles);
    }
}


public class PlayerRespawnPoint : MonoBehaviour, ISaveable
{
    public string SaveID => "Player/RespawnPoint";

    void Awake() => SaveManager.Register(this);
    void OnDestroy() => SaveManager.Unregister(this);

    public void SnapshotRestPoint()
    {
        SaveManager.SetVector3($"{SaveID}/Pos", transform.position);
        SaveManager.SetVector3($"{SaveID}/Rot", transform.eulerAngles);
        SaveManager.Set($"{SaveID}/Scene", gameObject.scene.name);
    }

    public void OnSave() { }

    public void OnLoad()
    {
        if (!SaveManager.HasKey($"{SaveID}/Pos")) return;

        string savedScene = SaveManager.Get($"{SaveID}/Scene", "");
        if (savedScene != gameObject.scene.name) return;



        StartCoroutine(ApplyNextFrame());
    }

    IEnumerator ApplyNextFrame()
    {
        yield return null;

        if (!SaveManager.HasKey($"{SaveID}/Pos")) yield break;
        string savedScene = SaveManager.Get($"{SaveID}/Scene", "");
        if (savedScene != gameObject.scene.name) yield break;

        transform.position = SaveManager.GetVector3($"{SaveID}/Pos", transform.position);
        transform.eulerAngles = SaveManager.GetVector3($"{SaveID}/Rot", transform.eulerAngles);
    }
}