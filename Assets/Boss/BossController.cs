using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class BossController : MonoBehaviour
{
    public enum BossPhase
    {
        Inactive,
        Throwing,      // 正常投弹阶段（不能打字扣血）
        Charging,      // 蓄力打字阶段（通常也不能扣血，或只计数）
        Vulnerable,    // 虚弱阶段（✅允许打字扣血）
        Dead
    }

    [Header("Boss Config")]
    public string bossName = "WORD TITAN";
    public int bossMaxHP = 10;

    [Header("Word")]
    public WordLibrary wordLibrary;
    public WordData currentWord;

    [Header("World UI")]
    public TMP_Text headWordText;

    [Header("UI (Optional but recommended)")]
    public BossUIController bossUI;

    [Header("Runtime")]
    public bool isActive;
    [SerializeField] private BossPhase phase = BossPhase.Inactive;

    [Header("Typing Damage (Vulnerable Only)")]
    public int typingDamagePerHit = 1;

    /// <summary>
    /// ✅ 只在虚弱阶段才允许“打字命中”扣血
    /// （不再需要外部脚本手动 SetTypingVulnerable）
    /// </summary>
    public bool TypingVulnerable => phase == BossPhase.Vulnerable;

    private EnemyHealth _hp;

    public string CurrentWordText => currentWord != null ? currentWord.wordText : "";

    private void Awake()
    {
        _hp = GetComponent<EnemyHealth>();
    }

    // ---------------- Public API ----------------

    public void ActivateBoss()
    {
        isActive = true;
        phase = BossPhase.Throwing;

        if (_hp != null) _hp.ResetHealth(bossMaxHP);

        RerollWord();
        RefreshHeadText();

        if (bossUI != null) bossUI.BindBoss(this);
        if (bossUI != null) bossUI.RefreshFromBoss();
    }

    /// <summary>
    /// ✅ 终极控制入口：外部只需要告诉 Boss“进入哪个阶段”
    /// Boss 自己决定能不能扣血（TypingVulnerable 自动计算）
    /// </summary>
    public void SetPhase(BossPhase newPhase)
    {
        Debug.Log($"[Boss] SetPhase -> {newPhase} (from {phase}) on {name}");
        phase = newPhase;
        Debug.Log($"[Boss] SetPhase -> {phase}");
        // 可选：阶段切换时你想换词，就在这里统一做
        if (phase == BossPhase.Vulnerable)
        {
            // 虚弱阶段通常给一个新词作为弱点
            RerollWord();
        }

        RefreshHeadText();
        if (bossUI != null) bossUI.RefreshFromBoss();
    }

    /// <summary>
    /// ✅ 兼容旧调用：你项目里可能还在调用 SetTypingVulnerable(true/false)
    /// 现在它会映射到 Phase，不会再出现“忘了开关”的问题
    /// </summary>
    public void SetTypingVulnerable(bool v)
    {
        if (!isActive) return;
        SetPhase(v ? BossPhase.Vulnerable : BossPhase.Throwing);
    }

    public void RerollWord()
    {
        if (wordLibrary == null) return;
        currentWord = wordLibrary.GetRandomWord();

        RefreshHeadText();
        if (bossUI != null) bossUI.RefreshFromBoss();
    }

    public void RefreshHeadText()
    {
        if (headWordText != null) headWordText.text = CurrentWordText;
    }

    // ---------------- Typing API ----------------

    public bool IsMatch(string rawInput)
    {
        string a = TypingTextUtil.NormalizeLettersOnly(rawInput);
        string b = TypingTextUtil.NormalizeLettersOnly(CurrentWordText);
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
        return a == b;
    }

    public void OnCorrectHit()
    {
        Debug.Log($"[Boss] OnCorrectHit phase={phase} active={isActive} TypingVulnerable={TypingVulnerable}");
        if (!isActive) return;
        if (_hp == null || _hp.IsDead) return;

        // ✅ 关键：只有虚弱阶段允许扣血
        if (!TypingVulnerable)
            return;

        _hp.TakeDamage(typingDamagePerHit);

        // UI 更新（你现在 UI 是订阅式，这句可有可无，但保留兼容）
        if (bossUI != null) bossUI.RefreshFromBoss();

        // 还没死就换词
        if (!_hp.IsDead)
        {
            RerollWord();
        }
        else
        {
            phase = BossPhase.Dead;
        }
    }

    // ---------------- UI Helpers ----------------
    public int GetCurrentHP() => _hp != null ? _hp.CurrentHealth : 0;
    public int GetMaxHP() => _hp != null ? _hp.MaxHealth : bossMaxHP;

    // ---------------- Debug Helpers ----------------
    public BossPhase GetPhase() => phase;
}
