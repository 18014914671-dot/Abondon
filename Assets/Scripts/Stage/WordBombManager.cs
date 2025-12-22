using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 放置：Assets/Scripts/Stage/WordBombManager.cs
/// 目标：稳定匹配（3_star / 3-star / 3 star -> star）
/// </summary>
public class WordBombManager : MonoBehaviour
{
    public static WordBombManager Instance { get; private set; }

    // key = normalized word token, value = all bombs that share the key
    private readonly Dictionary<string, List<BombCarrierEnemy>> _map = new Dictionary<string, List<BombCarrierEnemy>>();

    [Header("Debug")]
    public bool logRegister = true;
    public bool logMissWithActiveKeys = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // -------- Registration API (给 BombCarrierRegister 用) --------
    public void Register(BombCarrierEnemy bomb)
    {
        if (bomb == null) return;

        string key = GetBombKey(bomb);
        if (string.IsNullOrWhiteSpace(key)) return;

        if (!_map.TryGetValue(key, out var list))
        {
            list = new List<BombCarrierEnemy>(4);
            _map.Add(key, list);
        }

        if (!list.Contains(bomb))
            list.Add(bomb);

        if (logRegister)
            Debug.Log($"[WordBombManager] REGISTER key='{key}' bomb='{bomb.name}'");
    }

    public void Unregister(BombCarrierEnemy bomb)
    {
        if (bomb == null) return;

        string key = GetBombKey(bomb);
        if (string.IsNullOrWhiteSpace(key)) return;

        if (_map.TryGetValue(key, out var list))
        {
            list.Remove(bomb);
            if (list.Count == 0) _map.Remove(key);
        }

        if (logRegister)
            Debug.Log($"[WordBombManager] UNREGISTER key='{key}' bomb='{bomb.name}'");
    }

    /// <summary>
    /// 由 WordBombSpeechBridge 调用：传入 token（例如 "star"）。
    /// triggerAllSameWord = true 时，所有同词炸弹都爆；否则爆一个就停。
    /// </summary>
    public bool TriggerByWord(string token, bool triggerAllSameWord)
    {
        string key = NormalizeWordKey(token);
        if (string.IsNullOrWhiteSpace(key)) return false;

        if (!_map.TryGetValue(key, out var list) || list == null || list.Count == 0)
        {
            if (logMissWithActiveKeys)
                Debug.Log($"[WordBombManager] MISS token='{key}'. Active keys: {GetActiveKeysPreview()}");
            return false;
        }

        bool triggeredAny = false;

        // 复制一份，避免爆炸销毁对象时改动 list
        var snapshot = list.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
        {
            var bomb = snapshot[i];
            if (bomb == null) continue;

            TryTriggerBomb(bomb);
            triggeredAny = true;

            if (!triggerAllSameWord) break;
        }

        return triggeredAny;
    }

    // -------- Internal --------

    private string GetBombKey(BombCarrierEnemy bomb)
    {
        // 关键：不要依赖 WordData.word 这种字段（你报错就是因为它不存在）
        // 这里用 bomb.bombWord 的“对象名/asset名”，再做强力归一化。
        //
        // 注意：BombCarrierEnemy 脚本你已有，里面肯定有个 bombWord 引用（你 Inspector 图里就有）
        // 我们用反射拿，避免你 BombCarrierEnemy 字段名不一致导致编译炸。
        try
        {
            var t = bomb.GetType();
            var f = t.GetField("bombWord");
            if (f != null)
            {
                var wd = f.GetValue(bomb);
                if (wd != null)
                {
                    string raw = wd.ToString(); // 可能是 "3_star (Word Data)" 这种
                    // 更稳：优先 UnityEngine.Object.name
                    if (wd is UnityEngine.Object uo) raw = uo.name;
                    return NormalizeWordKey(raw);
                }
            }
        }
        catch { }

        // 兜底：直接用 bomb 自己的名字（不推荐，但至少不空）
        return NormalizeWordKey(bomb.name);
    }

    private void TryTriggerBomb(BombCarrierEnemy bomb)
    {
        // 你之前报错：BombCarrierEnemy 没有 TriggerExplosion
        // 所以这里用 SendMessage 兼容你现有的爆炸函数名（任意一个能接上就行）
        bomb.SendMessage("TriggerExplosion", SendMessageOptions.DontRequireReceiver);
        bomb.SendMessage("Explode", SendMessageOptions.DontRequireReceiver);
        bomb.SendMessage("ExplodeNow", SendMessageOptions.DontRequireReceiver);
        bomb.SendMessage("Trigger", SendMessageOptions.DontRequireReceiver);
    }

    private string NormalizeWordKey(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";

        // 1) 用你的 TextNormalizer 做基础归一化（已支持 _/- -> space）
        string norm = TextNormalizer.Normalize(raw);

        // 2) 如果是 "3 star" 或 "03 star" 这种，去掉前导数字
        //    只保留最后一个词（更贴合你“编号_单词”的资源命名）
        //    例如: "3 star" -> "star"
        norm = Regex.Replace(norm, @"^\d+\s*", ""); // 去掉开头数字
        var tokens = TextNormalizer.SplitTokens(norm);
        if (tokens.Length == 0) return "";

        // 3) 只取最后一个 token： "unit test star" -> "star"
        return tokens[tokens.Length - 1];
    }

    private string GetActiveKeysPreview()
    {
        int count = 0;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var kv in _map)
        {
            if (count++ > 0) sb.Append(", ");
            sb.Append(kv.Key);
            if (count >= 12) { sb.Append(" ..."); break; }
        }
        return sb.ToString();
    }
}
