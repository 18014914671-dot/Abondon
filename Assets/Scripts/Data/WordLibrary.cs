using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Abandon/Word Library", fileName = "NewWordLibrary")]
public class WordLibrary : ScriptableObject
{
    [Header("这一关所有可用单词（比如 500 个）")]
    public List<WordData> words = new List<WordData>();

    /// <summary>
    /// 从词库中随机拿一个单词（简单版：允许重复）
    /// </summary>
    public WordData GetRandomWord()
    {
        if (words == null || words.Count == 0) return null;
        int index = Random.Range(0, words.Count);
        return words[index];
    }
}
