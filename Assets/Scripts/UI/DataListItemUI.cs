using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DataListItemUI : MonoBehaviour
{
    [Header("UI 引用")]
    public Image iconImage;        // 图标
    public TMP_Text nameText;      // 名字
    public TMP_Text countText;     // 次数 例：3 / 20

    private DataItem _item;

    /// <summary>
    /// 第一次创建这个条目时调用
    /// </summary>
    public void Setup(DataItem item, int current, int required)
    {
        _item = item;

        if (iconImage != null) iconImage.sprite = item.icon;
        if (nameText != null) nameText.text = item.displayName;

        UpdateCount(current, required);
    }

    /// <summary>
    /// 同一个 Data 被再次拾取时，刷新计数文本
    /// </summary>
    public void UpdateCount(int current, int required)
    {
        if (countText != null)
        {
            countText.text = $"{current} / {required}";
        }
    }
}
