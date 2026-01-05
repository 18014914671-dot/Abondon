using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class TypingEnemy : MonoBehaviour
{
    [Header("Word")]
    public WordLibrary wordLibrary;
    public WordData assignedWord;

    [Header("UI")]
    [Tooltip("敌人头顶显示单词的 TMP_Text（世界空间/子物体都行）")]
    public TMP_Text wordLabel;

    public string WordText => assignedWord != null ? assignedWord.wordText : "";

    private void Awake()
    {
        if (assignedWord == null && wordLibrary != null)
        {
            assignedWord = wordLibrary.GetRandomWord();
        }

        RefreshLabel();
    }

    public void RefreshLabel()
    {
        if (wordLabel != null)
        {
            wordLabel.text = WordText;
        }
    }

    /// <summary>
    /// 输入匹配：大小写不敏感；只允许字母；不允许标点（标点会被剔除后再比）
    /// </summary>
    public bool IsMatch(string rawInput)
    {
        string a = TypingTextUtil.NormalizeLettersOnly(rawInput);
        string b = TypingTextUtil.NormalizeLettersOnly(WordText);
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
        return a == b;
    }
}
