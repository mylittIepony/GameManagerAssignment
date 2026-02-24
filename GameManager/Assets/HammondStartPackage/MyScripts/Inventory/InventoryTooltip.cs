using DG.Tweening;
using TMPro;
using UnityEngine;

public class InventoryTooltip : MonoBehaviour
{
    [Header("refs")]
    public GameObject tooltipPanel;
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI descriptionText;

    Transform _originalParent;
    Canvas _rootCanvas;
    Tween _fadeTween;

    void Awake()
    {
        _originalParent = transform.parent;

        _rootCanvas = GetComponentInParent<Canvas>();
        while (_rootCanvas != null && !_rootCanvas.isRootCanvas)
            _rootCanvas = _rootCanvas.transform.parent?.GetComponentInParent<Canvas>();

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    public void Show(InventoryItemData itemData, float delayIn, float fadeDuration)
    {
        if (itemData == null) return;

        if (nameText != null) nameText.text = itemData.itemName;
        if (typeText != null) typeText.text = itemData.itemType;
        if (descriptionText != null) descriptionText.text = itemData.description;

        if (_rootCanvas != null)
            transform.SetParent(_rootCanvas.transform, true);

        if (tooltipPanel != null) tooltipPanel.SetActive(true);

        _fadeTween?.Kill();
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        _fadeTween = canvasGroup
            .DOFade(1f, fadeDuration)
            .SetDelay(delayIn)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true); 
    }

    public void Hide(float fadeDuration)
    {
        _fadeTween?.Kill();

        if (canvasGroup == null) return;

        _fadeTween = canvasGroup
            .DOFade(0f, fadeDuration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (tooltipPanel != null) tooltipPanel.SetActive(false);
                transform.SetParent(_originalParent, true);
            });
    }
}