using UnityEngine;
using UnityEngine.Events;

public class LevelClearPanelController : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("Button Hook")]
    [Tooltip("把 ReturnToBaseHub / BaseHubManager 的回基地方法拖到这里")]
    public UnityEvent onReturnToHub;

    private void Awake()
    {
        if (root == null) root = gameObject;
        Hide();
    }

    public void Show()
    {
        if (root != null) root.SetActive(true);
        Time.timeScale = 0f; // 可选：通关暂停
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
        Time.timeScale = 1f;
    }

    // 给 Button 直接绑这个也行
    public void ClickReturnToHub()
    {
        Time.timeScale = 1f;
        onReturnToHub?.Invoke();
    }
}
