using System;
using UnityEngine;

public enum StoryConditionType
{
    KillCountAtLeast,
    TotalDataCountAtLeast,
    CompletedTypesAtLeast,
    AnySingleTypeReachedThreshold // “任意一个 id 第一次达到阈值”
}

public enum StoryCombineMode
{
    AND,
    OR
}

[CreateAssetMenu(menuName = "Abondon/Story/Story Node", fileName = "StoryNode_")]
public class StoryNodeSO : ScriptableObject
{
    [Header("Identity")]
    public string nodeId = "Node_01";
    public bool triggerOnce = true;

    [Header("Conditions")]
    public StoryCombineMode combineMode = StoryCombineMode.AND;

    [Serializable]
    public struct Condition
    {
        public StoryConditionType type;

        [Tooltip("Used for 'AtLeast' types. Ignored for AnySingleTypeReachedThreshold.")]
        public int threshold;

        [Tooltip("Only used for AnySingleTypeReachedThreshold: require a specific id (leave empty = any).")]
        public string specificId;
    }

    public Condition[] conditions;

    [Header("Presentation")]
    [TextArea(2, 6)] public string hudMessage;

    [Tooltip("How long to show HUD message (seconds). 0 = presenter default.")]
    public float hudDuration = 0f;

    [Header("Optional Popup Residue (Data-like fragment)")]
    public bool usePopupResidue = false;
    public string popupTitle = "RESIDUE";
    [TextArea(2, 6)] public string popupBody;

    [Header("Optional: for future extension")]
    public string debugNote;
}
