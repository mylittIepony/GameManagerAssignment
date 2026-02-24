using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public InputActionAsset inputActions;

    public string[] ignoreMapsForDuplicates;

    const string PrefKey = "InputRebinds";

    public event Action OnBindingsChanged;

    HashSet<string> _ignoredMaps;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _ignoredMaps = new HashSet<string>();
        if (ignoreMapsForDuplicates != null)
        {
            foreach (string map in ignoreMapsForDuplicates)
                _ignoredMaps.Add(map);
        }

        if (inputActions == null)
        {
            Debug.LogError("no InputActionAsset assigned");
            return;
        }

        LoadRebinds();
        inputActions.Enable();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }


    public InputAction FindAction(string actionName)
    {
        if (inputActions == null) return null;
        return inputActions.FindAction(actionName);
    }

    public InputAction FindAction(string mapName, string actionName)
    {
        if (inputActions == null) return null;
        var map = inputActions.FindActionMap(mapName);
        return map?.FindAction(actionName);
    }

    public List<InputAction> GetAllActions()
    {
        List<InputAction> all = new List<InputAction>();
        if (inputActions == null) return all;

        foreach (var map in inputActions.actionMaps)
        {
            foreach (var action in map.actions)
                all.Add(action);
        }

        return all;
    }

    public List<InputAction> GetActionsInMap(string mapName)
    {
        List<InputAction> result = new List<InputAction>();
        if (inputActions == null) return result;

        var map = inputActions.FindActionMap(mapName);
        if (map == null) return result;

        foreach (var action in map.actions)
            result.Add(action);

        return result;
    }


    public InputActionRebindingExtensions.RebindingOperation StartRebind(
        InputAction action,
        int bindingIndex,
        Action<bool> onComplete = null,
        Action<InputActionRebindingExtensions.RebindingOperation> onUpdate = null)
    {
        if (action == null) return null;

        action.Disable();

        var rebind = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("Mouse/position")
            .WithControlsExcluding("Mouse/delta")
            .WithControlsExcluding("Pointer/position")
            .WithControlsExcluding("Pointer/delta")
            .WithCancelingThrough("<Keyboard>/escape")
            .WithTimeout(10f);

        if (onUpdate != null)
            rebind.OnPotentialMatch(onUpdate);

        rebind.OnComplete(operation =>
        {
            operation.Dispose();
            action.Enable();
            SaveRebinds();
            OnBindingsChanged?.Invoke();
            onComplete?.Invoke(true);
        });

        rebind.OnCancel(operation =>
        {
            operation.Dispose();
            action.Enable();
            onComplete?.Invoke(false);
        });

        rebind.Start();
        return rebind;
    }

    public void ResetAction(InputAction action)
    {
        if (action == null) return;

        for (int i = 0; i < action.bindings.Count; i++)
            action.RemoveBindingOverride(i);

        SaveRebinds();
        OnBindingsChanged?.Invoke();
    }

    public void ResetAllBindings()
    {
        if (inputActions == null) return;

        foreach (var map in inputActions.actionMaps)
        {
            foreach (var action in map.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                    action.RemoveBindingOverride(i);
            }
        }

        SaveRebinds();
        OnBindingsChanged?.Invoke();
    }


    public InputAction GetDuplicateBinding(InputAction sourceAction, int bindingIndex)
    {
        if (inputActions == null || sourceAction == null) return null;

        string effectivePath = sourceAction.bindings[bindingIndex].effectivePath;
        if (string.IsNullOrEmpty(effectivePath)) return null;

        foreach (var map in inputActions.actionMaps)
        {
            if (_ignoredMaps != null && _ignoredMaps.Contains(map.name)) continue;

            foreach (var action in map.actions)
            {
                if (action == sourceAction) continue;

                for (int i = 0; i < action.bindings.Count; i++)
                {
                    if (action.bindings[i].isComposite) continue;

                    if (string.Equals(action.bindings[i].effectivePath, effectivePath,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return action;
                    }
                }
            }
        }

        return null;
    }

    public List<DuplicateInfo> GetAllDuplicates()
    {
        List<DuplicateInfo> duplicates = new List<DuplicateInfo>();
        if (inputActions == null) return duplicates;

        Dictionary<string, List<(InputAction action, int index)>> pathMap = new();

        foreach (var map in inputActions.actionMaps)
        {
            if (_ignoredMaps != null && _ignoredMaps.Contains(map.name)) continue;

            foreach (var action in map.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    if (action.bindings[i].isComposite) continue;
                    if (action.bindings[i].isPartOfComposite) continue;

                    string path = action.bindings[i].effectivePath;
                    if (string.IsNullOrEmpty(path)) continue;

                    string key = path.ToLowerInvariant();
                    if (!pathMap.ContainsKey(key))
                        pathMap[key] = new List<(InputAction, int)>();
                    pathMap[key].Add((action, i));
                }
            }
        }

        foreach (var kvp in pathMap)
        {
            if (kvp.Value.Count > 1)
            {
                duplicates.Add(new DuplicateInfo
                {
                    bindingPath = kvp.Key,
                    conflicts = kvp.Value
                });
            }
        }

        return duplicates;
    }


    void SaveRebinds()
    {
        if (inputActions == null) return;

        string json = inputActions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(PrefKey, json);
        PlayerPrefs.Save();
    }

    void LoadRebinds()
    {
        if (inputActions == null) return;

        string json = PlayerPrefs.GetString(PrefKey, null);
        if (!string.IsNullOrEmpty(json))
            inputActions.LoadBindingOverridesFromJson(json);
    }


    public struct DuplicateInfo
    {
        public string bindingPath;
        public List<(InputAction action, int index)> conflicts;
    }
}