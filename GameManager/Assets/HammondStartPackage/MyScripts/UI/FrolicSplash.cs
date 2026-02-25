using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using TMPro;

public class FrolicSplash : MonoBehaviour
{
    [Header("pony babies")]
    [SerializeField] CanvasGroup comicPony;
    [SerializeField] CanvasGroup colourPony;

    [Header("letters")]
    [SerializeField] RectTransform[] letters;
    [SerializeField] CanvasGroup[] letterGroups;
    [SerializeField] TMP_Text[] letterTexts;

    [Header("fade overlay")]
    [SerializeField] CanvasGroup blackOverlay;

    [Header("sceneshit")]
    [SerializeField] int nextSceneIndex = 1;
    [SerializeField] bool loadNextScene = true;

    [Header("timings")]
    [SerializeField] float openFromBlackDuration = 0.5f;
    [SerializeField] float comicFadeInDuration = 0.6f;
    [SerializeField] float comicHoldBeforeLetters = 0.2f;
    [SerializeField] float letterStaggerInterval = 0.12f;
    [SerializeField] float letterDropDuration = 0.55f;
    [SerializeField] float holdAfterLettersDuration = 0.6f;
    [SerializeField] float crossfadeDuration = 1.2f;
    [SerializeField] float holdAfterColourDuration = 0.9f;
    [SerializeField] float fadeToBlackDuration = 0.8f;

    [Header("letter drop")]
    [SerializeField] float letterDropDistance = 120f;
    [SerializeField] float elasticAmplitude = 1f;
    [SerializeField] float elasticPeriod = 0.4f;

    [Header("wobble (after colour reveal)")]
    [SerializeField] float wobbleStrength = 18f;
    [SerializeField] float wobbleDuration = 0.4f;
    [SerializeField] int wobbleVibrato = 10;
    [SerializeField] float wobbleStagger = 0.04f;
    [SerializeField] bool wobbleLetters = true;
    [SerializeField] bool wobblePony = true;

    [Header("gradient")]
    [SerializeField] Color gradientTopColour = Color.white;
    [SerializeField] Color gradientBottomStart = Color.black;
    [SerializeField] Color gradientBottomEnd = new Color(1f, 0.678f, 0.94f);

    void Start()
    {
        comicPony.alpha = 0f;
        colourPony.alpha = 0f;
        blackOverlay.alpha = 1f;

        foreach (var lg in letterGroups) lg.alpha = 0f;

        foreach (var tmp in letterTexts)
        {
            tmp.colorGradient = new VertexGradient(
                gradientTopColour, gradientTopColour,
                gradientBottomStart, gradientBottomStart
            );
        }

        float[] restingY = new float[letters.Length];
        for (int i = 0; i < letters.Length; i++)
        {
            restingY[i] = letters[i].anchoredPosition.y;
            var pos = letters[i].anchoredPosition;
            letters[i].anchoredPosition = new Vector2(pos.x, pos.y + letterDropDistance);
        }

        var seq = DOTween.Sequence();

        seq.Append(blackOverlay.DOFade(0f, openFromBlackDuration).SetEase(Ease.OutCubic));

        seq.Append(comicPony.DOFade(1f, comicFadeInDuration).SetEase(Ease.OutCubic));
        seq.AppendInterval(comicHoldBeforeLetters);

        float lettersStartTime = seq.Duration();

        for (int i = 0; i < letters.Length; i++)
        {
            int idx = i;
            float t = lettersStartTime + (i * letterStaggerInterval);

            seq.Insert(t, letters[idx]
                .DOAnchorPosY(restingY[idx], letterDropDuration)
                .SetEase(Ease.OutElastic, elasticAmplitude, elasticPeriod));

            seq.Insert(t, letterGroups[idx]
                .DOFade(1f, 0.12f));
        }

        float lastLetterLands = lettersStartTime + (letters.Length * letterStaggerInterval) + letterDropDuration;
        float crossfadeStart = lastLetterLands + holdAfterLettersDuration;

        seq.Insert(crossfadeStart, comicPony.DOFade(0f, crossfadeDuration).SetEase(Ease.InOutCubic));
        seq.Insert(crossfadeStart, colourPony.DOFade(1f, crossfadeDuration).SetEase(Ease.InOutCubic));

        foreach (var tmp in letterTexts)
        {
            var tmpRef = tmp;
            seq.Insert(crossfadeStart,
                DOTween.To(
                    () => 0f,
                    v =>
                    {
                        Color current = Color.Lerp(gradientBottomStart, gradientBottomEnd, v);
                        tmpRef.colorGradient = new VertexGradient(
                            gradientTopColour, gradientTopColour,
                            current, current
                        );
                    },
                    1f, crossfadeDuration
                ).SetEase(Ease.InOutCubic)
            );
        }

        float wobbleStart = crossfadeStart + crossfadeDuration;

        if (wobbleLetters)
        {
            for (int i = 0; i < letters.Length; i++)
            {
                int idx = i;
                float wt = wobbleStart + (i * wobbleStagger);
                seq.Insert(wt, letters[idx]
                    .DOShakeAnchorPos(wobbleDuration, new Vector2(0f, wobbleStrength), wobbleVibrato, 90f, false, true));
            }
        }

        if (wobblePony)
        {
            seq.Insert(wobbleStart, colourPony.transform
                .DOShakePosition(wobbleDuration, new Vector3(0f, wobbleStrength * 0.5f, 0f), wobbleVibrato, 90f, false, true));
        }

        seq.InsertCallback(wobbleStart + wobbleDuration, () => { });

        float wobbleEnd = wobbleStart + wobbleDuration + holdAfterColourDuration;
        float fadeOutStart = wobbleEnd;

        seq.Insert(fadeOutStart, blackOverlay.DOFade(1f, fadeToBlackDuration).SetEase(Ease.InCubic));


        seq.OnComplete(() =>
        {
            if (loadNextScene)
                SceneManager.LoadSceneAsync(nextSceneIndex);
        });
    }
}