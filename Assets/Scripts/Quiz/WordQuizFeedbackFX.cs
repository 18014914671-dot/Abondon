using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WordQuizFeedbackFX : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("用于抖动的 UI 根节点（建议拖 Canvas 下的某个根容器，例如 HUDRoot）")]
    public RectTransform shakeRoot;

    [Tooltip("三个选项按钮（和 WordQuizManager 的 optionButtons 对应）")]
    public Button[] optionButtons;

    [Header("Burst Prefabs (UI)")]
    [Tooltip("答对时在按钮位置生成的 UI 特效 Prefab（RectTransform）")]
    public RectTransform correctBurstPrefab;

    [Tooltip("答错时在按钮位置生成的 UI 特效 Prefab（RectTransform）")]
    public RectTransform wrongBurstPrefab;

    [Header("Timing")]
    public float lockDuration = 0.18f;      // 这段时间内锁住按钮，避免连续按
    public float buttonPunchScale = 1.10f;  // 按钮“啪”一下的幅度
    public float buttonPunchTime = 0.10f;

    [Header("Shake")]
    public float shakeDuration = 0.12f;
    public float shakeStrength = 16f;       // UI 抖动像素强度（越大越抖）

    [Header("Tint (Optional)")]
    [Tooltip("按钮 Image 颜色闪一下（可不填）")]
    public Color correctTint = new Color(0.4f, 1f, 0.9f, 1f);
    public Color wrongTint = new Color(1f, 0.35f, 0.35f, 1f);
    public float tintTime = 0.12f;

    private bool _busy;

    public void PlaySubmitFX(int chosenIndex, bool isCorrect)
    {
        if (_busy) return;
        if (optionButtons == null || chosenIndex < 0 || chosenIndex >= optionButtons.Length) return;

        StartCoroutine(Co_Play(chosenIndex, isCorrect));
    }

    private IEnumerator Co_Play(int chosenIndex, bool isCorrect)
    {
        _busy = true;

        // 1) 锁住按钮一小会（防止同帧连点/连按）
        SetButtonsInteractable(false);

        // 2) 选中按钮脉冲（scale punch）+ tint
        var btn = optionButtons[chosenIndex];
        if (btn != null)
        {
            var rt = btn.GetComponent<RectTransform>();
            if (rt != null) StartCoroutine(Co_Punch(rt, buttonPunchScale, buttonPunchTime));

            var img = btn.GetComponent<Image>();
            if (img != null) StartCoroutine(Co_Tint(img, isCorrect ? correctTint : wrongTint, tintTime));
        }

        // 3) Burst 特效（UI Prefab）生成在按钮中心
        SpawnBurstAtButton(chosenIndex, isCorrect);

        // 4) UI 抖动（抖 shakeRoot）
        if (shakeRoot != null) yield return Co_Shake(shakeRoot, shakeDuration, shakeStrength);
        else yield return new WaitForSeconds(shakeDuration);

        // 5) 解锁
        yield return new WaitForSeconds(Mathf.Max(0f, lockDuration - shakeDuration));
        SetButtonsInteractable(true);

        _busy = false;
    }

    private void SetButtonsInteractable(bool v)
    {
        if (optionButtons == null) return;
        foreach (var b in optionButtons)
        {
            if (b == null) continue;
            b.interactable = v;
        }
    }

    private IEnumerator Co_Punch(RectTransform rt, float scaleMul, float time)
    {
        if (rt == null) yield break;
        Vector3 baseScale = rt.localScale;
        Vector3 target = baseScale * scaleMul;

        float t = 0f;
        // 快速放大
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / time);
            rt.localScale = Vector3.Lerp(baseScale, target, EaseOutCubic(k));
            yield return null;
        }

        t = 0f;
        // 快速回弹
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / time);
            rt.localScale = Vector3.Lerp(target, baseScale, EaseOutCubic(k));
            yield return null;
        }

        rt.localScale = baseScale;
    }

    private IEnumerator Co_Tint(Image img, Color flash, float time)
    {
        if (img == null) yield break;
        Color baseColor = img.color;

        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / time);
            img.color = Color.Lerp(baseColor, flash, EaseOutCubic(k));
            yield return null;
        }

        t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / time);
            img.color = Color.Lerp(flash, baseColor, EaseOutCubic(k));
            yield return null;
        }

        img.color = baseColor;
    }

    private IEnumerator Co_Shake(RectTransform target, float duration, float strength)
    {
        if (target == null) yield break;

        Vector2 basePos = target.anchoredPosition;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / duration); // 越来越弱
            float x = (Random.value * 2f - 1f) * strength * k;
            float y = (Random.value * 2f - 1f) * strength * k;
            target.anchoredPosition = basePos + new Vector2(x, y);
            yield return null;
        }

        target.anchoredPosition = basePos;
    }

    private void SpawnBurstAtButton(int index, bool isCorrect)
    {
        RectTransform prefab = isCorrect ? correctBurstPrefab : wrongBurstPrefab;
        if (prefab == null) return;

        var btn = optionButtons[index];
        if (btn == null) return;

        RectTransform btnRT = btn.GetComponent<RectTransform>();
        if (btnRT == null) return;

        // 实例化到同一个 Canvas 层级里（prefab 的父节点用 shakeRoot 的父级或 Canvas）
        Transform parent = shakeRoot != null ? shakeRoot : btnRT.root;
        RectTransform inst = Instantiate(prefab, parent);
        inst.gameObject.SetActive(true);

        // 把特效放到按钮中心：同一 Canvas 里用世界坐标对齐即可
        inst.position = btnRT.position;

        // 自动销毁（避免堆垃圾）
        Destroy(inst.gameObject, 1.5f);
    }

    private float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
}
