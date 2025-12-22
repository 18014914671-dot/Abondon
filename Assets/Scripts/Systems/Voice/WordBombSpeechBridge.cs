using System;
using UnityEngine;

[DisallowMultipleComponent]
public class WordBombSpeechBridge : MonoBehaviour
{
    [Header("Optional (legacy)")]
    [Tooltip("如果你不想用 SpeechBus，也可以继续监听 WhisperServerAdapter")]
    public WhisperServerAdapter whisperServer;

    [Header("Match (stable first)")]
    [Tooltip("优先按 token 匹配；更稳，但可能误触发更多。")]
    public bool matchAnyToken = true;

    [Tooltip("命中一个词时：是否触发场景里所有同词炸弹")]
    public bool triggerAllSameWord = true;

    [Tooltip("如果 token 没命中，是否再用 whole text 再试一次（更稳）")]
    public bool fallbackTryWholeText = true;

    [Tooltip("whole text 再试一次时，是否把空格全部去掉再试（更笨但更稳）")]
    public bool fallbackTryNoSpace = true;

    [Header("Debug")]
    public bool logRawAndNorm = true;
    public bool logMatches = true;
    public bool logNoMatchDetails = true;

    private WordBombManager _manager;
    private bool _subscribedBus;
    private bool _subscribedAdapter;

    private void Awake()
    {
        TryResolveManager();
    }

    private void OnEnable()
    {
        // ✅ 关键：先确保 SpeechBus 一定存在，再订阅（避免加载顺序导致订阅丢失）
        SpeechBus.EnsureExists();

        SubscribeIfNeeded();
    }

    private void OnDisable()
    {
        UnsubscribeIfNeeded();
    }

    private void Update()
    {
        // ✅ 关键：今天不命中很可能是 manager 还没 ready（加载顺序/场景切换）
        if (_manager == null)
            TryResolveManager();
    }

    private void TryResolveManager()
    {
        _manager = WordBombManager.Instance;
        if (_manager == null)
            _manager = FindFirstObjectByType<WordBombManager>();
    }

    private void SubscribeIfNeeded()
    {
        if (!_subscribedBus && SpeechBus.Instance != null)
        {
            SpeechBus.Instance.OnSpeechFinal += OnSpeechFinal;
            _subscribedBus = true;
        }

        if (!_subscribedAdapter && whisperServer != null)
        {
            whisperServer.OnRecognized += OnRecognizedRaw;
            _subscribedAdapter = true;
        }
    }

    private void UnsubscribeIfNeeded()
    {
        if (_subscribedBus && SpeechBus.Instance != null)
        {
            SpeechBus.Instance.OnSpeechFinal -= OnSpeechFinal;
            _subscribedBus = false;
        }

        if (_subscribedAdapter && whisperServer != null)
        {
            whisperServer.OnRecognized -= OnRecognizedRaw;
            _subscribedAdapter = false;
        }
    }

    private void OnSpeechFinal(SpeechResult r)
    {
        if (r == null) return;

        string raw = r.rawText ?? "";
        string norm = r.normalizedText ?? "";

        HandleText(raw, norm);
    }

    // legacy：如果你没挂 SpeechBus，也能跑
    private void OnRecognizedRaw(string raw)
    {
        raw = raw ?? "";
        string norm = TextNormalizer.Normalize(raw);

        HandleText(raw, norm);
    }

    private void HandleText(string raw, string norm)
    {
        // manager 可能晚于 bridge 出现，所以这里再兜一次
        if (_manager == null) TryResolveManager();
        if (_manager == null) return;

        // 都为空就算了
        if (string.IsNullOrWhiteSpace(raw) && string.IsNullOrWhiteSpace(norm)) return;

        // 再 normalize 一次，避免你“改了 voice system 参数”导致 upstream 没走到 normalized
        if (string.IsNullOrWhiteSpace(norm))
            norm = TextNormalizer.Normalize(raw);

        if (logRawAndNorm)
            Debug.Log($"[WordBombSpeechBridge] raw='{raw}' norm='{norm}'");

        bool hit = MatchAndTriggerStable(raw, norm);

        if (!hit && logMatches)
            Debug.Log("[WordBombSpeechBridge] no match.");
    }

    private bool MatchAndTriggerStable(string raw, string norm)
    {
        bool anyHit = false;

        // 1) token 优先（最稳的玩法体验：说一句话里面包含目标词也能触发）
        if (matchAnyToken)
        {
            var tokens = TextNormalizer.SplitTokens(norm);
            for (int i = 0; i < tokens.Length; i++)
            {
                string t = tokens[i];
                if (string.IsNullOrWhiteSpace(t)) continue;

                bool hit = _manager.TriggerByWord(t, triggerAllSameWord);
                if (hit)
                {
                    anyHit = true;
                    if (logMatches) Debug.Log($"[WordBombSpeechBridge] MATCH token='{t}'");
                    if (!triggerAllSameWord) return true; // 命中一个就收
                }
            }

            if (anyHit) return true;
        }

        // 2) whole text 兜底（比如你炸弹词是短语/两词）
        if (fallbackTryWholeText && !string.IsNullOrWhiteSpace(norm))
        {
            bool hit = _manager.TriggerByWord(norm, triggerAllSameWord);
            if (hit)
            {
                if (logMatches) Debug.Log($"[WordBombSpeechBridge] MATCH whole='{norm}'");
                return true;
            }
        }

        // 3) 去空格再兜底（更笨但更稳）
        if (fallbackTryNoSpace && !string.IsNullOrWhiteSpace(norm))
        {
            string noSpace = norm.Replace(" ", "");
            if (!string.Equals(noSpace, norm, StringComparison.Ordinal))
            {
                bool hit = _manager.TriggerByWord(noSpace, triggerAllSameWord);
                if (hit)
                {
                    if (logMatches) Debug.Log($"[WordBombSpeechBridge] MATCH noSpace='{noSpace}'");
                    return true;
                }
            }
        }

        // 4) 最后一层：对 raw 再 normalize 一次再全走一遍（防止 upstream 某次传错）
        string rawNorm = TextNormalizer.Normalize(raw);
        if (!string.IsNullOrWhiteSpace(rawNorm) && rawNorm != norm)
        {
            if (matchAnyToken)
            {
                var tokens2 = TextNormalizer.SplitTokens(rawNorm);
                for (int i = 0; i < tokens2.Length; i++)
                {
                    string t = tokens2[i];
                    if (string.IsNullOrWhiteSpace(t)) continue;

                    bool hit = _manager.TriggerByWord(t, triggerAllSameWord);
                    if (hit)
                    {
                        if (logMatches) Debug.Log($"[WordBombSpeechBridge] MATCH rawToken='{t}'");
                        return true;
                    }
                }
            }

            if (fallbackTryWholeText)
            {
                bool hit = _manager.TriggerByWord(rawNorm, triggerAllSameWord);
                if (hit)
                {
                    if (logMatches) Debug.Log($"[WordBombSpeechBridge] MATCH rawWhole='{rawNorm}'");
                    return true;
                }
            }
        }

        if (logNoMatchDetails)
        {
            Debug.Log($"[WordBombSpeechBridge] NO MATCH details: norm='{norm}', rawNorm='{TextNormalizer.Normalize(raw)}'");
        }

        return false;
    }
}
