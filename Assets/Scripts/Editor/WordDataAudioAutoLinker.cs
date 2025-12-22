// Assets/Editor/Abondon/WordDataAudioIndexLinker.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

public class WordDataAudioIndexLinker : EditorWindow
{
    [Header("Folders")]
    [SerializeField] private DefaultAsset wordDataFolder;   // 例如 Assets/Data/WordData/NGC9011
    [SerializeField] private DefaultAsset audioFolder;      // 例如 Assets/Data/Video/NGC1 (你图1的目录)

    [Header("Options")]
    [SerializeField] private string audioFieldName = "audioClip"; // WordData里AudioClip字段的真实变量名
    [SerializeField] private bool overwriteIfAlreadySet = false;  // 已经有音频时是否覆盖
    [SerializeField] private bool alsoTryVideoClip = true;        // 如果音频其实导入成VideoClip，也尝试匹配

    [MenuItem("Tools/Abondon/Link WordData Audio By Index")]
    public static void Open()
    {
        GetWindow<WordDataAudioIndexLinker>("Link Audio By Index");
    }

    private void OnGUI()
    {
        GUILayout.Label("WordData <-> Audio auto linker (by index)", EditorStyles.boldLabel);

        wordDataFolder = (DefaultAsset)EditorGUILayout.ObjectField("WordData Folder", wordDataFolder, typeof(DefaultAsset), false);
        audioFolder = (DefaultAsset)EditorGUILayout.ObjectField("Audio Folder", audioFolder, typeof(DefaultAsset), false);

        EditorGUILayout.Space(6);

        audioFieldName = EditorGUILayout.TextField("WordData Audio Field Name", audioFieldName);
        overwriteIfAlreadySet = EditorGUILayout.Toggle("Overwrite If Already Set", overwriteIfAlreadySet);
        alsoTryVideoClip = EditorGUILayout.Toggle("Also Try VideoClip", alsoTryVideoClip);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Link Now"))
        {
            LinkNow();
        }

        EditorGUILayout.HelpBox(
            "匹配规则：\n" +
            "WordData 资产名形如 '1_cosmos'，取前面的数字 1 -> 变成 '0001' 去音频目录找同名文件。\n" +
            "音频文件名必须是 4 位数字（0001/0002/...）。",
            MessageType.Info
        );
    }

    private void LinkNow()
    {
        if (wordDataFolder == null || audioFolder == null)
        {
            EditorUtility.DisplayDialog("Missing", "请先选择 WordData Folder 和 Audio Folder。", "OK");
            return;
        }

        string wordPath = AssetDatabase.GetAssetPath(wordDataFolder);
        string audioPath = AssetDatabase.GetAssetPath(audioFolder);

        if (!AssetDatabase.IsValidFolder(wordPath) || !AssetDatabase.IsValidFolder(audioPath))
        {
            EditorUtility.DisplayDialog("Invalid", "选的不是文件夹。", "OK");
            return;
        }

        // 1) 建立： "0001" -> AudioClip/VideoClip 映射
        var audioMap = new Dictionary<string, UnityEngine.Object>(StringComparer.OrdinalIgnoreCase);

        // AudioClip
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { audioPath }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            var key = Path.GetFileNameWithoutExtension(p).Trim(); // 0001
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
            if (clip != null && IsFourDigits(key))
                audioMap[key] = clip;
        }

        // VideoClip（如果你那些文件其实是 mp4 导入成 VideoClip）
        if (alsoTryVideoClip)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:VideoClip", new[] { audioPath }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var key = Path.GetFileNameWithoutExtension(p).Trim(); // 0001
                var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(p);
                if (clip != null && IsFourDigits(key) && !audioMap.ContainsKey(key))
                    audioMap[key] = clip;
            }
        }

        // 2) 遍历 WordData 并写入
        var wordGuids = AssetDatabase.FindAssets("t:WordData", new[] { wordPath });

        int linked = 0, missing = 0, skipped = 0, overwritten = 0, badName = 0;

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Link WordData Audio By Index");

        foreach (var guid in wordGuids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var wordAssetName = Path.GetFileNameWithoutExtension(assetPath); // 1_cosmos

            if (!TryGetLeadingIndex(wordAssetName, out int idx))
            {
                badName++;
                continue;
            }

            string key = idx.ToString("D4"); // 0001

            var word = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (word == null) continue;

            var so = new SerializedObject(word);

            var audioProp = so.FindProperty(audioFieldName);
            if (audioProp == null || audioProp.propertyType != SerializedPropertyType.ObjectReference)
            {
                Debug.LogError($"[Linker] 找不到字段 '{audioFieldName}' 或字段不是 ObjectReference：{assetPath}");
                skipped++;
                continue;
            }

            bool alreadySet = audioProp.objectReferenceValue != null;
            if (alreadySet && !overwriteIfAlreadySet)
            {
                skipped++;
                continue;
            }

            if (audioMap.TryGetValue(key, out var clipObj) && clipObj != null)
            {
                Undo.RecordObject(word, "Assign Clip");

                audioProp.objectReferenceValue = clipObj;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(word);

                if (alreadySet) overwritten++;
                else linked++;
            }
            else
            {
                missing++;
                Debug.LogWarning($"[MissingAudio] key={key}  WordData={wordAssetName}  path={assetPath}");
            }
        }

        Undo.CollapseUndoOperations(group);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Linker Done] linked={linked}, overwritten={overwritten}, missing={missing}, skipped={skipped}, badName={badName}");
        EditorUtility.DisplayDialog("Done",
            $"linked={linked}\n" +
            $"overwritten={overwritten}\n" +
            $"missing={missing}\n" +
            $"skipped={skipped}\n" +
            $"badName={badName}\n\n" +
            $"详情看 Console。",
            "OK");
    }

    private static bool TryGetLeadingIndex(string assetName, out int index)
    {
        // 匹配： "1_cosmos" / "001_cosmos" / "12_xxx"
        // 取开头连续数字
        var m = Regex.Match(assetName, @"^(\d+)");
        if (m.Success && int.TryParse(m.Groups[1].Value, out index))
            return true;

        index = -1;
        return false;
    }

    private static bool IsFourDigits(string s)
        => Regex.IsMatch(s, @"^\d{4}$");
}
#endif
