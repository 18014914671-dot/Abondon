// Assets/Scripts/Boss/BossDamageFilter.cs
using UnityEngine;

public class BossDamageFilter : MonoBehaviour, IDamageFilter
{
    [Header("Refs")]
    public MonoBehaviour bossController; // 先用 MonoBehaviour 避免你类名不一致导致编译错误

    [Header("Damage Rules")]
    public bool allowDamageOnlyWhenVulnerable = true;

    [Tooltip("BossController 上用于判断“是否虚弱”的方法名（可选）")]
    public string isVulnerableMethodName = "IsVulnerable";

    [Tooltip("BossController 上用于判断“当前状态是否 Vulnerable”的 bool 字段名（可选）")]
    public string isVulnerableBoolFieldName = "isVulnerable";

    void Reset()
    {
        if (bossController == null)
            bossController = GetComponent<MonoBehaviour>();
    }

    void Awake()
    {
        if (bossController == null)
            bossController = GetComponent<MonoBehaviour>();
    }

    public bool CanTakeDamage(int amount, object source = null)
    {
        if (!allowDamageOnlyWhenVulnerable) return true;
        if (bossController == null) return true; // 没绑就不拦截，避免卡死

        // ✅ 最稳：用反射兼容你现有 BossController 命名（你不用为了我改类名）
        // 优先调用方法 IsVulnerable()
        var t = bossController.GetType();

        var method = t.GetMethod(isVulnerableMethodName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (method != null && method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
        {
            return (bool)method.Invoke(bossController, null);
        }

        // 次选：读 bool 字段 isVulnerable
        var field = t.GetField(isVulnerableBoolFieldName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (field != null && field.FieldType == typeof(bool))
        {
            return (bool)field.GetValue(bossController);
        }

        // 找不到任何标记，就默认允许（不然你会“怎么都打不动”）
        return true;
    }
}
