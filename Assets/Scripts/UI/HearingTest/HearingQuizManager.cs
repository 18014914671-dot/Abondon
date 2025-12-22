using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class HearingQuestion
{
    public AudioClip audioClip;               // 要播放的单词
    public string[] options = new string[3];  // 三个文字选项
    public int correctIndex;                  // 正确选项下标：0,1,2
}

public class HearingQuizManager : MonoBehaviour
{
    [Header("Quiz Data")]
    public HearingQuestion[] questions;

    [Header("UI Refs")]
    public Button[] optionButtons;       // 三个按钮
    public TMP_Text[] optionTexts;       // 按钮上的文字（TextMeshPro）

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Combat")]
    public WeaponController weaponController;

    // ✅ 给外部订阅（连击、特效、音效、爆炸倍率都能接）
    public event Action OnAnswerCorrect;
    public event Action OnAnswerWrong;

    private int currentQuestionIndex = 0;

    private void Start()
    {
        SetupQuestion(currentQuestionIndex);
    }

    public void SetupQuestion(int index)
    {
        if (questions == null || questions.Length == 0)
        {
            Debug.LogWarning("HearingQuizManager: 没有题目数据！");
            return;
        }

        if (index < 0 || index >= questions.Length)
            index = 0;

        currentQuestionIndex = index;
        var q = questions[currentQuestionIndex];

        // 播放音频
        if (audioSource != null && q.audioClip != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(q.audioClip);
        }

        // 填充 UI
        for (int i = 0; i < optionButtons.Length && i < q.options.Length; i++)
        {
            int choiceIndex = i;

            if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null)
            {
                optionTexts[i].text = q.options[i];
            }

            if (optionButtons[i] != null)
            {
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(choiceIndex));
            }
        }
    }

    private void OnOptionSelected(int selectedIndex)
    {
        if (questions == null || questions.Length == 0) return;

        var q = questions[currentQuestionIndex];

        if (selectedIndex == q.correctIndex)
        {
            Debug.Log("答对了！发射子弹攻击最近敌人。");

            OnAnswerCorrect?.Invoke();

            if (weaponController != null)
            {
                weaponController.FireAtNearestEnemy();
            }

            int next = (currentQuestionIndex + 1) % questions.Length;
            SetupQuestion(next);
        }
        else
        {
            Debug.Log("答错了 QAQ");
            OnAnswerWrong?.Invoke();
        }
    }
}
