using TMPro;
using UnityEngine;

public class DataHUD : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text totalText; // 显示总数，比如 "Data: 12"

    private void OnEnable()
    {
        // 订阅事件：每次数据变化就刷新
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnDataChanged += HandleChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnDataChanged -= HandleChanged;
    }

    private void HandleChanged(string id, int newCount, bool firstTime)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (totalText == null) return;

        var inv = PlayerInventory.Instance != null
            ? PlayerInventory.Instance
            : FindFirstObjectByType<PlayerInventory>();

        if (inv == null)
        {
            totalText.text = "Data: -";
            return;
        }

        // 这里先显示“总收集次数”（所有 id 的 count 累加）
        totalText.text = $"Data: {inv.GetTotalCount()}";
    }
}
