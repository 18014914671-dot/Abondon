using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TimingRingPanel : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("UI")]
    public Image ringFill;              // Radial360 fill
    public RectTransform needle;        // optional: rotates around
    public TMP_Text wordText;           // optional
    public TMP_Text hintText;           // optional

    [Header("Timing")]
    [Range(0f, 1f)] public float windowStart = 0.35f;
    [Range(0f, 1f)] public float windowEnd = 0.55f;

    private float _t01;                 // 0..1
    private float _duration = 1f;
    private bool _running;

    public float Current01 => _t01;
    public bool Running => _running;

    public void Show(string word, float duration, float winStart01, float winEnd01)
    {
        _duration = Mathf.Max(0.05f, duration);
        windowStart = Mathf.Clamp01(winStart01);
        windowEnd = Mathf.Clamp01(winEnd01);
        if (windowEnd < windowStart) (windowStart, windowEnd) = (windowEnd, windowStart);

        _t01 = 0f;
        _running = true;

        if (root) root.SetActive(true);
        if (wordText) wordText.text = word;
        if (hintText) hintText.text = "Enter at the right moment";

        RefreshVisual();
    }

    public void Hide()
    {
        _running = false;
        if (root) root.SetActive(false);
    }

    private void Update()
    {
        if (!_running) return;

        _t01 += Time.unscaledDeltaTime / _duration;
        if (_t01 >= 1f)
        {
            _t01 = 1f;
            RefreshVisual();
            // 到头了不自动 Hide，让上层决定是 fail 还是继续
            _running = false;
            return;
        }

        RefreshVisual();
    }

    public bool IsInWindow()
    {
        return _t01 >= windowStart && _t01 <= windowEnd;
    }

    private void RefreshVisual()
    {
        if (ringFill) ringFill.fillAmount = _t01;

        if (needle)
        {
            // 0..1 -> 0..360 旋转（你可以按美术需求改方向）
            float ang = -360f * _t01;
            needle.localRotation = Quaternion.Euler(0f, 0f, ang);
        }
    }
}
