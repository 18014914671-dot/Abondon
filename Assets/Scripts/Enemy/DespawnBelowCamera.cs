using UnityEngine;

/// <summary>
/// 物体低于相机下边界一定距离后销毁（2D 正交）
/// 放置：Assets/Scripts/Systems/Spawning/DespawnBelowCamera.cs
/// </summary>
public class DespawnBelowCamera : MonoBehaviour
{
    public Camera targetCamera;
    public float margin = 2f;
    public bool destroy = true;

    private void Update()
    {
        if (!targetCamera) targetCamera = Camera.main;
        if (!targetCamera) return;

        float bottomY = targetCamera.transform.position.y - targetCamera.orthographicSize;
        if (transform.position.y < bottomY - margin)
        {
            if (destroy) Destroy(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
