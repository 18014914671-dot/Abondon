using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DataVaultList : MonoBehaviour
{
    [Header("Data")]
    public DataDatabase database;

    [Header("UI")]
    public RectTransform contentRoot;
    public DataHUDRow rowPrefab;

    [Header("Popup")]
    public DataPopupUI popupUI; // ✅ Inspector 里拖 BaseHub 的 DataPopupUI
    [TextArea] public string lockedSuffix = "\n\n[LOCKED] You haven't collected this Data yet.";

    [Header("Style")]
    [Range(0f, 1f)] public float lockedAlpha = 0.35f;

    private readonly List<DataHUDRow> _rows = new();

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (database == null || contentRoot == null || rowPrefab == null) return;
        if (PlayerInventory.Instance == null) return;

        Clear();

        foreach (var item in database.allItems)
        {
            if (item == null) continue;

            int count = PlayerInventory.Instance.GetCount(item.name); // ✅ key = asset name
            var row = Instantiate(rowPrefab, contentRoot);
            row.gameObject.SetActive(true);
            _rows.Add(row);

            row.Set(item, count);

            bool owned = count > 0;
            ApplyLockedVisual(row.gameObject, owned);

            BindClick(row.gameObject, item, owned, count);
        }

        ForceRebuildLayout();
    }

    private void BindClick(GameObject rowObj, DataItem item, bool owned, int count)
    {
        // 1) 确保有 Button
        var btn = rowObj.GetComponent<Button>();
        if (btn == null) btn = rowObj.AddComponent<Button>();

        // 2) 不要 Add Image！避免和 TMP 冲突
        //    只需要保证“某个 Graphic 的 raycastTarget = true”
        Graphic g = rowObj.GetComponent<Graphic>();
        if (g == null)
        {
            // 如果根上没有 Graphic，就在子物体里找一个（比如 Icon Image / TMP）
            g = rowObj.GetComponentInChildren<Graphic>(true);
        }

        if (g != null)
        {
            g.raycastTarget = true;
        }
        else
        {
            Debug.LogWarning("[DataVaultList] No Graphic found for raycast. Click might not work on: " + rowObj.name);
        }

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            if (popupUI == null)
            {
                Debug.LogWarning("[DataVaultList] popupUI is null. Please assign it in Inspector.");
                return;
            }

            string title = string.IsNullOrEmpty(item.displayName) ? item.name : item.displayName;

            int req = Mathf.Max(1, item.requiredCollectCount);
            int clamped = Mathf.Clamp(count, 0, req);

            string progress = owned ? $"Progress: {clamped}/{req}" : $"Progress: 0/{req}";
            string desc = item.description ?? "";
            desc = $"{desc}\n\n{progress}";
            if (!owned) desc += lockedSuffix;

            // ✅ 注意参数顺序：你的 DataPopupUI 是 (string, Sprite, string)
            popupUI.Show(title, item.icon, desc);
        });
    }

    private void ApplyLockedVisual(GameObject rowObj, bool owned)
    {
        var group = rowObj.GetComponent<CanvasGroup>();
        if (group == null) group = rowObj.AddComponent<CanvasGroup>();
        group.alpha = owned ? 1f : lockedAlpha;
    }

    private void Clear()
    {
        foreach (var r in _rows)
            if (r != null) Destroy(r.gameObject);
        _rows.Clear();
    }

    private void ForceRebuildLayout()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        Canvas.ForceUpdateCanvases();
    }
}
