using System.Collections;
using UnityEngine;

public class ScreenShake2D : MonoBehaviour
{
    public static ScreenShake2D Instance { get; private set; }

    [Header("Target Camera (leave empty to use main camera)")]
    public Transform cameraTransform;

    [Header("Pixel Snap (optional)")]
    public bool pixelSnap = true;
    public float pixelsPerUnit = 32f; // 你的像素密度，按项目改

    Vector3 _originalPos;
    Coroutine _routine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
        if (cameraTransform) _originalPos = cameraTransform.localPosition;
    }

    public void Shake(float duration = 0.12f, float amplitude = 0.18f, float frequency = 30f)
    {
        if (!cameraTransform) return;
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ShakeRoutine(duration, amplitude, frequency));
    }

    IEnumerator ShakeRoutine(float duration, float amplitude, float frequency)
    {
        _originalPos = cameraTransform.localPosition;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;

            float damper = 1f - Mathf.Clamp01(t / duration);
            float x = (Mathf.PerlinNoise(0f, Time.unscaledTime * frequency) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(1f, Time.unscaledTime * frequency) - 0.5f) * 2f;

            Vector3 offset = new Vector3(x, y, 0f) * (amplitude * damper);
            Vector3 pos = _originalPos + offset;

            if (pixelSnap && pixelsPerUnit > 0.01f)
            {
                pos.x = Mathf.Round(pos.x * pixelsPerUnit) / pixelsPerUnit;
                pos.y = Mathf.Round(pos.y * pixelsPerUnit) / pixelsPerUnit;
            }

            cameraTransform.localPosition = pos;
            yield return null;
        }

        cameraTransform.localPosition = _originalPos;
        _routine = null;
    }
}
