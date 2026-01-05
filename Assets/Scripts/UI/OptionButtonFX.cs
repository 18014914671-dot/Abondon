using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OptionButtonFX : MonoBehaviour
{
    [Header("UI")]
    public RectTransform target;      // 不填就用自己
    public Image targetImage;         // 用来闪一下
    public Color flashColor = Color.white;
    [Range(0f, 1f)] public float flashAlpha = 0.35f;

    [Header("Punch")]
    public float punchScale = 1.12f;
    public float punchIn = 0.06f;
    public float punchOut = 0.10f;

    [Header("Particles (optional)")]
    public ParticleSystem burstParticles;

    void Reset()
    {
        target = GetComponent<RectTransform>();
        targetImage = GetComponent<Image>();
    }

    public void PlayHitFX()
    {
        if (!target) target = GetComponent<RectTransform>();

        if (burstParticles) burstParticles.Play(true);
        StopAllCoroutines();
        StartCoroutine(PunchRoutine());
        StartCoroutine(FlashRoutine());
    }

    IEnumerator PunchRoutine()
    {
        Vector3 baseScale = target.localScale;
        Vector3 peak = baseScale * punchScale;

        float t = 0f;
        while (t < punchIn)
        {
            t += Time.unscaledDeltaTime;
            target.localScale = Vector3.Lerp(baseScale, peak, t / punchIn);
            yield return null;
        }

        t = 0f;
        while (t < punchOut)
        {
            t += Time.unscaledDeltaTime;
            target.localScale = Vector3.Lerp(peak, baseScale, t / punchOut);
            yield return null;
        }

        target.localScale = baseScale;
    }

    IEnumerator FlashRoutine()
    {
        if (!targetImage) yield break;

        Color baseC = targetImage.color;
        Color flashC = flashColor;
        flashC.a = flashAlpha;

        // 快闪
        targetImage.color = Color.Lerp(baseC, flashC, 1f);
        yield return new WaitForSecondsRealtime(0.05f);
        targetImage.color = baseC;
    }
}
