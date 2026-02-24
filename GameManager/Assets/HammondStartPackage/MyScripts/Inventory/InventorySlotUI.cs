using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("refs")]
    public Image backgroundImage;
    public Image iconImage;
    public TextMeshProUGUI slotNumberText;
    public TextMeshProUGUI quantityText;
    public GameObject selectedIndicator;

    [Header("tooltip")]
    public InventoryTooltip tooltip;
    public float hoverDelayIn = 0.3f;
    public float hoverFadeDuration = 0.2f;

    [Header("colors")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color selectedColor = new Color(0.4f, 0.6f, 1f, 0.9f);
    public Color emptyIconColor = new Color(1f, 1f, 1f, 0.3f);

    [Header("empty slot")]
    public Sprite emptySlotSprite;

    [Header("hover wobble")]
    public float wobbleAngle = 8f;   
    public float wobbleDuration = 0.12f;
    public float hoverScaleMult = 1.08f; 

    [Header("selected pulse")]
    public float selectedScaleMult = 1.12f; 
    public float selectedPulseLowMult = 1.05f; 
    public float selectedPulseHighMult = 1.08f; 
    public float selectPopDuration = 0.15f;
    public float pulseSpeed = 0.9f;  

    [Header("color tween")]
    public float colorTweenDuration = 0.15f;

    int _slotIndex;
    InventoryManager _manager;
    InventorySlot _currentSlot;
    bool _isSelected;

    RectTransform _rect;
    Vector3 _baseScale;   
    Tween _scaleTween;
    Tween _wobbleTween;
    Tween _colorTween;
    Sequence _pulseSequence;


    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _baseScale = _rect != null ? _rect.localScale : Vector3.one;
    }

    void OnDestroy()
    {
        KillAll();
    }


    public void Initialize(int index, InventoryManager manager)
    {
        _slotIndex = index;
        _manager = manager;
    }


    public void UpdateSlot(InventorySlot slot, bool isActive, int displayNumber)
    {

        _currentSlot = slot;

        if (slotNumberText != null)
            slotNumberText.text = (displayNumber + 1).ToString();

        if (backgroundImage != null)
        {
            Color target = isActive ? selectedColor : normalColor;
            _colorTween?.Kill();
            _colorTween = backgroundImage
                .DOColor(target, colorTweenDuration)
                .SetEase(Ease.OutQuad);
        }

        if (selectedIndicator != null)
            selectedIndicator.SetActive(isActive);

        bool wasSelected = _isSelected;
        _isSelected = isActive;

        if (isActive && !wasSelected)
            PlaySelectPop();
        else if (!isActive && wasSelected)
            PlayDeselect();

        if (slot.IsEmpty)
        {
            ShowEmptyIcon();
            if (quantityText != null) quantityText.text = "";
            tooltip?.Hide(hoverFadeDuration);
        }
        else
        {
            if (iconImage != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = slot.itemData.inventoryIcon;
                iconImage.color = Color.white;
            }

            if (quantityText != null)
                quantityText.text = (slot.itemData.isStackable && slot.quantity > 1)
                    ? slot.quantity.ToString()
                    : "";
        }
    }

    void ShowEmptyIcon()
    {
        if (iconImage == null) return;

        if (emptySlotSprite != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = emptySlotSprite;
            iconImage.color = emptyIconColor;
        }
        else
        {
            iconImage.enabled = false;
        }
    }


    void PlaySelectPop()
    {
        if (_rect == null) return;
        KillScaleAndPulse();

        _scaleTween = _rect
            .DOScale(_baseScale * selectedScaleMult, selectPopDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(StartPulse);
    }

    void StartPulse()
    {
        if (_rect == null || !_isSelected) return;

        _pulseSequence = DOTween.Sequence()
            .Append(_rect.DOScale(_baseScale * selectedPulseLowMult, pulseSpeed).SetEase(Ease.InOutSine))
            .Append(_rect.DOScale(_baseScale * selectedPulseHighMult, pulseSpeed).SetEase(Ease.InOutSine))
            .SetLoops(-1, LoopType.Restart);
    }

    void PlayDeselect()
    {
        if (_rect == null) return;
        KillScaleAndPulse();

        _scaleTween = _rect
            .DOScale(_baseScale, selectPopDuration)
            .SetEase(Ease.InOutQuad);
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltip != null && _currentSlot != null && !_currentSlot.IsEmpty)
            tooltip.Show(_currentSlot.itemData, hoverDelayIn, hoverFadeDuration);

        if (!_isSelected)
            PlayHoverEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltip?.Hide(hoverFadeDuration);

        if (!_isSelected)
            PlayHoverExit();

        StopWobble();
    }

    void PlayHoverEnter()
    {
        if (_rect == null) return;

        _scaleTween?.Kill();
        _scaleTween = _rect
            .DOScale(_baseScale * hoverScaleMult, 0.12f)
            .SetEase(Ease.OutBack);

        StartWobble();
    }

    void PlayHoverExit()
    {
        if (_rect == null) return;

        _scaleTween?.Kill();
        _scaleTween = _rect
            .DOScale(_baseScale, 0.1f)
            .SetEase(Ease.InOutQuad);
    }

    void StartWobble()
    {
        StopWobble();
        if (_rect == null) return;

        _wobbleTween = _rect
            .DORotate(new Vector3(0f, 0f, wobbleAngle), wobbleDuration, RotateMode.Fast)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                _wobbleTween = _rect
                    .DORotate(Vector3.zero, wobbleDuration * 4f, RotateMode.Fast)
                    .SetEase(Ease.OutElastic);
            });
    }

    void StopWobble()
    {
        _wobbleTween?.Kill();
        _wobbleTween = null;
        if (_rect != null) _rect.localRotation = Quaternion.identity;
    }


    void KillScaleAndPulse()
    {
        _scaleTween?.Kill();
        _pulseSequence?.Kill();
        _scaleTween = null;
        _pulseSequence = null;
    }

    void KillAll()
    {
        KillScaleAndPulse();
        _wobbleTween?.Kill();
        _colorTween?.Kill();
    }
}