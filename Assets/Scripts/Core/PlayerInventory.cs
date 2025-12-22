using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Serializable]
    private class SaveData
    {
        public List<string> ids = new();
        public List<int> counts = new();
    }

    private readonly Dictionary<string, int> _counts = new();
    public IReadOnlyDictionary<string, int> GetAllCounts() => _counts;

    public event Action<string, int, bool> OnDataChanged;   // (id, newCount, firstTime)
    public event Action OnLoaded;

    private string SavePath => Path.Combine(Application.persistentDataPath, "abondon_inventory.json");

    // 延迟保存
    private bool _dirty;
    private float _saveTimer;
    [SerializeField] private float saveDelaySeconds = 0.5f;

    public bool IsReady { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
        IsReady = true;
        OnLoaded?.Invoke();
    }

    private void Update()
    {
        if (!_dirty) return;

        _saveTimer += Time.unscaledDeltaTime;
        if (_saveTimer >= saveDelaySeconds)
        {
            _saveTimer = 0f;
            _dirty = false;
            SaveNow();
        }
    }

    public int GetCount(string id)
        => (!string.IsNullOrEmpty(id) && _counts.TryGetValue(id, out var c)) ? c : 0;

    public int GetTotalCount()
    {
        int total = 0;
        foreach (var kv in _counts) total += kv.Value;
        return total;
    }

    public List<KeyValuePair<string, int>> GetAllCountsSorted()
    {
        var list = new List<KeyValuePair<string, int>>(_counts);
        list.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.Ordinal));
        return list;
    }

    public void Add(string id, int amount = 1)
    {
        if (string.IsNullOrEmpty(id)) return;
        amount = Mathf.Max(1, amount);

        int before = GetCount(id);
        int after = before + amount;
        _counts[id] = after;

        bool firstTime = (before == 0);

        _dirty = true;
        _saveTimer = 0f;

        OnDataChanged?.Invoke(id, after, firstTime);
    }

    public void SaveNow()
    {
        try
        {
            var data = new SaveData();
            foreach (var kv in _counts)
            {
                data.ids.Add(kv.Key);
                data.counts.Add(kv.Value);
            }
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerInventory] Save failed: {e.Message}");
        }
    }

    public void Load()
    {
        _counts.Clear();

        try
        {
            if (!File.Exists(SavePath)) return;

            var json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) return;

            int n = Mathf.Min(data.ids.Count, data.counts.Count);
            for (int i = 0; i < n; i++)
            {
                string id = data.ids[i];
                if (string.IsNullOrEmpty(id)) continue;
                _counts[id] = Mathf.Max(0, data.counts[i]);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerInventory] Load failed: {e.Message}");
        }
    }

    // 开发期很实用：清档
    public void ResetAll(bool deleteSaveFile = true)
    {
        _counts.Clear();
        _dirty = false;
        _saveTimer = 0f;

        if (deleteSaveFile)
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerInventory] Delete save failed: {e.Message}");
            }
        }

        OnDataChanged?.Invoke("", 0, false);
    }
}
