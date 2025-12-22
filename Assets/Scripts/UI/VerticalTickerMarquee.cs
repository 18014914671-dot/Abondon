using UnityEngine;
using TMPro;

public class VerticalTickerMarquee : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform maskArea;   // 裁剪区域（TickerRoot）
    [SerializeField] private RectTransform textRect;   // 文本 RectTransform
    [SerializeField] private TextMeshProUGUI tmpText;

    [Header("Content")]
    [TextArea(3, 10)]
    [SerializeField]
    private string content =
        "ABONDON SYSTEM LOG\n" +
        "--------------------------------\n" +
        "Pilot connected...\n" +
        "Data recovered: 0x01\n" +
        "Navigation system online\n" +
        "Awaiting deployment...\n";

    [Header("Motion")]
    [SerializeField] private float speed = 40f; // 像素 / 秒
    [SerializeField] private float gap = 40f;   // 滚完后底部留白

    private float topSpawnY;
    private float bottomLimitY;

    private void Awake()
    {
        if (tmpText != null)
            tmpText.text = content;
    }

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        RecalculateBounds();
        PlaceAtTop();
    }

    private void Update()
    {
        if (textRect == null) return;

        // 向下移动
        textRect.anchoredPosition += Vector2.down * speed * Time.deltaTime;

        // 当整段文字完全滚出底部
        float textTopEdge = textRect.anchoredPosition.y;
        if (textTopEdge < bottomLimitY)
        {
            PlaceAtTop();
        }
    }

    private void RecalculateBounds()
    {
        if (maskArea == null || textRect == null) return;

        float maskHeight = maskArea.rect.height;

        topSpawnY = maskHeight + gap;
        bottomLimitY = -textRect.rect.height - gap;
    }

    private void PlaceAtTop()
    {
        textRect.anchoredPosition = new Vector2(
            textRect.anchoredPosition.x,
            topSpawnY
        );
    }

    // 可在运行时换内容（比如动态日志）
    public void SetContent(string newContent)
    {
        content = newContent;
        if (tmpText != null) tmpText.text = content;

        Canvas.ForceUpdateCanvases();
        RecalculateBounds();
        PlaceAtTop();
    }
}
