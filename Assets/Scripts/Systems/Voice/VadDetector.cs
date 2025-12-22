using System;
using System.Collections.Generic;
using UnityEngine;

public class VadDetector : MonoBehaviour
{
    [Header("VAD Params")]
    public float rmsThreshold = 0.001f;
    public float minSpeechSeconds = 0.2f;
    public float endSilenceSeconds = 0.2f;
    public float maxSegmentSeconds = 1.0f;

    [Header("Debug")]
    public bool logState = false;

    /// <summary>
    /// 当检测到一句话结束时触发：float PCM(-1~1), sampleRate
    /// </summary>
    public event Action<float[], int> OnSpeechSegmentReady;

    public int SampleRate { get; private set; } = 16000;

    private readonly List<float> _segment = new List<float>(16000);
    private bool _inSpeech = false;
    private float _speechTime = 0f;
    private float _silenceTime = 0f;

    public void SetSampleRate(int sr)
    {
        if (sr > 0) SampleRate = sr;
    }

    // VoiceService 兼容：Feed(float[] samples) / Feed(float[] samples, int count)
    public void Feed(float[] samples)
    {
        if (samples == null) return;
        Feed(samples, samples.Length);
    }

    public void Feed(float[] samples, int count)
    {
        if (samples == null || count <= 0) return;
        if (count > samples.Length) count = samples.Length;

        float rms = CalcRms(samples, count);
        float dt = (float)count / Mathf.Max(1, SampleRate);

        bool voiced = rms >= rmsThreshold;

        if (!_inSpeech)
        {
            if (voiced)
            {
                _inSpeech = true;
                _speechTime = 0f;
                _silenceTime = 0f;
                _segment.Clear();

                if (logState) UnityEngine.Debug.Log($"[VAD] Speech START (rms={rms:0.0000})");
            }
            else
            {
                return;
            }
        }

        // in speech
        Append(samples, count);
        _speechTime += dt;

        if (voiced)
        {
            _silenceTime = 0f;
        }
        else
        {
            _silenceTime += dt;
        }

        bool reachMinSpeech = _speechTime >= minSpeechSeconds;
        bool reachEndSilence = _silenceTime >= endSilenceSeconds;
        bool reachMaxLen = _speechTime >= maxSegmentSeconds;

        if ((reachMinSpeech && reachEndSilence) || reachMaxLen)
        {
            FlushSegment(forced: reachMaxLen);
        }
    }

    private void FlushSegment(bool forced)
    {
        if (!_inSpeech) return;

        _inSpeech = false;

        if (_segment.Count > 0)
        {
            var pcm = _segment.ToArray();
            OnSpeechSegmentReady?.Invoke(pcm, SampleRate);

            if (logState)
            {
                float len = (float)pcm.Length / Mathf.Max(1, SampleRate);
                UnityEngine.Debug.Log($"[VAD] Speech END len={len:0.00} forced={forced}");
                Debug.Log($"[VAD] SEGMENT READY samples={pcm.Length}");
            }
        }

        _segment.Clear();
        _speechTime = 0f;
        _silenceTime = 0f;
    }

    private void Append(float[] samples, int count)
    {
        // 避免 List 每次增长抖动
        if (_segment.Capacity < _segment.Count + count)
            _segment.Capacity = Mathf.Max(_segment.Capacity * 2, _segment.Count + count);

        for (int i = 0; i < count; i++)
            _segment.Add(samples[i]);
    }

    private float CalcRms(float[] samples, int count)
    {
        double sum = 0.0;
        for (int i = 0; i < count; i++)
        {
            double v = samples[i];
            sum += v * v;
        }
        double mean = sum / Mathf.Max(1, count);
        return (float)Math.Sqrt(mean);
    }
}
