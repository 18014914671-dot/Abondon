using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DataPopupUI : MonoBehaviour
{
    public static DataPopupUI Instance;

    [Header("UI References")]
    public CanvasGroup canvasGroup;
    public Image iconImage;
    public TMP_Text titleText;
    public TMP_Text descriptionText;

    [Header("Settings")]
    public float showDuration = 2f;

    private float timer;
    private bool isShowing = false;

    private void Awake()
    {
        Instance = this;
        HideInstant();
    }

    public void Show(string title, Sprite icon, string description)
    {
        if (canvasGroup == null)
        {
            Debug.LogError("DataPopupUI: CanvasGroup 未设置");
            return;
        }

        titleText.text = title;
        descriptionText.text = description;

        if (iconImage != null)
            iconImage.sprite = icon;

        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        timer = showDuration;
        isShowing = true;
    }

    private void Update()
    {
        if (!isShowing) return;

        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            HideInstant();
            isShowing = false;
        }
    }

    public void HideInstant()
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
