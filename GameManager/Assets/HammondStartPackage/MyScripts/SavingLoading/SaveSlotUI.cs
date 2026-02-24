using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SaveSlotUI : MonoBehaviour
{
    public GameObject slotRowPrefab;
    public Transform container;

    [Header("first time scene")]
    public string newGameScene = "SampleScene";

    public GameObject confirmPanel;
    public Button confirmDeleteButton;
    public Button cancelDeleteButton;
    public TextMeshProUGUI confirmText;

    int _pendingDeleteSlot = -1;

    void Start()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        if (confirmDeleteButton != null)
            confirmDeleteButton.onClick.AddListener(ConfirmDelete);

        if (cancelDeleteButton != null)
            cancelDeleteButton.onClick.AddListener(CancelDelete);

        GenerateSlots();
    }

    void GenerateSlots()
    {
        if (slotRowPrefab == null || container == null) return;

        foreach (Transform child in container)
            Destroy(child.gameObject);

        int slotCount = SaveManager.Instance != null ? SaveManager.Instance.maxSaveSlots : 3;

        for (int i = 0; i < slotCount; i++)
        {
            int slotIndex = i;
            GameObject row = Instantiate(slotRowPrefab, container);
            row.name = $"SaveSlot_{i}";

            SaveSlotRow slotRow = row.GetComponent<SaveSlotRow>();
            if (slotRow == null) continue;

            bool hasData = SaveManager.SlotHasData(slotIndex);
            string info = SaveManager.GetSlotInfo(slotIndex);

            if (slotRow.slotLabel != null)
                slotRow.slotLabel.text = info;

            if (slotRow.playButton != null)
            {
                slotRow.playButton.onClick.AddListener(() => OnPlaySlot(slotIndex));

                if (slotRow.playButtonText != null)
                    slotRow.playButtonText.text = hasData ? "continue" : "new game";
            }

            if (slotRow.deleteButton != null)
            {
                slotRow.deleteButton.interactable = hasData;
                slotRow.deleteButton.onClick.AddListener(() => OnDeleteSlot(slotIndex));
            }
        }
    }

    void OnPlaySlot(int slot)
    {
        SaveManager.SetActiveSlot(slot);

        string savedScene = SaveManager.Get("_meta/scene", "");

        if (!string.IsNullOrEmpty(savedScene))
            SceneManager.LoadScene(savedScene);
        else
            SceneManager.LoadScene(newGameScene);
    }

    void OnDeleteSlot(int slot)
    {
        _pendingDeleteSlot = slot;

        if (confirmPanel != null)
        {
            confirmPanel.SetActive(true);

            if (confirmText != null)
                confirmText.text = $"delete slot {slot + 1}?";
        }
        else
        {
            ConfirmDelete();
        }
    }

    void ConfirmDelete()
    {
        if (_pendingDeleteSlot >= 0)
        {
            SaveManager.DeleteSlot(_pendingDeleteSlot);
            _pendingDeleteSlot = -1;
        }

        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        GenerateSlots();
    }

    void CancelDelete()
    {
        _pendingDeleteSlot = -1;

        if (confirmPanel != null)
            confirmPanel.SetActive(false);
    }
}