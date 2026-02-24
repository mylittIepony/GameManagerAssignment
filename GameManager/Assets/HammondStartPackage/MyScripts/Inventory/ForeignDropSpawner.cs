using UnityEngine;
using UnityEngine.SceneManagement;

public class ForeignDropSpawner : MonoBehaviour
{
    public string resourcesFolder = "Items";

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (string id in ForeignDropRegistry.GetAll())
        {
            string prefix = $"ForeignDrop/WorldItem/{id}";
            string itemScene = SaveManager.Get($"{prefix}/Scene", "");

            if (itemScene != scene.name) continue;

            bool pickedUp = SaveManager.GetBool($"{prefix}/PickedUp", false);
            if (pickedUp) continue;

            SpawnItem(id, prefix);
        }
    }

    void SpawnItem(string id, string prefix)
    {
        string itemName = SaveManager.Get($"{prefix}/ItemName", "");
        if (string.IsNullOrEmpty(itemName)) return;

        InventoryItemData data = Resources.Load<InventoryItemData>($"{resourcesFolder}/{itemName}");
        if (data?.worldPrefab == null)
        {
            Debug.LogWarning($"[ForeignDropSpawner] can't find item '{itemName}' in Resources/{resourcesFolder}/");
            return;
        }

        Vector3 pos = SaveManager.GetVector3($"{prefix}/Position");
        Vector3 rot = SaveManager.GetVector3($"{prefix}/Rotation");
        int qty = SaveManager.GetInt($"{prefix}/Quantity", 1);

        GameObject go = Instantiate(data.worldPrefab, pos, Quaternion.Euler(rot));
        WorldItem wi = go.GetComponent<WorldItem>();
        if (wi == null)
        {
            Destroy(go);
            return;
        }

        wi.uniqueID = id;
        wi.itemData = data;
        wi.quantity = qty;

        SaveManager.Unregister(wi);
        wi.SendMessage("SetAsForeignDrop", SendMessageOptions.DontRequireReceiver);
        SaveManager.Register(wi);
    }
}