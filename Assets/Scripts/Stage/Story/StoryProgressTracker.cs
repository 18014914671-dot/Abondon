using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoryProgressTracker : MonoBehaviour
{
    public static StoryProgressTracker Instance { get; private set; }

    [Header("Completion Rule")]
    [Tooltip("A data type is considered 'completed' when its count >= this value.")]
    public int completeThresholdPerType = 10;

    [Header("Debug")]
    public bool verboseLog = false;

    public int KillCount { get; private set; }
    public int TotalDataCount { get; private set; }
    public int CompletedTypesCount { get; private set; }

    /// <summary>
    /// 任意一个 dataId 第一次达到 completeThresholdPerType 时触发
    /// 参数：达成的 dataId
    /// </summary>
    public event Action<string> OnAnyTypeReachedThreshold;

    public event Action<int> OnKillCountChanged;
    public event Action<int> OnTotalDataCountChanged;
    public event Action<int> OnCompletedTypesCountChanged;

    private readonly HashSet<string> _completedIds = new();

    private Coroutine _bindRoutine;
    private PlayerInventory _boundInv;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // ✅ 关键：每次启用都尝试绑定（场景切换/对象重建不会丢）
        StartBindRoutine();
    }

    private void OnDisable()
    {
        StopBindRoutine();
        UnbindInventory();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        StopBindRoutine();
        UnbindInventory();
    }

    private void StartBindRoutine()
    {
        StopBindRoutine();
        _bindRoutine = StartCoroutine(BindInventoryLoop());
    }

    private void StopBindRoutine()
    {
        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }
    }

    /// <summary>
    /// 一直等到 PlayerInventory.Instance 可用为止（不会只等5秒就放弃）
    /// 同时：如果 Instance 发生变化（比如切场景产生新实例），会自动重绑。
    /// </summary>
    private IEnumerator BindInventoryLoop()
    {
        while (true)
        {
            var inv = PlayerInventory.Instance;

            if (inv != null)
            {
                if (_boundInv != inv)
                {
                    // Instance 变了（或首次绑定），重绑一次
                    UnbindInventory();
                    BindInventory(inv);
                }
            }
            else
            {
                // 没有 inventory，确保已解绑
                UnbindInventory();
            }

            yield return null; // 每帧检查一次（开销很小）
        }
    }

    private void BindInventory(PlayerInventory inv)
    {
        if (inv == null) return;

        // ✅ 防重复订阅：先退订一次
        inv.OnDataChanged -= HandleDataChanged;
        inv.OnLoaded -= HandleInventoryLoaded;

        inv.OnDataChanged += HandleDataChanged;
        inv.OnLoaded += HandleInventoryLoaded;

        _boundInv = inv;

        // 初次同步一次
        RecalculateFromInventory();

        if (verboseLog)
            Debug.Log($"[StoryProgressTracker] Bound to PlayerInventory. Total={TotalDataCount}, Completed={CompletedTypesCount}, Kills={KillCount}");
    }

    private void UnbindInventory()
    {
        if (_boundInv == null) return;

        _boundInv.OnDataChanged -= HandleDataChanged;
        _boundInv.OnLoaded -= HandleInventoryLoaded;
        _boundInv = null;
    }

    private void HandleInventoryLoaded()
    {
        RecalculateFromInventory();
    }

    private void HandleDataChanged(string id, int newCount, bool firstTime)
    {
        // 总量：直接从库存总数算
        int beforeTotal = TotalDataCount;
        TotalDataCount = (_boundInv != null) ? _boundInv.GetTotalCount() : TotalDataCount;
        if (TotalDataCount != beforeTotal)
            OnTotalDataCountChanged?.Invoke(TotalDataCount);

        if (verboseLog)
            Debug.Log($"[Story] DataChanged id={id} newCount={newCount} Total={TotalDataCount} Completed={CompletedTypesCount}");

        // ResetAll 会发 id=""，此时全量重算更安全
        if (string.IsNullOrEmpty(id))
        {
            RecalculateFromInventory();
            return;
        }

        bool wasCompleted = _completedIds.Contains(id);
        bool nowCompleted = newCount >= completeThresholdPerType;

        if (!wasCompleted && nowCompleted)
        {
            _completedIds.Add(id);
            CompletedTypesCount = _completedIds.Count;
            OnCompletedTypesCountChanged?.Invoke(CompletedTypesCount);

            OnAnyTypeReachedThreshold?.Invoke(id);

            if (verboseLog)
                Debug.Log($"[Story] TypeReachedThreshold id={id} CompletedTypes={CompletedTypesCount}");
        }
        else if (wasCompleted && !nowCompleted)
        {
            _completedIds.Remove(id);
            CompletedTypesCount = _completedIds.Count;
            OnCompletedTypesCountChanged?.Invoke(CompletedTypesCount);

            if (verboseLog)
                Debug.Log($"[Story] TypeUncompleted id={id} CompletedTypes={CompletedTypesCount}");
        }
    }

    private void RecalculateFromInventory()
    {
        if (_boundInv == null) return;

        // total
        int beforeTotal = TotalDataCount;
        TotalDataCount = _boundInv.GetTotalCount();
        if (TotalDataCount != beforeTotal)
            OnTotalDataCountChanged?.Invoke(TotalDataCount);

        // completed ids
        _completedIds.Clear();
        var dict = _boundInv.GetAllCounts();
        foreach (var kv in dict)
        {
            if (string.IsNullOrEmpty(kv.Key)) continue;
            if (kv.Value >= completeThresholdPerType)
                _completedIds.Add(kv.Key);
        }

        int beforeC = CompletedTypesCount;
        CompletedTypesCount = _completedIds.Count;
        if (CompletedTypesCount != beforeC)
            OnCompletedTypesCountChanged?.Invoke(CompletedTypesCount);

        if (verboseLog)
            Debug.Log($"[Story] Recalc Total={TotalDataCount} Completed={CompletedTypesCount}");
    }

    /// <summary>
    /// 给 EnemyHealth.Die() 调用：增加击杀数
    /// </summary>
    public void AddKill(int amount = 1)
    {
        amount = Mathf.Max(1, amount);
        KillCount += amount;

        if (verboseLog)
            Debug.Log($"[Story] KillCount={KillCount}");

        OnKillCountChanged?.Invoke(KillCount);
    }

    public void ResetKills()
    {
        KillCount = 0;
        OnKillCountChanged?.Invoke(KillCount);

        if (verboseLog)
            Debug.Log("[Story] KillCount reset");
    }
}
