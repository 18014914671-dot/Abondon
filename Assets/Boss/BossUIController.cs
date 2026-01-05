using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BossUIController : MonoBehaviour
{
    [Header("Root")]
    public GameObject root; // 整体显隐

    [Header("UI")]
    public TMP_Text bossNameText;
    public TMP_Text bossWordText;
    public Slider hpSlider;

    [Header("Debug")]
    public bool verboseLog = false;

    private BossController _boss;
    private EnemyHealth _health; // ✅ 真正的血量来源

    /// <summary>
    /// ✅ 绑定 Boss（运行时实例 Boss(Clone)）
    /// - 名字/单词从 BossController 来
    /// - 血量从 EnemyHealth 来（订阅事件自动更新）
    /// </summary>
    public void BindBoss(BossController boss)
    {
        // 先解绑旧的
        Unbind();

        _boss = boss;
        if (_boss == null)
        {
            SetVisible(false);
            return;
        }

        // 找 EnemyHealth（在 Boss 根物体上，或子物体上都行）
        _health = _boss.GetComponent<EnemyHealth>();
        if (_health == null)
            _health = _boss.GetComponentInChildren<EnemyHealth>();

        // 显示 UI
        SetVisible(true);

        // 先刷新静态信息（名字/单词）
        RefreshHeaderFromBoss();

        // 绑定血量事件
        if (_health != null)
        {
            _health.OnHealthChanged += HandleHealthChanged;

            // 立刻推一次当前血量到 UI（避免等到下一次受伤才更新）
            HandleHealthChanged(_health.CurrentHealth, _health.MaxHealth);

            if (verboseLog)
                Debug.Log($"[BossUI] Bound to Boss={_boss.name}, Health={_health.CurrentHealth}/{_health.MaxHealth}");
        }
        else
        {
            // 找不到 EnemyHealth：至少把 slider 设置到一个默认值，避免显示错
            if (hpSlider != null)
            {
                hpSlider.minValue = 0;
                hpSlider.maxValue = 1;
                hpSlider.value = 1;
            }

            if (verboseLog)
                Debug.LogWarning($"[BossUI] Boss={_boss.name} has NO EnemyHealth. Slider will not update.");
        }
    }
    // ✅ 兼容旧代码：别的脚本还在调用 RefreshFromBoss()
    public void RefreshFromBoss()
    {
        // 旧逻辑通常是：刷新名字/单词 + 刷新血量
        RefreshHeaderFromBoss();

        if (_health != null)
        {
            HandleHealthChanged(_health.CurrentHealth, _health.MaxHealth);
        }
        else if (_boss != null)
        {
            // 兜底：如果还没找到 EnemyHealth，就再找一次
            _health = _boss.GetComponent<EnemyHealth>() ?? _boss.GetComponentInChildren<EnemyHealth>();
            if (_health != null)
            {
                // 绑定事件（避免漏订阅）
                _health.OnHealthChanged -= HandleHealthChanged;
                _health.OnHealthChanged += HandleHealthChanged;

                HandleHealthChanged(_health.CurrentHealth, _health.MaxHealth);
            }
        }
    }

    /// <summary>
    /// ✅ 手动解绑（Boss 死亡/切换 Boss 时调用）
    /// </summary>
    public void Unbind()
    {
        if (_health != null)
        {
            _health.OnHealthChanged -= HandleHealthChanged;
            _health = null;
        }

        _boss = null;
    }

    public void SetVisible(bool visible)
    {
        if (root != null) root.SetActive(visible);
    }

    /// <summary>
    /// ✅ 只刷新 Boss 名字 + 当前单词（如果你单词会变，建议 Boss 在变更时调用一次）
    /// </summary>
    public void RefreshHeaderFromBoss()
    {
        if (_boss == null) return;

        if (bossNameText != null) bossNameText.text = _boss.bossName;
        if (bossWordText != null) bossWordText.text = _boss.CurrentWordText;
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (hpSlider == null) return;

        int safeMax = Mathf.Max(1, max);
        int safeCur = Mathf.Clamp(current, 0, safeMax);

        hpSlider.minValue = 0;
        hpSlider.maxValue = safeMax;
        hpSlider.value = safeCur;

        if (verboseLog)
            Debug.Log($"[BossUI] HP updated: {safeCur}/{safeMax}");
    }

    private void OnDisable()
    {
        // 防止 UI 被关闭/切场景时留下订阅
        Unbind();
    }
}
