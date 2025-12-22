using System;
using UnityEngine;

[DisallowMultipleComponent]
public class SpeechBus : MonoBehaviour
{
    public static SpeechBus Instance { get; private set; }

    public event Action<SpeechResult> OnSpeechFinal;

    /// <summary>自动创建（不需要手动挂）</summary>
    public static SpeechBus EnsureExists()
    {
        if (Instance != null) return Instance;

        var go = new GameObject("SpeechBus");
        var bus = go.AddComponent<SpeechBus>();
        DontDestroyOnLoad(go);
        return bus;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[SpeechBus] Ready");
    }

    public void PublishFinal(string rawText, string wavPath = "")
    {
        string norm = TextNormalizer.Normalize(rawText);
        var result = new SpeechResult(rawText, norm, Time.time, wavPath);

        Debug.Log($"[SpeechBus] FINAL raw='{rawText}' norm='{norm}'");
        OnSpeechFinal?.Invoke(result);
    }
}
