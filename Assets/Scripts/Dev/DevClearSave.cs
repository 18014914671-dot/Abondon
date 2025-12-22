using UnityEngine;

/// <summary>
/// 开发期工具：一键清除 PlayerInventory 存档
/// 默认快捷键：F9
/// </summary>
public class DevClearSave : MonoBehaviour
{
    [Header("Hotkey")]
    public KeyCode clearKey = KeyCode.F9;

    [Header("Options")]
    [Tooltip("Also reset runtime kill count and story progress if available.")]
    public bool resetStoryProgress = true;

    private void Update()
    {
        if (!Input.GetKeyDown(clearKey)) return;

        // 1️⃣ 清除 Data 存档
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.ResetAll(true);
            Debug.Log("[DevClearSave] Inventory save cleared.");
        }
        else
        {
            Debug.LogWarning("[DevClearSave] PlayerInventory.Instance not found.");
        }

        // 2️⃣ 可选：清除剧情运行时进度（击杀数等）
        if (resetStoryProgress && StoryProgressTracker.Instance != null)
        {
            StoryProgressTracker.Instance.ResetKills();
            Debug.Log("[DevClearSave] Story progress reset (kills).");
        }

        Debug.Log("<color=#6fd3ff>[DevClearSave] Press Play again or continue testing.</color>");
    }
}
