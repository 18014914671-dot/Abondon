using UnityEngine;
using TMPro;
using System.Collections;

public class TypewriterText : MonoBehaviour
{
    [Header("每个字出现的间隔（秒）")]
    public float charInterval = 0.03f;

    [Header("是否在开始时自动播放")]
    public bool playOnAwake = false;

    private TextMeshProUGUI tmp;
    private Coroutine typingCoroutine;

    private void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        if (playOnAwake && !string.IsNullOrEmpty(tmp.text))
        {
            Play(tmp.text);
        }
    }

    /// <summary>
    /// 播放打字效果
    /// </summary>
    public void Play(string fullText)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeRoutine(fullText));
    }

    private IEnumerator TypeRoutine(string fullText)
    {
        tmp.text = "";
        foreach (char c in fullText)
        {
            tmp.text += c;
            yield return new WaitForSeconds(charInterval);
        }
    }

    /// <summary>
    /// 立即跳到最终文本
    /// </summary>
    public void ShowAll(string fullText)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        tmp.text = fullText;
    }
}
