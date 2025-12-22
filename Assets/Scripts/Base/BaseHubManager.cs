using UnityEngine;
using UnityEngine.SceneManagement;

public class BaseHubManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject homePanel;
    public GameObject dataVaultPanel;
    public GameObject optionsPanel;

    [Header("Scenes")]
    public string levelSelectSceneName = "LevelSelect";
    public string mainMenuSceneName = "MainMenu";

    private void Start()
    {
        ShowHome();
    }

    // ---------- Panel Switch ----------
    public void ShowHome()
    {
        SetPanels(home: true, vault: false, options: false);
    }

    public void ShowDataVault()
    {
        SetPanels(home: false, vault: true, options: false);
    }

    public void ShowOptions()
    {
        SetPanels(home: false, vault: false, options: true);
    }

    private void SetPanels(bool home, bool vault, bool options)
    {
        if (homePanel) homePanel.SetActive(home);
        if (dataVaultPanel) dataVaultPanel.SetActive(vault);
        if (optionsPanel) optionsPanel.SetActive(options);
    }

    // ---------- Navigation ----------
    public void GoExplore()
    {
        SceneManager.LoadScene(levelSelectSceneName);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
