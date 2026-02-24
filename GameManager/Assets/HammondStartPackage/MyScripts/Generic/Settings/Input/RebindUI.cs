using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RebindUI : MonoBehaviour
{
    [Header("action")]
    public string actionName;
    public int bindingIndex = 0;

    [Header("ui")]
    public TextMeshProUGUI bindingText;
    public Button rebindButton;
    public Button resetButton;
    public GameObject waitingOverlay;
    public TextMeshProUGUI actionLabel;
    public TextMeshProUGUI duplicateWarning;
    public Color duplicateColor = new Color(1f, 0.4f, 0.3f, 1f);
    public Color normalColor = Color.white;

    InputAction _action;
    InputActionRebindingExtensions.RebindingOperation _currentRebind;
    bool _valid;

    void Start()
    {

        if (InputManager.Instance == null)
        {
            return;
        }

        _action = InputManager.Instance.FindAction(actionName);

        if (_action == null)
        {
            return;
        }

        if (bindingIndex < 0 || bindingIndex >= _action.bindings.Count)
        {
            gameObject.SetActive(false);
            return;
        }

        if (_action.bindings[bindingIndex].isComposite)
        {
            gameObject.SetActive(false);
            return;
        }

        _valid = true;

        if (actionLabel != null)
            actionLabel.text = FormatActionName(actionName);

        if (rebindButton != null)
            rebindButton.onClick.AddListener(StartRebind);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetBinding);

        InputManager.Instance.OnBindingsChanged += RefreshDisplay;

        RefreshDisplay();
    }

    void OnDestroy()
    {
        _currentRebind?.Cancel();

        if (InputManager.Instance != null)
            InputManager.Instance.OnBindingsChanged -= RefreshDisplay;
    }


    void StartRebind()
    {
        if (!_valid || _action == null) return;

        if (bindingText != null) bindingText.text = "...";
        if (waitingOverlay != null) waitingOverlay.SetActive(true);
        if (rebindButton != null) rebindButton.interactable = false;

        _currentRebind = InputManager.Instance.StartRebind(
            _action,
            bindingIndex,
            onComplete: success =>
            {
                _currentRebind = null;
                if (waitingOverlay != null) waitingOverlay.SetActive(false);
                if (rebindButton != null) rebindButton.interactable = true;
                RefreshDisplay();
            }
        );
    }

    void ResetBinding()
    {
        if (!_valid || _action == null) return;

        _action.RemoveBindingOverride(bindingIndex);
        InputManager.Instance.ResetAction(_action);
        RefreshDisplay();
    }


    void RefreshDisplay()
    {
        if (!_valid || _action == null) return;

        if (bindingText != null)
        {
            string path = _action.bindings[bindingIndex].effectivePath;

            if (string.IsNullOrEmpty(path))
            {
                bindingText.text = "Unbound";
            }
            else
            {
                string display = InputControlPath.ToHumanReadableString(
                    path,
                    InputControlPath.HumanReadableStringOptions.OmitDevice);

                bindingText.text = string.IsNullOrEmpty(display) ? "Unbound" : display;
            }
        }

        CheckDuplicate();
    }

    void CheckDuplicate()
    {
        if (!_valid || _action == null) return;

        InputAction conflict = InputManager.Instance.GetDuplicateBinding(_action, bindingIndex);

        if (conflict != null)
        {
            if (duplicateWarning != null)
            {
                duplicateWarning.gameObject.SetActive(true);
                duplicateWarning.text = $"conflicts with {FormatActionName(conflict.name)}";
            }

            if (bindingText != null)
                bindingText.color = duplicateColor;
        }
        else
        {
            if (duplicateWarning != null)
                duplicateWarning.gameObject.SetActive(false);

            if (bindingText != null)
                bindingText.color = normalColor;
        }
    }


    string FormatActionName(string name)
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