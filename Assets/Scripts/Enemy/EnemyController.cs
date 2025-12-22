using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Optional: Homing (OFF for STG basic enemies)")]
    public bool homing = false;
    public float homingStrength = 1f; // 0~1，只有 homing=true 才有意义

    private Transform player;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;  // 不要乱转
    }

    private void Start()
    {
        // 只有需要追踪时才找玩家
        if (homing)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("EnemyController: 找不到 Tag=Player 的玩家物体（homing 已开启）");
            }
        }
    }

    // 用 FixedUpdate 配合 Rigidbody2D 更稳定
    private void FixedUpdate()
    {
        Vector2 nextPos;

        // ✅ STG 基础敌人：永远向下飞
        if (!homing || player == null)
        {
            nextPos = rb.position + Vector2.down * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(nextPos);
            return;
        }

        // （可选）追踪模式：保留给未来特殊敌人
        Vector2 dir = ((Vector2)player.position - rb.position).normalized;
        Vector2 desired = rb.position + dir * moveSpeed * Time.fixedDeltaTime;

        // homingStrength=1 -> 完全追踪；0 -> 等同不追踪
        nextPos = Vector2.Lerp(
    rb.position + Vector2.down * moveSpeed * Time.fixedDeltaTime,
    desired,
    Mathf.Clamp01(homingStrength)
);

        rb.MovePosition(nextPos);
    }
}
