using System;
using UnityEngine;

[Serializable]
public class SpeechResult
{
    public string rawText;
    public string normalizedText;
    public float timestamp;
    public string wavPath;

    public SpeechResult(string raw, string norm, float t, string wav = "")
    {
        rawText = raw;
        normalizedText = norm;
        timestamp = t;
        wavPath = wav;
    }
}
