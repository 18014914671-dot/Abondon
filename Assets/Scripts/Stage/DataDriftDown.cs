using UnityEngine;

public class DataDriftDown : MonoBehaviour
{
    [Header("Down drift speed (world units/sec)")]
    public float driftSpeed = 1.5f;

    [Header("Optional: auto destroy when below camera")]
    public bool destroyBelowCamera = true;
    public float despawnBuffer = 2f;

    public Camera targetCamera; // 可不填，默认 Camera.main

    private void Update()
    {
        // 缓慢向下漂移
        transform.position += Vector3.down * driftSpeed * Time.deltaTime;

        if (!destroyBelowCamera) return;

        var cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;

        float bottomY = cam.transform.position.y - cam.orthographicSize;
        if (transform.position.y < bottomY - despawnBuffer)
        {
            Destroy(gameObject);
        }
    }
}
