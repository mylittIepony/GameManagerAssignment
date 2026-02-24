using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonScaler : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("scale")]
    public float selectedScale = 1.1f;
    public float scaleDuration = 0.3f;

    [Header("idle pulse")]
    public float pulseAmount = 0.03f;
    public float pulseDuration = 0.8f;

    [Header("wobble")]
    public float wobbleAngle = 5f;
    public float wobbleDuration = 0.6f;

    [Header("nighbour push")]
    public RectTransform buttonAbove;
    public RectTransform buttonBelow;
    public RectTransform buttonLeft;
    public RectTransform buttonRight;
    public float pushAngle = 3f;
    public float pushDuration = 0.4f;

    [Header("entrance")]
    public float entranceDelay = 0f;

    private Vector3 originalScale;
    private RectTransform rectTransform;
    private Tweener scaleTween;
    private Sequence pulseSequence;
    private Sequence seesawSequence;
    private bool isHovered;
    private BouncyText bouncyText;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;

        rectTransform.localScale = Vector3.zero;
        rectTransform.DOScale(originalScale, 0.5f)
            .SetEase(Ease.OutBack)
            .SetDelay(entranceDelay)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                DoSeesaw(rectTransform, 5f, 0.4f);
            });

        bouncyText = GetComponentInChildren<BouncyText>();
        rectTransform.pivot = new Vector2(0.5f, 0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;

        scaleTween?.Kill();
        scaleTween = rectTransform.DOScale(originalScale * selectedScale, scaleDuration)
            .SetEase(Ease.OutElastic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                StartPulse();
            });

        DoSeesaw(rectTransform, wobbleAngle, wobbleDuration);
        bouncyText?.TriggerBounce(1f);

        if (buttonAbove != null)
            DoSeesaw(buttonAbove, -pushAngle, pushDuration);
        if (buttonBelow != null)
            DoSeesaw(buttonBelow, pushAngle, pushDuration);
        if (buttonLeft != null)
        {
            DoSeesaw(buttonLeft, pushAngle, pushDuration);
            buttonLeft.GetComponentInChildren<BouncyText>()?.TriggerBounceSide(1f);
        }
        if (buttonRight != null)
        {
            DoSeesaw(buttonRight, -pushAngle, pushDuration);
            buttonRight.GetComponentInChildren<BouncyText>()?.TriggerBounceSide(-1f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        pulseSequence?.Kill();
        rectTransform.localRotation = Quaternion.identity;

        scaleTween?.Kill();
        scaleTween = rectTransform.DOScale(originalScale, 0.2f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!isHovered)
        {
            scaleTween?.Kill();
            scaleTween = rectTransform.DOScale(originalScale * selectedScale, scaleDuration)
                .SetEase(Ease.OutElastic)
                .SetUpdate(true)
                .OnComplete(() => StartPulse());

            DoSeesaw(rectTransform, wobbleAngle, wobbleDuration);
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!isHovered)
        {
            pulseSequence?.Kill();
            rectTransform.localRotation = Quaternion.identity;
            scaleTween?.Kill();
            scaleTween = rectTransform.DOScale(originalScale, 0.2f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }
    }

    private void StartPulse()
    {
        pulseSequence?.Kill();
        pulseSequence = DOTween.Sequence().SetUpdate(true);
        pulseSequence.Append(
            rectTransform.DOLocalRotate(new Vector3(0, 0, wobbleAngle * 0.3f), 0.6f)
                .SetEase(Ease.InOutSine)
        );
        pulseSequence.Append(
            rectTransform.DOLocalRotate(new Vector3(0, 0, -wobbleAngle * 0.3f), 0.6f)
                .SetEase(Ease.InOutSine)
        );
        pulseSequence.SetLoops(-1);
    }

    private void DoSeesaw(RectTransform target, float angle, float duration)
    {



        Sequence seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(
            target.DOLocalRotate(new Vector3(0, 0, angle), duration / 4f)
                .SetEase(Ease.OutQuad)
        );
        seq.Append(
            target.DOLocalRotate(new Vector3(0, 0, -angle * 0.6f), duration / 3f)
                .SetEase(Ease.InOutQuad)
        );
        seq.Append(
            target.DOLocalRotate(Vector3.zero, duration / 2.5f)
                .SetEase(Ease.OutBack)
        );
    }

    void OnDestroy()
    {
        scaleTween?.Kill();
        pulseSequence?.Kill();
        seesawSequence?.Kill();
    }
}