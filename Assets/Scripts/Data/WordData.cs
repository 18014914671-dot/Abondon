using UnityEngine;

[CreateAssetMenu(menuName = "Abandon/Word Data", fileName = "NewWordData")]
public class WordData : ScriptableObject
{
    [Header("唯一 ID（可选，例如 w_0001）")]
    public string id;

    [Header("显示在选项里的英文单词")]
    public string wordText;

    [Header("发音音频")]
    public AudioClip audioClip;

    [Header("（可选）中文释义 / 备注")]
    [TextArea]
    public string meaning;
}
