using System.Collections;
using TMPro;
using UnityEngine;

public class StoryPresenter : MonoBehaviour
{
    [Header("HUD Text (TMP_Text)")]
    public TMP_Text hudText;

    public float defaultHudDuration = 2.2f;

    [Header("Optional: DataPopupUI residue")]
    public DataPopupUI dataPopupUI;

    private Coroutine _hudRoutine;

    private void Awake()
    {
        // 兜底：如果你忘记拖，自动找一个 TMP_Text（优先在 Canvas 下找）
        if (hudText == null)
        {
            hudText = FindFirstObjectByType<TMP_Text>();
        }

        if (dataPopupUI == null)
        {
            dataPopupUI = FindFirstObjectByType<DataPopupUI>();
        }
        
        if (hudText != null)
            hudText.text = "";
    }

    public void Present(StoryNodeSO node)
    {
        if (node == null) return;

        if (!string.IsNullOrEmpty(node.hudMessage) && hudText != null)
        {
            float dur = (node.hudDuration > 0f) ? node.hudDuration : defaultHudDuration;

            if (_hudRoutine != null) StopCoroutine(_hudRoutine);
            _hudRoutine = StartCoroutine(ShowHudRoutine(node.hudMessage, dur));
        }

        if (node.usePopupResidue && dataPopupUI != null)
        {
            dataPopupUI.Show(node.popupTitle, null, node.popupBody);
        }
    }

    private IEnumerator ShowHudRoutine(string msg, float seconds)
    {
        hudText.text = msg;
        yield return new WaitForSecondsRealtime(seconds);
        hudText.text = "";
        _hudRoutine = null;
    }
}
