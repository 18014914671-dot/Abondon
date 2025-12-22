using UnityEngine;

public class PlanetRotate : MonoBehaviour
{
    [Header("自转轴向（默认 Y 轴）")]
    public Vector3 rotationAxis = new Vector3(0f, 1f, 0f);

    [Header("旋转速度（度/秒）")]
    public float rotationSpeed = 20f;

    [Header("是否在被选中时加速")]
    public bool accelerateWhenSelected = false;

    [Header("被选中时倍率（仅在开启 accelerateWhenSelected 时有效）")]
    public float selectedSpeedMultiplier = 2f;

    // 由 LevelSelectManager 调用
    [HideInInspector] public bool isSelected = false;

    private void Update()
    {
        float speed = rotationSpeed;

        if (accelerateWhenSelected && isSelected)
        {
            speed *= selectedSpeedMultiplier;
        }

        transform.Rotate(rotationAxis, speed * Time.deltaTime, Space.Self);
    }
}
