using UnityEngine;

public class PlayerPlaneController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;

    [Header("Fixed Rotation (STG)")]
    public float fixedAngle = 90f;   // 90 = 永远朝上（Unity 2D）

    private Vector2 input;

    private void Start()
    {
        // 开局就锁定朝向
        transform.rotation = Quaternion.Euler(0f, 0f, fixedAngle);
    }

    private void Update()
    {
        // 1. 输入
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        input = new Vector2(h, v);
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        // 2. 移动（8 向）
        transform.position += (Vector3)input * moveSpeed * Time.deltaTime;

        // 3. 不再根据输入旋转（雷电规则）
        // 朝向始终保持 fixedAngle
    }
}
