using UnityEngine;

public class BackgroundLooper2D : MonoBehaviour
{
    [Header("Two background pieces (same sprite size)")]
    public Transform bgA;
    public Transform bgB;

    [Header("Scroll settings")]
    public float scrollSpeed = 3f;

    [Header("Auto height (SpriteRenderer bounds)")]
    public bool autoDetectHeight = true;
    public float manualHeight = 20f;

    [Header("Align to Camera on Start")]
    public bool alignToCameraOnStart = true;
    public Camera targetCamera; // 可不填，默认 Camera.main

    private float _height;

    private void Start()
    {
        _height = manualHeight;

        if (autoDetectHeight && bgA != null)
        {
            var sr = bgA.GetComponent<SpriteRenderer>();
            if (sr != null)
                _height = sr.bounds.size.y; // 你这里会是 12.8
        }

        if (bgA == null || bgB == null) return;

        // ✅ 关键：开局把背景强制对齐到相机视野中心
        if (alignToCameraOnStart)
        {
            var cam = targetCamera != null ? targetCamera : Camera.main;
            float camY = cam != null ? cam.transform.position.y : 0f;

            bgA.position = new Vector3(bgA.position.x, camY, bgA.position.z);
            bgB.position = new Vector3(bgA.position.x, camY + _height, bgA.position.z);
        }
        else
        {
            // 保留你原来的逻辑
            bgB.position = new Vector3(bgA.position.x, bgA.position.y + _height, bgA.position.z);
        }
    }

    private void Update()
    {
        if (bgA == null || bgB == null) return;

        float dy = scrollSpeed * Time.deltaTime;

        bgA.position += Vector3.down * dy;
        bgB.position += Vector3.down * dy;

        var cam = targetCamera != null ? targetCamera : Camera.main;
        float camY = cam != null ? cam.transform.position.y : 0f;
        float screenBottom = camY - cam.orthographicSize;

        // A 出屏 → 放到 B 上方
        if (bgA.position.y + _height / 2f < screenBottom)
        {
            bgA.position = new Vector3(
                bgA.position.x,
                bgB.position.y + _height,
                bgA.position.z
            );
        }

        // B 出屏 → 放到 A 上方
        if (bgB.position.y + _height / 2f < screenBottom)
        {
            bgB.position = new Vector3(
                bgB.position.x,
                bgA.position.y + _height,
                bgB.position.z
            );
        }
    }
}