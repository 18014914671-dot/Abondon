using UnityEngine;

[CreateAssetMenu(menuName = "Abondon/Data Item", fileName = "NewDataItem")]
public class DataItem : ScriptableObject
{
    [Header("ID（唯一标识，比如 abandon_01）")]
    [Tooltip("用于代码中区分不同 Data 的唯一 ID，不要重复")]
    public string id;

    [Header("展示名称")]
    [Tooltip("玩家在 UI 上看到的名字")]
    public string displayName;

    [Header("图标")]
    [Tooltip("在弹窗和左侧列表中显示的图标")]
    public Sprite icon;

    [Header("说明文字")]
    [TextArea]
    [Tooltip("弹窗里展示的描述文本")]
    public string description;

    [Header("收集需求设置")]
    [Tooltip("同一个 Data 需要被拾取多少次才算“收集完成”，比如 10、20 次")]
    [Min(1)]
    public int requiredCollectCount = 10;
}
