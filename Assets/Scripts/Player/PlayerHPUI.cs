using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerHPUI : MonoBehaviour
{
    public PlayerHealth playerHealth;

    [Header("UI (Optional)")]
    public Slider hpSlider;
    public TMP_Text hpText;

    private void Awake()
    {
        if (playerHealth == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) playerHealth = p.GetComponent<PlayerHealth>();
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null) playerHealth.OnHPChanged += Refresh;
        if (playerHealth != null) Refresh(playerHealth.CurrentHP, playerHealth.MaxHP);
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.OnHPChanged -= Refresh;
    }

    private void Refresh(int cur, int max)
    {
        if (hpSlider != null)
        {
            hpSlider.minValue = 0;
            hpSlider.maxValue = Mathf.Max(1, max);
            hpSlider.value = Mathf.Clamp(cur, 0, max);
        }

        if (hpText != null)
        {
            hpText.text = $"HP {cur}/{max}";
        }
    }
}
