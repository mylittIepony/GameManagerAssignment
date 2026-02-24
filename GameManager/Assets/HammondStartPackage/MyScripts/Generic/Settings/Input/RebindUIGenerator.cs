using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class RebindUIGenerator : MonoBehaviour
{
    [Header("prefab")]
    public GameObject rowPrefab;

    [Header("vertical layout group")]
    public Transform container;
    public Button resetAllButton;

    [Header("map names")]
    public string[] actionMapNames;

    [Header("filter")]
    public bool expandComposites = true;
    public DeviceFilter showDevices = DeviceFilter.KeyboardMouse;
    public string[] excludeActions;

    [Header("ignore maps")]
    public string[] ignoreActionMaps;



    public enum DeviceFilter
    {
        All,
        KeyboardMouse,
        Gamepad
    }




    List<RebindUI> _generatedRows = new List<RebindUI>();
    bool _initialized;

    void OnEnable()
    {
        if (!_initialized)
            TryInitialize();

        if (!_initialized)
            StartCoroutine(WaitForInputManager());
    }

    IEnumerator WaitForInputManager()
    {
        float timeout = 2f;
        float elapsed = 0f;

        while (InputManager.Instance == null && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!_initialized)
            TryInitialize();

        if (!_initialized)
            Debug.LogError("failed");
    }

    void TryInitialize()
    {

        if (_initialized)
        {
            return;
        }

        if (InputManager.Instance == null)
        {
            return;
        }

        if (rowPrefab == null)
        {
            return;
        }

        if (container == null)
        {
            return;
        }

        GenerateRows();

        if (resetAllButton != null)
            resetAllButton.onClick.AddListener(OnResetAll);

        _initialized = true;
    }

    void GenerateRows()
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);
        _generatedRows.Clear();

        HashSet<string> excluded = new HashSet<string>();
        if (excludeActions != null)
        {
            foreach (string name in excludeActions)
                excluded.Add(name);
        }

        List<InputAction> actions;



        if (actionMapNames != null && actionMapNames.Length > 0)
        {
            actions = new List<InputAction>();
            foreach (string mapName in actionMapNames)
            {
                var mapActions = InputManager.Instance.GetActionsInMap(mapName);
                if (mapActions.Count > 0)
                    actions.AddRange(mapActions);
            }
        }
        else
        {
            actions = InputManager.Instance.GetAllActions();
        }

        if (actions.Count == 0)
        {
            return;
        }

        int rowCount = 0;

        foreach (InputAction action in actions)
        {

            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];

                if (binding.isComposite)
                {
                    continue;
                }

                if (showDevices != DeviceFilter.All && !MatchesDeviceFilter(binding))
                {
                    continue;
                }

                if (binding.isPartOfComposite && !expandComposites)
                {
                    continue;
                }

                CreateRow(action, i, action.name);
                rowCount++;
            }
        }

    }

    void CreateRow(InputAction action, int bindingIndex, string displayName)
    {
        GameObject row = Instantiate(rowPrefab, container);

        if (row == null)
            return;

        var rebindUI = row.GetComponent<RebindUI>();
        if (rebindUI == null)
        {
            Destroy(row);
            return;
        }

        rebindUI.actionName = action.name;
        rebindUI.bindingIndex = bindingIndex;

    }

    void OnResetAll()
    {
        InputManager.Instance?.ResetAllBindings();
    }

    bool MatchesDeviceFilter(InputBinding binding)
    {
        string path = binding.effectivePath;
        if (string.IsNullOrEmpty(path)) return false;

        string pathLower = path.ToLowerInvariant();

        return showDevices switch
        {
            DeviceFilter.KeyboardMouse =>
                pathLower.Contains("<keyboard>") ||
                pathLower.Contains("<mouse>"),

            DeviceFilter.Gamepad =>
                pathLower.Contains("<gamepad>") ||
                pathLower.Contains("<joystick>") ||
                pathLower.Contains("<xinputcontroller>") ||
                pathLower.Contains("<dualshockgamepad>") ||
                pathLower.Contains("<switchprocontroller>"),

            _ => true
        };
    }

    string FormatName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
                sb.Append(' ');
            sb.Append(c);
        }

        return sb.ToString();
    }
}