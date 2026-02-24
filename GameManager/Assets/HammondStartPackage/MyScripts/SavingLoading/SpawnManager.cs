using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("character select data")]
    [Tooltip("must be the same so asset referenced by characterSelectManager")]
    public CharacterCustomizationSave customizationSave;

    [Tooltip("must match characterSelectManager.characters order")]
    public CharacterData[] characters;

    [Tooltip("must match characterSelectManager.weapons order")]
    public WeaponData[] weapons;

    [Header("fallback")]
    public GameObject fallbackPlayerPrefab;

    [Header("spawn points")]
    public Transform defaultSpawnPoint;
    public SpawnEntry[] entries;

    [Header("weapon socket")]
    [Tooltip("name of the child transform on the player prefab where weapons are parented")]
    public string weaponSocketName = "WeaponSocket";

    [Header("settings")]
    public bool disablePhysicsDuringSpawn = true;

    void Awake()
    {
        if (customizationSave != null)
            customizationSave.LoadFromSaveManager();

        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        Transform spawnPoint = ResolveSpawnPoint() ?? transform;
        GameObject prefab = ResolvePrefab();

        if (prefab == null)
        {
            Debug.LogWarning("[spawnManager] no player prefab resolved.");
            return;
        }

        GameObject player = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        ApplyCustomization(player);
        ApplyWeapons(player);

        SceneTransitionData.Clear();
    }

    GameObject ResolvePrefab()
    {
        if (customizationSave == null || characters == null || characters.Length == 0)
            return fallbackPlayerPrefab;

        int idx = Mathf.Clamp(customizationSave.selectedCharacterIndex, 0, characters.Length - 1);
        CharacterData data = characters[idx];

        if (data?.characterPrefab == null)
        {
            Debug.LogWarning($"[spawnManager] characterdata '{data?.characterName}' has no prefab assigned.");
            return fallbackPlayerPrefab;
        }

        return data.characterPrefab;
    }

    void ApplyCustomization(GameObject player)
    {
        if (customizationSave == null) return;

        int idx = Mathf.Clamp(customizationSave.selectedCharacterIndex, 0, (characters?.Length ?? 1) - 1);
        CharacterData data = characters != null && characters.Length > 0 ? characters[idx] : null;

        if (data == null || !data.useCustomization) return;

        PlayerCustomization custom = player.GetComponent<PlayerCustomization>();
        if (custom == null) return;

        foreach (var col in customizationSave.colours)
            custom.SetColour(col.rendererPath, col.materialIndex, col.colour);

        if (data.accessorySlots == null) return;

        foreach (var acc in customizationSave.accessories)
        {
            var slot = data.accessorySlots.FirstOrDefault(s => s.slotName == acc.slotName);
            if (slot == null || slot.options == null || acc.accessoryIndex >= slot.options.Length) continue;
            custom.EquipAccessory(slot.slotName, slot.options[acc.accessoryIndex]);
        }
    }

    void ApplyWeapons(GameObject player)
    {
        if (customizationSave == null)
        {
            Debug.LogWarning("[spawnManager] no customizationSave.");
            return;
        }

        int charIdx = Mathf.Clamp(customizationSave.selectedCharacterIndex, 0, (characters?.Length ?? 1) - 1);
        CharacterData charData = characters != null && characters.Length > 0 ? characters[charIdx] : null;

        WeaponData[] activeWeapons = (charData != null && charData.useCharacterWeapons && charData.characterWeapons != null)
            ? charData.characterWeapons
            : weapons;

        if (activeWeapons == null || activeWeapons.Length == 0)
        {
            Debug.LogWarning("[spawnManager] no active weapons array.");
            return;
        }

        List<int> indices = customizationSave.selectedWeaponIndices;

        Debug.Log($"[spawnManager] weapon indices count: {indices?.Count ?? -1}");

        if (indices == null || indices.Count == 0)
        {
            Debug.LogWarning("[spawnManager] no weapon indices saved — did you select weapons in character select?");
            return;
        }

        Transform socket = FindSocket(player.transform, weaponSocketName);
        if (socket == null)
        {
            Debug.LogWarning($"[spawnManager] no weapon socket named '{weaponSocketName}' found on player.");
            return;
        }

        List<GameObject> spawnedWeapons = new List<GameObject>();
        foreach (int idx in indices)
        {
            if (idx < 0 || idx >= activeWeapons.Length) continue;
            WeaponData weaponData = activeWeapons[idx];
            if (weaponData?.weaponPrefab == null) continue;

            GameObject weaponGO = Instantiate(weaponData.weaponPrefab);
            weaponGO.transform.SetParent(socket, false);
            weaponGO.transform.localPosition = Vector3.zero;
            spawnedWeapons.Add(weaponGO);
            Debug.Log($"[spawnManager] spawned weapon: {weaponData.weaponName}");
        }

        WeaponSwitch ws = player.GetComponentInChildren<WeaponSwitch>();
        if (ws == null)
        {
            Debug.LogWarning("[spawnManager] no weaponSwitch found on player.");
            return;
        }

        if (spawnedWeapons.Count > 0)
        {
            foreach (GameObject w in spawnedWeapons)
                w.SetActive(false);
            ws.EquipLoadout(spawnedWeapons.ToArray(), 0);
            Debug.Log($"[spawnManager] equipLoadout called with {spawnedWeapons.Count} weapons.");
        }
    }

    Transform ResolveSpawnPoint()
    {
        if (entries != null && entries.Length > 0)
        {
            if (!string.IsNullOrEmpty(SceneTransitionData.PortalID))
            {
                SpawnEntry match = FindEntry(e => e.matchPortalID && e.portalID == SceneTransitionData.PortalID);
                if (match != null) return PickSpawnPoint(match);
            }

            if (!string.IsNullOrEmpty(SceneTransitionData.PreviousScene))
            {
                SpawnEntry match = FindEntry(e => !e.matchPortalID && e.fromScene == SceneTransitionData.PreviousScene);
                if (match != null) return PickSpawnPoint(match);
            }
        }

        return defaultSpawnPoint;
    }

    SpawnEntry FindEntry(System.Predicate<SpawnEntry> predicate)
    {
        foreach (SpawnEntry entry in entries)
            if (predicate(entry)) return entry;
        return null;
    }

    Transform PickSpawnPoint(SpawnEntry entry)
    {
        if (entry.spawnPoints == null || entry.spawnPoints.Length == 0)
            return defaultSpawnPoint;
        if (entry.useRandomSpawn && entry.spawnPoints.Length > 1)
            return entry.spawnPoints[Random.Range(0, entry.spawnPoints.Length)];
        return entry.spawnPoints[0];
    }

    Transform FindSocket(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform found = FindSocket(child, name);
            if (found != null) return found;
        }
        return null;
    }

    void OnDrawGizmosSelected()
    {
        if (defaultSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            DrawSpawnGizmo(defaultSpawnPoint, "default");
        }

        if (entries == null) return;

        Color[] colors = { Color.cyan, Color.magenta, Color.yellow, new Color(1f, 0.5f, 0f) };
        for (int i = 0; i < entries.Length; i++)
        {
            Gizmos.color = colors[i % colors.Length];
            SpawnEntry entry = entries[i];
            if (entry.spawnPoints == null) continue;
            string label = entry.matchPortalID ? $"portal {entry.portalID}" : $"from {entry.fromScene}";
            foreach (Transform point in entry.spawnPoints)
                if (point != null) DrawSpawnGizmo(point, label);
        }
    }

    void DrawSpawnGizmo(Transform point, string label)
    {
        Gizmos.DrawWireSphere(point.position + Vector3.up * 0.3f, 0.3f);
        Gizmos.DrawWireSphere(point.position + Vector3.up * 1.5f, 0.3f);
        Gizmos.DrawLine(point.position + Vector3.up * 0.3f + Vector3.left * 0.3f, point.position + Vector3.up * 1.5f + Vector3.left * 0.3f);
        Gizmos.DrawLine(point.position + Vector3.up * 0.3f + Vector3.right * 0.3f, point.position + Vector3.up * 1.5f + Vector3.right * 0.3f);
        Gizmos.DrawRay(point.position + Vector3.up, point.forward * 0.8f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(point.position + Vector3.up * 2f, label);
#endif
    }

    [System.Serializable]
    public class SpawnEntry
    {
        public bool matchPortalID;
        public string portalID;
        public string fromScene;
        public Transform[] spawnPoints;
        public bool useRandomSpawn;
    }
}