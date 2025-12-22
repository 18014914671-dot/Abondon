using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class BlinkEffect : MonoBehaviour
{
    [Header("闪烁速度（次/秒）")]
    public float blinkFrequency = 4f;

    [Header("是否一开始就闪烁")]
    public bool playOnStart = true;

    private Renderer _renderer;
    private bool _isBlinking;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        if (playOnStart)
            StartBlink();
    }

    private void Update()
    {
        if (!_isBlinking || _renderer == null) return;

        // 用正弦波在 0~1 间来回
        float t = (Mathf.Sin(Time.time * blinkFrequency * Mathf.PI * 2f) + 1f) * 0.5f;

        // 0~1 中间设个阈值，低于阈值关掉，高于打开
        bool visible = t > 0.5f;
        _renderer.enabled = visible;
    }

    public void StartBlink()
    {
        _isBlinking = true;
    }

    public void StopBlink()
    {
        _isBlinking = false;
        if (_renderer != null)
            _renderer.enabled = true; // 结束时恢复显示
    }
}
