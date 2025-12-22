using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextFlashEffect : MonoBehaviour
{
    public float flashFrequency = 4f;
    public Color flashColor = Color.yellow;
    public float flashStrength = 1f;

    private TextMeshProUGUI tmp;
    private Color originalColor;
    private bool flashing = true;

    void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        originalColor = tmp.color;
    }

    void Update()
    {
        if (!flashing) return;

        float t = (Mathf.Sin(Time.time * flashFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
        float lerpT = t * flashStrength;

        tmp.color = Color.Lerp(originalColor, flashColor, lerpT);
    }

    public void StartFlash() => flashing = true;

    public void StopFlash()
    {
        flashing = false;
        tmp.color = originalColor;
    }
}
