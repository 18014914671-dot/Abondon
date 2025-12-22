using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToBaseHub : MonoBehaviour
{
    [Header("Scene")]
    public string baseHubSceneName = "BaseHub";

    [Header("Safety")]
    public bool saveInventoryBeforeLeave = true;
    public bool resetKillsBeforeLeave = false;

    public void Go()
    {
        if (saveInventoryBeforeLeave && PlayerInventory.Instance != null)
            PlayerInventory.Instance.SaveNow();

        if (resetKillsBeforeLeave && StoryProgressTracker.Instance != null)
            StoryProgressTracker.Instance.ResetKills();

        SceneManager.LoadScene(baseHubSceneName);
    }
}
