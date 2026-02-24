using System.Collections.Generic;
using UnityEngine;

public class PlayerCustomization : MonoBehaviour
{
    [System.Serializable]
    public class AccessorySocket
    {
        public string slotName = "Hat";
        public Transform socketTransform;
        [HideInInspector] public GameObject spawnedAccessory;
    }

    [Header("accessory sockets")]
    public AccessorySocket[] sockets;

    readonly Dictionary<string, GameObject> _spawnedAccessories = new Dictionary<string, GameObject>();

    Dictionary<string, Color[]> _originalColours = new Dictionary<string, Color[]>();

    void Awake()
    {
        CacheOriginalColors();
    }

    public void EquipAccessory(string slotName, AccessoryData data)
    {

        AccessorySocket socket = GetSocket(slotName);
        if (socket == null)
        {
            Debug.LogWarning($"no socket named '{slotName}' found.");
            return;
        }

        if (_spawnedAccessories.TryGetValue(slotName, out GameObject old))
        {
            if (old != null) Destroy(old);
            _spawnedAccessories.Remove(slotName);
            socket.spawnedAccessory = null;
        }

        if (data == null || data.prefab == null) return;

        GameObject spawned = Instantiate(data.prefab, socket.socketTransform.position,
                                         socket.socketTransform.rotation, socket.socketTransform);
        _spawnedAccessories[slotName] = spawned;
        socket.spawnedAccessory = spawned;
    }

    public void ClearAllAccessories()
    {
        foreach (var kvp in _spawnedAccessories)
            if (kvp.Value != null) Destroy(kvp.Value);
        _spawnedAccessories.Clear();

        foreach (AccessorySocket s in sockets)
            s.spawnedAccessory = null;
    }


    public void SetColour(string rendererPath, int materialIndex, Color color)
    {
        Transform t = transform.Find(rendererPath);
        if (t == null)
        {
            Debug.LogWarning($"renderer path not found: '{rendererPath}'");
            return;
        }

        Renderer rend = t.GetComponent<Renderer>();
        if (rend == null) return;

        Material[] mats = rend.materials;
        if (materialIndex < 0 || materialIndex >= mats.Length) return;

        mats[materialIndex].color = color;
        rend.materials = mats;
    }


    public void ResetColors()
    {
        foreach (var kvp in _originalColours)
        {
            Transform t = transform.Find(kvp.Key);
            if (t == null) continue;
            Renderer rend = t.GetComponent<Renderer>();
            if (rend == null) continue;

            Material[] mats = rend.materials;
            for (int i = 0; i < kvp.Value.Length && i < mats.Length; i++)
                mats[i].color = kvp.Value[i];
            rend.materials = mats;
        }
    }


    public void ApplyCustomization(CharacterCustomizationSave save,
                                   List<List<AccessoryData>> accessoriesPerSlot)
    {
        if (save == null) return;

        foreach (var col in save.colours)
            SetColour(col.rendererPath, col.materialIndex, col.colour);
        foreach (var acc in save.accessories)
        {
            if (accessoriesPerSlot == null) continue;

            AccessorySocket socket = GetSocket(acc.slotName);
            if (socket == null) continue;

 
        }
    }

    AccessorySocket GetSocket(string slotName)
    {
        if (sockets == null) return null;
        foreach (AccessorySocket s in sockets)
            if (s.slotName == slotName) return s;
        return null;
    }

    void CacheOriginalColors()
    {
        _originalColours.Clear();
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
        {
            string path = GetRelativePath(rend.transform);
            Color[] colors = new Color[rend.sharedMaterials.Length];
            for (int i = 0; i < rend.sharedMaterials.Length; i++)
                colors[i] = rend.sharedMaterials[i] != null ? rend.sharedMaterials[i].color : Color.white;
            _originalColours[path] = colors;
        }
    }

    string GetRelativePath(Transform t)
    {
        string path = t.name;
        Transform current = t.parent;
        while (current != null && current != transform)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }


    public List<(string path, int matCount)> GetRendererInfo()
    {
        var result = new List<(string, int)>();
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
            result.Add((GetRelativePath(rend.transform), rend.sharedMaterials.Length));
        return result;
    }
}
