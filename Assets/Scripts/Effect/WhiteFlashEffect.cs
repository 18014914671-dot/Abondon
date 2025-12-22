using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DataPickupFlash : MonoBehaviour
{
    [Header("闪烁频率（次/秒）")]
    public float flashFrequency = 4f;

    [Header("闪烁颜色（先用鲜艳色测试，例如黄色）")]
    public Color flashColor = Color.yellow;

    [Header("闪烁强度（0~1）")]
    [Range(0f, 1f)]
    public float flashStrength = 1f;

    private SpriteRenderer sr;
    private Color originalColor;
    private bool flashing = true;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;    // 通常是 white
    }

    void Update()
    {
        if (!flashing) return;

        // 0 ~ 1 的往复值
        float t = (Mathf.Sin(Time.time * flashFrequency * Mathf.PI * 2f) + 1f) * 0.5f;

        // 控制强度
        float lerpT = t * flashStrength;

        // 在原色 和 闪烁色 之间插值
        Color c = Color.Lerp(originalColor, flashColor, lerpT);
        sr.color = c;
    }

    public void StartFlash() => flashing = true;

    public void StopFlash()
    {
        flashing = false;
        sr.color = originalColor;
    }
}
