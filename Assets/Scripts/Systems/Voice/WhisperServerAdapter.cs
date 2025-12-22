using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

using UDebug = UnityEngine.Debug;
using UApp = UnityEngine.Application;

[DisallowMultipleComponent]
public class WhisperServerAdapter : MonoBehaviour
{
    [Header("Refs")]
    public VadDetector vad;
    public WhisperServerProcess server;

    [Header("Request")]
    [Tooltip("常见：/inference 或 /transcribe。")]
    public string endpointPath = "/transcribe";

    [Tooltip("en / zh / auto（取决于你的 server）")]
    public string language = "en";

    [Header("Temp WAV")]
    public string subFolder = "WhisperTmp";
    public bool saveWavToDisk = false;
    public bool cleanupTempFiles = true;

    [Header("Debug")]
    public bool logRawText = true;
    public bool logHttpBodyOnSuccess = false;

    public event Action<string> OnRecognized;

    private string _tmpDir;
    private Coroutine _inflight;

    private void Awake()
    {
        _tmpDir = Path.Combine(UApp.persistentDataPath, subFolder);
        Directory.CreateDirectory(_tmpDir);
    }

    private void OnEnable()
    {
        if (vad != null) vad.OnSpeechSegmentReady += HandleSegmentReady;
    }

    private void OnDisable()
    {
        if (vad != null) vad.OnSpeechSegmentReady -= HandleSegmentReady;
    }

    private void Start()
    {
        if (server != null) server.EnsureRunning();
        SpeechBus.EnsureExists();
    }

    private void HandleSegmentReady(float[] pcm, int sampleRate)
    {
        if (pcm == null || pcm.Length == 0) return;

        UDebug.Log($"[WhisperServerAdapter] SEGMENT IN samples={pcm.Length}");

        if (server == null)
        {
            UDebug.LogWarning("[WhisperServerAdapter] server ref is NULL.");
            return;
        }

        if (!server.IsRunning)
        {
            server.EnsureRunning();
            if (!server.IsRunning)
            {
                UDebug.LogWarning("[WhisperServerAdapter] server not running, ignore segment.");
                return;
            }
        }

        // 可选：如果你不希望并发（上一个还没回，这个就丢掉/替换），用这一句：
        if (_inflight != null) StopCoroutine(_inflight);
        _inflight = StartCoroutine(SendToServerCoroutine(pcm, sampleRate));
    }

    private IEnumerator SendToServerCoroutine(float[] pcm, int sampleRate)
    {
        UDebug.Log("[WhisperServerAdapter] POST coroutine started");

        byte[] wavBytes = PcmToWav16Mono(pcm, sampleRate);

        string wavPath = "";
        if (saveWavToDisk)
        {
            wavPath = Path.Combine(_tmpDir, $"seg_{DateTime.Now:yyyyMMdd_HHmmss_fff}.wav");
            try { File.WriteAllBytes(wavPath, wavBytes); }
            catch (Exception e) { UDebug.LogWarning($"[WhisperServerAdapter] Save wav failed: {e.Message}"); }
        }

        string url = server.BaseUrl.TrimEnd('/') + endpointPath;
        UDebug.Log($"[WhisperServerAdapter] POST {url}");

        var form = new WWWForm();

        // ✅ 最大兼容：很多 whisper.cpp server 用 "file"
        form.AddBinaryData("file", wavBytes, "audio.wav", "audio/wav");
        // ✅ 有些分支用 "audio"
        form.AddBinaryData("audio", wavBytes, "audio.wav", "audio/wav");

        if (!string.IsNullOrEmpty(language)) form.AddField("language", language);

        using (UnityWebRequest req = UnityWebRequest.Post(url, form))
        {
            req.timeout = 60;

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool ok = req.result == UnityWebRequest.Result.Success;
#else
            bool ok = !req.isNetworkError && !req.isHttpError;
#endif

            long code = req.responseCode;
            string body = req.downloadHandler != null ? req.downloadHandler.text : "";

            if (!ok)
            {
                UDebug.LogWarning(
                    $"[WhisperServerAdapter] HTTP FAIL code={code} err={req.error}\nURL: {url}\nBody:\n{body}"
                );
                CleanupTemp(wavPath);
                yield break;
            }

            if (logHttpBodyOnSuccess)
                UDebug.Log($"[WhisperServerAdapter] HTTP OK code={code}\nBody:\n{body}");

            string text = ExtractTextFromResponse(body);

            if (logRawText)
                UDebug.Log($"[WhisperServerAdapter] TEXT: {text}");

            if (!string.IsNullOrWhiteSpace(text))
            {
                OnRecognized?.Invoke(text);
                SpeechBus.EnsureExists().PublishFinal(text, wavPath);
            }

            CleanupTemp(wavPath);
        }
    }

    private void CleanupTemp(string wavPath)
    {
        if (!cleanupTempFiles) return;
        if (string.IsNullOrEmpty(wavPath)) return;
        if (!File.Exists(wavPath)) return;

        try { File.Delete(wavPath); } catch { }
    }

    private string ExtractTextFromResponse(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return "";
        string trimmed = body.Trim();

        // 纯文本直接返回
        if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
            return trimmed;

        // 常见 JSON key
        string t = TryExtractJsonString(trimmed, "text");
        if (!string.IsNullOrEmpty(t)) return t;

        t = TryExtractJsonString(trimmed, "transcription");
        if (!string.IsNullOrEmpty(t)) return t;

        t = TryExtractJsonString(trimmed, "result");
        if (!string.IsNullOrEmpty(t)) return t;

        // 兜底：返回原串，方便你看 server 格式
        return trimmed;
    }

    private string TryExtractJsonString(string json, string key)
    {
        string k = $"\"{key}\"";
        int ki = json.IndexOf(k, StringComparison.Ordinal);
        if (ki < 0) return "";

        int colon = json.IndexOf(':', ki + k.Length);
        if (colon < 0) return "";

        int q1 = json.IndexOf('\"', colon + 1);
        if (q1 < 0) return "";

        int q2 = q1 + 1;
        while (q2 < json.Length)
        {
            if (json[q2] == '\"' && json[q2 - 1] != '\\') break;
            q2++;
        }
        if (q2 >= json.Length) return "";

        string raw = json.Substring(q1 + 1, q2 - (q1 + 1));
        return raw.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\\"", "\"").Trim();
    }

    private byte[] PcmToWav16Mono(float[] pcm, int sampleRate)
    {
        int samples = pcm.Length;
        int dataLen = samples * 2;
        int byteRate = sampleRate * 2;

        using (var ms = new MemoryStream(44 + dataLen))
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataLen);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)1);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write((short)2);
            bw.Write((short)16);

            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataLen);

            for (int i = 0; i < samples; i++)
            {
                float v = Mathf.Clamp(pcm[i], -1f, 1f);
                short s = (short)Mathf.RoundToInt(v * 32767f);
                bw.Write(s);
            }

            return ms.ToArray();
        }
    }
}
