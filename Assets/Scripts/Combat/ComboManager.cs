using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ComboManager : MonoBehaviour
{
    [Header("引用：答题系统")]
    public WordQuizManager quizManager;

    [Header("引用：武器控制器")]
    public WeaponController weaponController;

    [Header("UI：连击显示（可选）")]
    public TextMeshProUGUI comboText;

    [Header("基础规则")]
    public bool resetComboOnWrong = true;
    public bool updateMaxOnWrong = true;

    [Header("默认子弹模式")]
    public ComboShotMode defaultShotMode = ComboShotMode.SingleShot;

    [Header("连击奖励档位配置")]
    public List<ComboTier> comboTiers = new List<ComboTier>();

    [Header("当前状态（运行时查看）")]
    public int currentCombo = 0;
    public int maxCombo = 0;

    private void Awake()
    {
        if (quizManager == null)
            quizManager = FindFirstObjectByType<WordQuizManager>();

        if (weaponController == null)
            weaponController = FindFirstObjectByType<WeaponController>();
    }

    private void OnEnable()
    {
        // ✅ 强制防重复：每次启用都先退订再订阅
        Unsubscribe();
        Subscribe();
        UpdateComboUI();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (quizManager == null) return;
        quizManager.OnAnswerCorrect += HandleAnswerCorrect;
        quizManager.OnAnswerWrong += HandleAnswerWrong;
    }

    private void Unsubscribe()
    {
        if (quizManager == null) return;
        quizManager.OnAnswerCorrect -= HandleAnswerCorrect;
        quizManager.OnAnswerWrong -= HandleAnswerWrong;
    }

    private void HandleAnswerCorrect()
    {
        currentCombo++;
        if (currentCombo > maxCombo) maxCombo = currentCombo;

        UpdateComboUI();
        TriggerComboReward();
    }

    private void HandleAnswerWrong()
    {
        if (updateMaxOnWrong && currentCombo > maxCombo)
            maxCombo = currentCombo;

        if (resetComboOnWrong)
        {
            currentCombo = 0;
            UpdateComboUI();
        }
    }

    private void UpdateComboUI()
    {
        if (comboText == null) return;
        comboText.text = (currentCombo <= 0) ? "" : $"COMBO x{currentCombo}";
    }

    public int GetCurrentCombo() => currentCombo;

    public float GetComboMultiplier(float perCombo = 0.05f, float maxMul = 2.0f)
    {
        return Mathf.Min(1f + currentCombo * perCombo, maxMul);
    }

    private void TriggerComboReward()
    {
        if (weaponController == null) return;

        ComboShotMode modeToUse = defaultShotMode;

        int bestMin = int.MinValue;
        foreach (var tier in comboTiers)
        {
            if (tier == null) continue;

            if (currentCombo >= tier.minCombo && tier.minCombo > bestMin)
            {
                bestMin = tier.minCombo;
                modeToUse = tier.shotMode;
            }
        }

        switch (modeToUse)
        {
            case ComboShotMode.SingleShot:
                weaponController.FireSingleShot();
                break;
            case ComboShotMode.DoubleShot:
                weaponController.FireDoubleShot();
                break;
            case ComboShotMode.SpreadShot:
                weaponController.FireSpreadShot();
                break;
            default:
                weaponController.FireSingleShot();
                break;
        }
    }
}

public enum ComboShotMode { SingleShot, DoubleShot, SpreadShot }

[Serializable]
public class ComboTier
{
    public int minCombo = 1;
    public ComboShotMode shotMode = ComboShotMode.SingleShot;
}
