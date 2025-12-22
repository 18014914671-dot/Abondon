using System.Collections;
using UnityEngine;

public class StoryProgressManager : MonoBehaviour
{
    [Header("Nodes (order = story order)")]
    public StoryNodeSO[] nodes;

    [Header("Dependencies")]
    public StoryPresenter presenter;

    [Header("Debug")]
    public bool verboseLog = true;

    private int _currentIndex = 0;

    private StoryProgressTracker _boundTracker;
    private Coroutine _bindRoutine;

    private void OnEnable()
    {
        StartBindLoop();
    }

    private void OnDisable()
    {
        StopBindLoop();
        UnbindTracker();
    }

    private void OnDestroy()
    {
        StopBindLoop();
        UnbindTracker();
    }

    private void StartBindLoop()
    {
        StopBindLoop();
        _bindRoutine = StartCoroutine(BindLoop());
    }

    private void StopBindLoop()
    {
        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }
    }

    // ✅ 核心：一直等到 Tracker.Instance 出现，并且如果 Instance 变化会自动重绑
    private IEnumerator BindLoop()
    {
        while (true)
        {
            var t = StoryProgressTracker.Instance;

            if (t != null && t != _boundTracker)
            {
                UnbindTracker();
                BindTracker(t);

                if (verboseLog)
                    Debug.Log("[StoryProgressManager] Bound to StoryProgressTracker.");

                // 不在启动时 TryAdvance（避免读档秒跳）
                // 但绑定成功后，允许做一次“安全检查”，如果你想立即显示当前节点，可以解除下面注释：
                // TryAdvance();
            }

            yield return null;
        }
    }

    private void BindTracker(StoryProgressTracker t)
    {
        if (t == null) return;

        // 防重复订阅
        t.OnKillCountChanged -= HandleProgressChanged;
        t.OnTotalDataCountChanged -= HandleProgressChanged;
        t.OnCompletedTypesCountChanged -= HandleProgressChanged;
        t.OnAnyTypeReachedThreshold -= HandleAnyTypeReachedThreshold;

        t.OnKillCountChanged += HandleProgressChanged;
        t.OnTotalDataCountChanged += HandleProgressChanged;
        t.OnCompletedTypesCountChanged += HandleProgressChanged;
        t.OnAnyTypeReachedThreshold += HandleAnyTypeReachedThreshold;

        _boundTracker = t;
    }

    private void UnbindTracker()
    {
        if (_boundTracker == null) return;

        _boundTracker.OnKillCountChanged -= HandleProgressChanged;
        _boundTracker.OnTotalDataCountChanged -= HandleProgressChanged;
        _boundTracker.OnCompletedTypesCountChanged -= HandleProgressChanged;
        _boundTracker.OnAnyTypeReachedThreshold -= HandleAnyTypeReachedThreshold;

        _boundTracker = null;
    }

    private void HandleProgressChanged(int _)
    {
        if (verboseLog)
            Debug.Log($"[StoryProgressManager] ProgressChanged. idx={_currentIndex}");

        TryAdvance();
    }

    private void HandleAnyTypeReachedThreshold(string id)
    {
        if (verboseLog)
            Debug.Log($"[StoryProgressManager] AnyTypeReachedThreshold id={id} idx={_currentIndex}");

        TryAdvance();
    }

    public void TryAdvance()
    {
        if (nodes == null || nodes.Length == 0) return;
        if (presenter == null) return;

        while (_currentIndex < nodes.Length)
        {
            var node = nodes[_currentIndex];
            if (node == null)
            {
                _currentIndex++;
                continue;
            }

            bool satisfied = IsNodeSatisfied(node);

            if (verboseLog)
                Debug.Log($"[StoryProgressManager] Check idx={_currentIndex} id={node.nodeId} satisfied={satisfied}");

            if (satisfied)
            {
                presenter.Present(node);

                if (node.triggerOnce)
                {
                    _currentIndex++;
                    continue; // 继续检查下一个是否也满足
                }
                else break;
            }
            else break;
        }
    }

    private bool IsNodeSatisfied(StoryNodeSO node)
    {
        if (node.conditions == null || node.conditions.Length == 0)
            return true;

        bool anyTrue = false;
        bool allTrue = true;

        for (int i = 0; i < node.conditions.Length; i++)
        {
            var c = node.conditions[i];
            bool ok = EvaluateCondition(c);
            anyTrue |= ok;
            allTrue &= ok;
        }

        return node.combineMode == StoryCombineMode.AND ? allTrue : anyTrue;
    }

    private bool EvaluateCondition(StoryNodeSO.Condition c)
    {
        var t = StoryProgressTracker.Instance;
        if (t == null) return false;

        switch (c.type)
        {
            case StoryConditionType.KillCountAtLeast:
                return t.KillCount >= c.threshold;

            case StoryConditionType.TotalDataCountAtLeast:
                return t.TotalDataCount >= c.threshold;

            case StoryConditionType.CompletedTypesAtLeast:
                return t.CompletedTypesCount >= c.threshold;

            case StoryConditionType.AnySingleTypeReachedThreshold:
                // 这个节点推荐靠事件触发（OnAnyTypeReachedThreshold）推进
                return false;

            default:
                return false;
        }
    }
}
