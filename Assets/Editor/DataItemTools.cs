using UnityEditor;
using UnityEngine;

public static class DataItemTools
{
    [MenuItem("Abondon/Data/Sync DataItem.id From Asset Name")]
    public static void SyncIdFromAssetName()
    {
        string[] guids = AssetDatabase.FindAssets("t:DataItem");
        int changed = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<DataItem>(path);
            if (item == null) continue;

            string assetName = item.name;
            if (item.id != assetName)
            {
                Undo.RecordObject(item, "Sync DataItem.id");
                item.id = assetName;
                EditorUtility.SetDirty(item);
                changed++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[DataItemTools] Synced {changed} DataItem ids from asset names.");
    }

    [MenuItem("Abondon/Data/Scan Duplicate DataItem.id")]
    public static void ScanDuplicates()
    {
        string[] guids = AssetDatabase.FindAssets("t:DataItem");
        var map = new System.Collections.Generic.Dictionary<string, string>();

        int dup = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<DataItem>(path);
            if (item == null) continue;

            string key = item.id;
            if (string.IsNullOrEmpty(key)) continue;

            if (map.TryGetValue(key, out var existedPath))
            {
                dup++;
                Debug.LogError($"[Duplicate id] {key}\n  A: {existedPath}\n  B: {path}");
            }
            else
            {
                map[key] = path;
            }
        }

        Debug.Log($"[DataItemTools] Duplicate count = {dup}");
    }
}
