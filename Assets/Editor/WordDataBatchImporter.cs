using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class WordDataBatchImporter : EditorWindow
{
    [Header("CSV 文本（从 Excel 导出的 CSV，作为 TextAsset 放在工程里）")]
    public TextAsset csvFile;

    [Header("WordData 生成到哪个文件夹下")]
    public string wordDataFolder = "Assets/Data/WordData/NGC9011";

    [Header("WordLibrary 资源保存路径")]
    public string wordLibraryAssetPath = "Assets/Data/WordData/WordLibrary_NGC9011.asset";

    [MenuItem("Abandon/导入单词库（从 CSV 批量创建 WordData）")]
    public static void ShowWindow()
    {
        GetWindow<WordDataBatchImporter>("WordData 批量导入");
    }

    private void OnGUI()
    {
        GUILayout.Label("从 CSV 批量创建 WordData + WordLibrary", EditorStyles.boldLabel);

        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV 文件", csvFile, typeof(TextAsset), false);
        wordDataFolder = EditorGUILayout.TextField("WordData 文件夹", wordDataFolder);
        wordLibraryAssetPath = EditorGUILayout.TextField("WordLibrary 资源路径", wordLibraryAssetPath);

        EditorGUILayout.Space();

        if (GUILayout.Button("开始导入"))
        {
            Import();
        }
    }

    private void Import()
    {
        if (csvFile == null)
        {
            Debug.LogError("请先指定 CSV 文件（从 Excel 导出的 TextAsset）。");
            return;
        }

        // 确保目标文件夹存在
        if (!AssetDatabase.IsValidFolder(wordDataFolder))
        {
            // 递归创建文件夹
            CreateFolderRecursive(wordDataFolder);
        }

        // 解析 CSV 文本
        string[] lines = csvFile.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        var createdWordList = new List<WordData>();

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // 按逗号或制表符分列（兼容不同导出方式）
            string[] cols = line.Split(new[] { ',', '\t' });

            if (cols.Length < 2)
            {
                Debug.LogWarning($"第 {i + 1} 行列数不足，跳过：{line}");
                continue;
            }

            // 第 0 列：序号（可选）
            string indexStr = cols[0].Trim();

            // 第 1 列：英文单词
            string wordText = cols[1].Trim();

            // 第 2 列：中文释义（如果有）
            string meaning = cols.Length >= 3 ? cols[2].Trim() : "";

            // 创建 WordData ScriptableObject
            WordData wordData = ScriptableObject.CreateInstance<WordData>();
            wordData.id = $"ngc9011_{indexStr}";
            wordData.wordText = wordText;
            wordData.meaning = meaning;
            // audioClip 先留空，之后可以根据命名规则再自动匹配

            // 生成资源路径（避免重名覆盖，加上 index）
            string safeName = MakeSafeFileName(wordText);
            string assetPath = $"{wordDataFolder}/{indexStr}_{safeName}.asset";

            AssetDatabase.CreateAsset(wordData, assetPath);
            createdWordList.Add(wordData);
        }

        // 创建 / 覆盖 WordLibrary
        WordLibrary library = AssetDatabase.LoadAssetAtPath<WordLibrary>(wordLibraryAssetPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<WordLibrary>();
            AssetDatabase.CreateAsset(library, wordLibraryAssetPath);
        }

        library.words = createdWordList;
        EditorUtility.SetDirty(library);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"导入完成！共创建 {createdWordList.Count} 个 WordData，并写入 WordLibrary：{wordLibraryAssetPath}");
    }

    /// <summary>
    /// 递归创建多级文件夹，例如 "Assets/Data/WordData/NGC9011"
    /// </summary>
    private void CreateFolderRecursive(string folderPath)
    {
        // 标准化分隔符
        folderPath = folderPath.Replace("\\", "/");

        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] parts = folderPath.Split('/');
        string current = parts[0]; // 一般是 "Assets"

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    /// <summary>
    /// 把单词变成适合当文件名的形式（去掉不合法字符）
    /// </summary>
    private string MakeSafeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }
}
