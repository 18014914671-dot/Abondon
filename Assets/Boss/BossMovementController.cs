using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class BossMovementController : MonoBehaviour
{
    [Header("Horizontal Patrol (Raiden-like)")]
    public float patrolSpeed = 2.5f;          // 左右移动速度
    public float patrolRange = 3.5f;          // 左右摆动幅度（世界坐标）
    public float centerFollowLerp = 1.5f;     // Boss中心点跟随（如果你想让它稍微跟着相机）

    [Header("Vertical Anchor")]
    public float fixedY = 3.5f;               // Boss 常态停在屏幕上方某个高度
    public bool lockY = true;

    [Header("State")]
    [Tooltip("蓄力/虚弱等阶段需要停住时设 true")]
    public bool freezeMovement = false;

    private Rigidbody2D _rb;
    private Vector2 _spawnCenter;
    private float _t;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
    }

    private void Start()
    {
        _spawnCenter = _rb.position;
        if (lockY) _spawnCenter.y = fixedY;
    }

    private void FixedUpdate()
    {
        if (freezeMovement) return;

        _t += Time.fixedDeltaTime;

        // 以 spawnCenter 为中心做左右正弦巡航
        float x = _spawnCenter.x + Mathf.Sin(_t * patrolSpeed) * patrolRange;
        float y = lockY ? fixedY : _rb.position.y;

        _rb.MovePosition(new Vector2(x, y));
    }

    /// <summary>
    /// 进入蓄力/弱点阶段时调用：停住
    /// </summary>
    public void Freeze(bool freeze)
    {
        freezeMovement = freeze;
        if (freeze)
        {
            // 停住时把刚体速度清掉（避免被外力影响）
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
    }

    /// <summary>
    /// 如果你希望 Boss 以相机 X 为中心巡航（雷电那种永远在上方跟着屏幕）
    /// </summary>
    public void SetCenterX(float x)
    {
        _spawnCenter.x = Mathf.Lerp(_spawnCenter.x, x, Time.fixedDeltaTime * centerFollowLerp);
    }
}
