using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DataHUDRow : MonoBehaviour
{
    [Header("UI")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text progressText;

    // 备用：如果没找到 DataItem，就显示 id
    public void SetFallback(string id, int count)
    {
        if (nameText != null) nameText.text = id;
        if (progressText != null) progressText.text = $"x{count}";
        if (iconImage != null) iconImage.enabled = false;
    }

    public void Set(DataItem item, int count)
    {
        if (item == null)
        {
            SetFallback("Unknown", count);
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = (item.icon != null);
        }

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(item.displayName) ? item.id : item.displayName;

        if (progressText != null)
        {
            int req = Mathf.Max(1, item.requiredCollectCount);
            int clamped = Mathf.Min(count, req);
            progressText.text = $"{clamped}/{req}";
        }
    }
}
