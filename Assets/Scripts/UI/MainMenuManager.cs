using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Start Game")]
    [SerializeField] private string nextSceneName = "LevelSelect"; // 先写占位，后面你改成真实场景名

    [Header("Options UI")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_Text masterVolumeValueText; // 可不填
    [SerializeField] private Toggle fullscreenToggle;

    private const string PREF_VOLUME = "opt_master_volume";
    private const string PREF_FULLSCREEN = "opt_fullscreen";

    private void Start()
    {
        // 读设置
        float vol = PlayerPrefs.GetFloat(PREF_VOLUME, 1f);
        int fs = PlayerPrefs.GetInt(PREF_FULLSCREEN, 1);

        AudioListener.volume = vol;
        Screen.fullScreen = (fs == 1);

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(vol);
            UpdateVolumeText(vol);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        }

        ShowMainMenu();
    }

    // ===== 主菜单按钮 =====
    public void OnClickStartGame()
    {
        SceneManager.LoadScene(nextSceneName);
    }

    public void OnClickOpenOptions()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ===== 选项面板按钮/控件 =====
    public void OnClickBackFromOptions()
    {
        ShowMainMenu();
    }

    public void OnMasterVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(PREF_VOLUME, value);
        PlayerPrefs.Save();
        UpdateVolumeText(value);
    }

    public void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(PREF_FULLSCREEN, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    private void UpdateVolumeText(float value)
    {
        if (masterVolumeValueText != null)
            masterVolumeValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}
