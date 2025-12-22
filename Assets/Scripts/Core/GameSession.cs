using System.Collections.Generic;
using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    [Header("Database (assign in inspector)")]
    public DataDatabase dataDatabase;

    // 玩家拥有数据：dataId -> count
    private readonly Dictionary<string, int> owned = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public int GetOwnedCount(string dataId)
        => owned.TryGetValue(dataId, out var c) ? c : 0;

    public void AddData(string dataId, int amount = 1)
    {
        if (string.IsNullOrEmpty(dataId)) return;
        if (!owned.ContainsKey(dataId)) owned[dataId] = 0;
        owned[dataId] += Mathf.Max(1, amount);
    }

    public int GetTotalOwnedCount()
    {
        int total = 0;
        foreach (var kv in owned) total += kv.Value;
        return total;
    }
}
