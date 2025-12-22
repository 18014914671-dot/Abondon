using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DataHUDList : MonoBehaviour
{
    [Header("Data")]
    public DataDatabase database;

    [Header("UI")]
    public RectTransform contentRoot;
    public DataHUDRow rowPrefab;

    [Header("Options")]
    public bool showOnlyOwned = true;
    public int maxRows = 0; // 0 = unlimited

    // id (string) -> DataItem
    private Dictionary<string, DataItem> _lookup;
    private readonly List<DataHUDRow> _rows = new();

    private Coroutine _bindRoutine;
    private PlayerInventory _boundInv;

    private void OnEnable()
    {
        // ✅ 面板打开时：先解绑旧的，再等待库存就绪后绑定并刷新
        Unbind();

        if (_bindRoutine != null) StopCoroutine(_bindRoutine);
        _bindRoutine = StartCoroutine(BindAndRefresh());
    }

    private void OnDisable()
    {
        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }
        Unbind();
    }

    private IEnumerator BindAndRefresh()
    {
        // 等一帧，确保 UI / Canvas 初始化完成
        yield return null;

        // ✅ 等 PlayerInventory.Instance 出现（最多等 5 秒，防止死等）
        float timeout = 5f;
        float t = 0f;
        while (PlayerInventory.Instance == null && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        var inv = PlayerInventory.Instance;
        if (inv == null)
        {
            Debug.LogWarning("[DataHUDList] PlayerInventory.Instance not ready. List will not auto-refresh.");
            // 仍然尝试刷新一次（显示空）
            RefreshAll();
            yield break;
        }

        _boundInv = inv;
        _boundInv.OnDataChanged += HandleChanged;

        // ✅ 绑定成功后立即刷新
        RefreshAll();
    }

    private void Unbind()
    {
        if (_boundInv != null)
        {
            _boundInv.OnDataChanged -= HandleChanged;
            _boundInv = null;
        }
    }

    private void HandleChanged(string id, int count, bool firstTime)
    {
        RefreshAll();
    }

    // ===============================
    // 核心刷新逻辑
    // ===============================
    public void RefreshAll()
    {
        if (database == null || rowPrefab == null || contentRoot == null)
            return;

        BuildLookup();
        ClearRows();

        if (PlayerInventory.Instance == null)
            return;

        var list = PlayerInventory.Instance.GetAllCountsSorted();

        int shown = 0;

        foreach (var kv in list)
        {
            string id = kv.Key;
            int count = kv.Value;

            if (showOnlyOwned && count <= 0)
                continue;

            if (_lookup.TryGetValue(id, out var item))
            {
                var row = CreateRow();
                row.Set(item, count);
                shown++;
            }
            else
            {
                var row = CreateRow();
                row.SetFallback(id, count);
                shown++;
            }

            if (maxRows > 0 && shown >= maxRows)
                break;
        }

        ForceRebuildLayout();
    }

    // ===============================
    // 内部工具
    // ===============================
    private void BuildLookup()
    {
        _lookup = new Dictionary<string, DataItem>();

        foreach (var item in database.allItems)
        {
            if (item == null) continue;

            // ⚠️ 注意：这里的 key 必须和你 inventory 里 Add() 用的 id 一致
            // 你当前是 inv.Add(dataItem.id, 1)（DataPickup）:contentReference[oaicite:1]{index=1}
            // 所以这里应该用 item.id（而不是 item.name）
            string key = item.id;

            if (string.IsNullOrEmpty(key)) continue;

            if (!_lookup.ContainsKey(key))
                _lookup.Add(key, item);
        }
    }

    private DataHUDRow CreateRow()
    {
        var row = Instantiate(rowPrefab, contentRoot);
        row.gameObject.SetActive(true);
        _rows.Add(row);
        return row;
    }

    private void ClearRows()
    {
        foreach (var row in _rows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }
        _rows.Clear();
    }

    // ===============================
    // UI 布局强制刷新
    // ===============================
    private void ForceRebuildLayout()
    {
        Canvas.ForceUpdateCanvases();

        if (contentRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

        Canvas.ForceUpdateCanvases();
    }
}
