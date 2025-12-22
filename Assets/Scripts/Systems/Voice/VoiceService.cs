using System;
using UnityEngine;

/// <summary>
/// 持续监听麦克风，把音频帧喂给 VadDetector。
/// 放置：Assets/Scripts/Systems/Voice/VoiceService.cs
/// </summary>
public class VoiceService : MonoBehaviour
{
    public static VoiceService Instance { get; private set; }

    [Header("Mic")]
    public string deviceName = null;          // null = default
    public int sampleRate = 16000;
    public int clipLengthSec = 1;             // 循环录音片段长度（秒）
    public int frameSamples = 512;            // 每帧取多少采样送给VAD

    [Header("Refs")]
    public VadDetector vad;

    public bool autoStart = true;

    private AudioClip _clip;
    private int _lastPos;
    private float[] _frame;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!vad) vad = GetComponentInChildren<VadDetector>();
        _frame = new float[frameSamples];
    }

    private void Start()
    {
        // ✅ 一次性把 sampleRate 告诉 VAD（dt 才会正确）
        if (vad != null) vad.SetSampleRate(sampleRate);

        if (autoStart) StartMic();
    }

    private void OnDisable()
    {
        StopMic();
    }

    public void StartMic()
    {
        if (_clip != null) return;

        _clip = Microphone.Start(deviceName, true, clipLengthSec, sampleRate);
        _lastPos = 0;
        Debug.Log("[VoiceService] Mic started. device=" + (deviceName ?? "default"));
    }

    public void StopMic()
    {
        if (_clip == null) return;

        Microphone.End(deviceName);
        _clip = null;
        _lastPos = 0;
        Debug.Log("[VoiceService] Mic stopped.");
    }

    private void Update()
    {
        if (_clip == null || vad == null) return;

        int pos = Microphone.GetPosition(deviceName);
        if (pos < 0) return;

        int samplesAvailable = pos - _lastPos;
        if (samplesAvailable < 0) samplesAvailable += _clip.samples;

        while (samplesAvailable >= frameSamples)
        {
            ReadFrame(_lastPos, _frame);
            _lastPos = (_lastPos + frameSamples) % _clip.samples;
            samplesAvailable -= frameSamples;

            // ✅ 这里第二个参数应该是 count（本帧采样数），不是 sampleRate
            vad.Feed(_frame, _frame.Length);
        }
    }

    private void ReadFrame(int startSample, float[] buffer)
    {
        int clipSamples = _clip.samples;

        if (startSample + buffer.Length <= clipSamples)
        {
            _clip.GetData(buffer, startSample);
        }
        else
        {
            int firstLen = clipSamples - startSample;
            float[] tmp1 = new float[firstLen];
            float[] tmp2 = new float[buffer.Length - firstLen];

            _clip.GetData(tmp1, startSample);
            _clip.GetData(tmp2, 0);

            Array.Copy(tmp1, 0, buffer, 0, tmp1.Length);
            Array.Copy(tmp2, 0, buffer, tmp1.Length, tmp2.Length);
        }
    }
}
