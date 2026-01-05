using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class BossBattleDirector : MonoBehaviour
{
    public enum Phase { Normal, Charging, Vulnerable }

    [Header("Refs (Boss)")]
    public BossController boss;
    public BossMovementController move;
    public EnemyHealth bossHp;

    [Header("Player Refs (Scene)")]
    public Transform player;
    public PlayerHealth playerHealth;
    public TypingInputPanel typingInput;
    public TimingRingPanel timingRing;

    [Header("Auto Resolve (Recommended)")]
    [Tooltip("Prefab 资产无法拖 Scene 引用；勾选后会在运行时自动查找 Player/UI 引用")]
    public bool autoResolveSceneRefs = true;

    [Tooltip("玩家物体的 Tag（默认 Player）")]
    public string playerTag = "Player";

    [Header("Bomb (Assets)")]
    [Tooltip("挂了 WordBombProjectile 的 prefab")]
    public GameObject wordBombPrefab;

    [Tooltip("炸弹用的单词库（ScriptableObject）")]
    public WordLibrary bombWordLibrary;

    [Tooltip("每隔多少秒触发一次“丢炸弹动作”（会丢一波 burst）")]
    public float bombInterval = 2.2f;

    [Header("Bomb Burst (Stage 1 Key Change)")]
    [Tooltip("每次 interval 一共丢几颗炸弹（1 = 原来行为）")]
    [Min(1)] public int bombsPerInterval = 1;

    [Tooltip("一波里两颗炸弹之间的间隔（秒）。建议 0.05~0.25")]
    public float burstSpacing = 0.12f;

    [Tooltip("一波里炸弹生成位置的水平散布（随机偏移范围）")]
    public float spawnSpreadX = 0.6f;

    [Header("Bomb Timing Tuning")]
    [Tooltip("炸弹圆环持续时间（秒）")]
    public float bombRingDuration = 2.5f;

    [Header("Trigger: Perfect Defuse -> Charging")]
    [Tooltip("完美拆弹累计多少次后进入蓄力")]
    public int perfectDefuseToCharge = 6;

    [Header("Charging: Repeat One Word")]
    [Tooltip("蓄力阶段：同一个单词需要成功输入多少次")]
    public int chargeWordRepeatCount = 5;

    [Tooltip("每次重复的圆环时长（秒），越短越难")]
    public float repeatWordDuration = 0.75f;

    [Tooltip("整个蓄力阶段总时间上限（秒）。超过则失败（建议 3~6 秒）")]
    public float chargeTotalTimeLimit = 5.0f;

    [Tooltip("两次重复之间间隔（秒）")]
    public float repeatWordGap = 0.05f;

    [Tooltip("蓄力失败时玩家扣的大血量")]
    public int failBigDamage = 3;

    [Header("Vulnerable")]
    [Tooltip("虚弱窗口持续（秒）")]
    public float vulnerableSeconds = 4.0f;

    [Header("VFX")]
    [Tooltip("可选：进入蓄力时震屏")]
    public CameraShake2D shakeOnCharge;

    [Header("Runtime")]
    public Phase phase = Phase.Normal;

    [HideInInspector] public bool bossVulnerable = false;

    [Header("Debug")]
    public bool verboseLog = false;

    private float _bombTimer;
    private int _perfectCount;

    private Coroutine _loopCo;
    private Coroutine _bombBurstCo;
    private Coroutine _chargingCo;
    private Coroutine _vulnerableCo;

    private void Awake()
    {
        if (!boss) boss = GetComponent<BossController>();
        if (!bossHp) bossHp = GetComponent<EnemyHealth>();
        if (!move) move = GetComponent<BossMovementController>();
    }

    private void Start()
    {
        if (autoResolveSceneRefs)
        {
            ResolveSceneRefs();
            StartCoroutine(CoLateResolve());
        }

        // ✅ 开局强制同步一次（非常关键）
        SyncBossPhaseToDirector();

        _loopCo = StartCoroutine(BattleLoop());
    }

    private IEnumerator CoLateResolve()
    {
        yield return null;
        ResolveSceneRefs();

        // ✅ UI/Player补齐后，再同步一次（避免首帧引用未就绪）
        SyncBossPhaseToDirector();
    }

    private bool ResolveSceneRefs()
    {
        bool ok = true;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform;
        }
        if (player == null) ok = false;

        if (playerHealth == null && player != null)
            playerHealth = player.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth == null) ok = false;

        if (typingInput == null)
            typingInput = FindFirstObjectByType<TypingInputPanel>();
        if (typingInput == null) ok = false;

        if (timingRing == null)
            timingRing = FindFirstObjectByType<TimingRingPanel>();
        if (timingRing == null) ok = false;

        // ✅ 自动把 BossUI 绑定上（你之前 Boss UI = None 会导致你以为 UI 没动）
        if (boss != null && boss.bossUI == null)
        {
            var ui = FindFirstObjectByType<BossUIController>();
            if (ui != null)
            {
                boss.bossUI = ui;
                boss.bossUI.BindBoss(boss);
                boss.bossUI.RefreshFromBoss();
                if (verboseLog) Debug.Log("[Director] Auto-bound BossUIController.");
            }
        }

        return ok;
    }

    // =========================
    // 外部通知：炸弹结束（成功/失败）
    // WordBombProjectile 会调用它
    // =========================
    public void NotifyBombResolved(bool success, bool perfect)
    {
        if (phase != Phase.Normal) return;

        if (success && perfect)
            _perfectCount++;

        int need = Mathf.Max(1, perfectDefuseToCharge);
        if (_perfectCount >= need)
        {
            _perfectCount = 0;
            EnterCharging();
        }
    }

    private IEnumerator BattleLoop()
    {
        while (boss != null && bossHp != null && !bossHp.IsDead)
        {
            if (autoResolveSceneRefs)
            {
                if (player == null || playerHealth == null || typingInput == null || timingRing == null)
                    ResolveSceneRefs();
            }

            switch (phase)
            {
                case Phase.Normal:
                    yield return NormalTick();
                    break;

                case Phase.Charging:
                case Phase.Vulnerable:
                    // 由 EnterCharging/EnterVulnerable 内部协程控制
                    yield return null;
                    break;
            }

            yield return null;
        }
    }

    private IEnumerator NormalTick()
    {
        // Normal：移动开启
        if (move) move.Freeze(false);

        // Normal：Boss不可被真正伤害（只能在 Vulnerable）
        SetBossVulnerable(false);

        // ✅ 关键：Director -> BossController 同步
        SyncBossPhaseToDirector();

        // 必要引用不齐：不丢（避免无声报错）
        if (wordBombPrefab == null || bombWordLibrary == null) yield break;
        if (typingInput == null || timingRing == null) yield break;
        if (player == null || playerHealth == null) yield break;

        // 定时触发“一波 burst”
        _bombTimer += Time.deltaTime;
        if (_bombTimer >= bombInterval)
        {
            _bombTimer = 0f;

            if (_bombBurstCo == null)
                _bombBurstCo = StartCoroutine(CoBombBurst());
        }

        yield break;
    }

    private IEnumerator CoBombBurst()
    {
        int count = Mathf.Max(1, bombsPerInterval);

        for (int i = 0; i < count; i++)
        {
            if (phase != Phase.Normal) break;

            while (timingRing != null && timingRing.Running)
            {
                if (phase != Phase.Normal) break;
                yield return null;
            }
            if (phase != Phase.Normal) break;

            SpawnOneBomb();

            if (burstSpacing > 0f)
                yield return new WaitForSeconds(burstSpacing);
        }

        _bombBurstCo = null;
    }

    private void SpawnOneBomb()
    {
        if (wordBombPrefab == null || bombWordLibrary == null) return;
        if (typingInput == null || timingRing == null) return;
        if (player == null || playerHealth == null) return;

        WordData w = bombWordLibrary.GetRandomWord();
        if (w == null) return;

        float dx = (spawnSpreadX <= 0f) ? 0f : Random.Range(-spawnSpreadX, spawnSpreadX);
        Vector3 spawnPos = transform.position + new Vector3(dx, -0.3f, 0f);

        var go = Instantiate(wordBombPrefab, spawnPos, Quaternion.identity);
        go.tag = "Enemy";

        var bomb = go.GetComponent<WordBombProjectile>();
        if (bomb != null)
        {
            bomb.director = this;

            bomb.ringDuration = bombRingDuration;
            bomb.windowStart01 = timingRing.windowStart;
            bomb.windowEnd01 = timingRing.windowEnd;

            bomb.Init(player, playerHealth, typingInput, timingRing, w);
        }
    }

    private void EnterCharging()
    {
        if (phase == Phase.Charging) return;
        phase = Phase.Charging;

        // ✅ 关键同步
        SyncBossPhaseToDirector();

        if (_bombBurstCo != null) { StopCoroutine(_bombBurstCo); _bombBurstCo = null; }

        if (move) move.Freeze(true);
        SetBossVulnerable(false);

        if (shakeOnCharge != null) shakeOnCharge.Shake();

        if (_chargingCo != null) StopCoroutine(_chargingCo);
        _chargingCo = StartCoroutine(CoChargingSequence());
    }

    private IEnumerator CoChargingSequence()
    {
        if (typingInput == null || timingRing == null || bombWordLibrary == null)
        {
            phase = Phase.Normal;
            SyncBossPhaseToDirector();
            yield break;
        }

        WordData word = bombWordLibrary.GetRandomWord();
        if (word == null)
        {
            phase = Phase.Normal;
            SyncBossPhaseToDirector();
            yield break;
        }

        int need = Mathf.Max(1, chargeWordRepeatCount);
        float totalLimit = Mathf.Max(0.5f, chargeTotalTimeLimit);
        float totalT = 0f;

        for (int i = 0; i < need; i++)
        {
            if (totalT >= totalLimit)
            {
                FailCharging();
                yield break;
            }

            var rep = new RepeatWordChallenge(
                typingInput,
                timingRing,
                word,
                repeatWordDuration,
                timingRing.windowStart,
                timingRing.windowEnd
            );

            typingInput.SetActiveChallenge(rep);
            rep.Begin();

            while (!rep.Done)
            {
                totalT += Time.unscaledDeltaTime;
                if (totalT >= totalLimit)
                {
                    typingInput.ClearActiveChallenge(rep);
                    if (timingRing != null) timingRing.Hide();
                    FailCharging();
                    yield break;
                }
                yield return null;
            }

            typingInput.ClearActiveChallenge(rep);

            if (!rep.Success)
            {
                FailCharging();
                yield break;
            }

            if (repeatWordGap > 0f)
                yield return new WaitForSeconds(repeatWordGap);
        }

        EnterVulnerable();
    }

    private void FailCharging()
    {
        if (playerHealth != null) playerHealth.TakeDamage(failBigDamage);
        if (timingRing != null) timingRing.Hide();

        phase = Phase.Normal;

        if (_chargingCo != null) { StopCoroutine(_chargingCo); _chargingCo = null; }
        if (_vulnerableCo != null) { StopCoroutine(_vulnerableCo); _vulnerableCo = null; }

        if (move) move.Freeze(false);
        SetBossVulnerable(false);

        // ✅ 关键同步
        SyncBossPhaseToDirector();
    }

    private void EnterVulnerable()
    {
        if (phase == Phase.Vulnerable) return;
        phase = Phase.Vulnerable;

        // Vulnerable：Boss 停住 + 可以被伤害
        if (move) move.Freeze(true);
        SetBossVulnerable(true);

        // ✅ 关键同步（这一步决定 BossController.TypingVulnerable 会变 True）
        SyncBossPhaseToDirector();

        if (_vulnerableCo != null) StopCoroutine(_vulnerableCo);
        _vulnerableCo = StartCoroutine(CoVulnerableWindow());
    }

    private IEnumerator CoVulnerableWindow()
    {
        float t = Mathf.Max(0.1f, vulnerableSeconds);

        while (t > 0f && boss != null && bossHp != null && !bossHp.IsDead)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        SetBossVulnerable(false);
        if (move) move.Freeze(false);

        phase = Phase.Normal;

        // ✅ 关键同步
        SyncBossPhaseToDirector();

        _vulnerableCo = null;
    }

    private void SetBossVulnerable(bool canTakeDamage)
    {
        bossVulnerable = canTakeDamage;
    }

    /// <summary>
    /// ✅ 终极关键：把 Director 的 phase 同步到 BossController.phase
    /// 你之前的 bug（Boss 永远 Throwing）就是因为缺这一层。
    /// </summary>
    private void SyncBossPhaseToDirector()
    {
        if (boss == null) return;

        // 确保 Boss 参与战斗（避免 OnCorrectHit 直接 return）
        boss.isActive = true;

        // Director.Phase -> BossController.BossPhase
        switch (phase)
        {
            case Phase.Normal:
                boss.SetPhase(BossController.BossPhase.Throwing);
                break;
            case Phase.Charging:
                boss.SetPhase(BossController.BossPhase.Charging);
                break;
            case Phase.Vulnerable:
                boss.SetPhase(BossController.BossPhase.Vulnerable);
                break;
        }

        if (verboseLog)
            Debug.Log($"[Director] SyncBossPhase: director={phase} -> boss={boss.GetPhase()}");
    }

    // =========================
    // 内部挑战：重复同一个单词
    // =========================
    private class RepeatWordChallenge : IActiveTypingChallenge
    {
        private readonly TypingInputPanel _typing;
        private readonly TimingRingPanel _ring;
        private readonly WordData _word;
        private readonly float _duration;
        private readonly float _ws;
        private readonly float _we;

        private float _t;
        private bool _running;

        public bool Done { get; private set; }
        public bool Success { get; private set; }

        public RepeatWordChallenge(
            TypingInputPanel typing,
            TimingRingPanel ring,
            WordData word,
            float duration,
            float windowStart,
            float windowEnd)
        {
            _typing = typing;
            _ring = ring;
            _word = word;
            _duration = Mathf.Max(0.15f, duration);
            _ws = windowStart;
            _we = windowEnd;
        }

        public void Begin()
        {
            Done = false;
            Success = false;

            if (_word == null)
            {
                Done = true;
                return;
            }

            _running = true;
            _t = 0f;

            if (_ring != null)
                _ring.Show(_word.wordText, _duration, _ws, _we);

            _typing.StartCoroutine(CoTimeout());
        }

        private IEnumerator CoTimeout()
        {
            while (_running)
            {
                _t += Time.unscaledDeltaTime;
                if (_t >= _duration)
                {
                    _running = false;
                    Done = true;
                    Success = false;
                    if (_ring != null) _ring.Hide();
                    yield break;
                }
                yield return null;
            }
        }

        public bool TryConsumeInput(string normalizedLettersOnly)
        {
            if (Done) return true;
            if (_word == null) return false;

            string want = TypingTextUtil.NormalizeLettersOnly(_word.wordText);
            if (normalizedLettersOnly != want) return false;

            if (_ring != null && !_ring.IsInWindow())
            {
                _running = false;
                Done = true;
                Success = false;
                _ring.Hide();
                return true;
            }

            _running = false;
            Done = true;
            Success = true;
            if (_ring != null) _ring.Hide();
            return true;
        }
    }
}
