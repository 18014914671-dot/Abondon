using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class WordQuizManager : MonoBehaviour
{
    [Header("这一关使用的单词库")]
    public WordLibrary wordLibrary;

    [Header("播放单词发音的 AudioSource")]
    public AudioSource audioSource;

    [Header("UI：三个选项按钮 (0,1,2)")]
    public Button[] optionButtons;

    [Header("UI：按钮上的文字 (0,1,2)")]
    public TMP_Text[] optionTexts;

    [Header("Hotkeys")]
    public bool allowHotkeys = true;

    [Header("UI VFX (Animator)")]
    [Tooltip("拖 UIVFX_Burst_Correct 对象上的 Animator")]
    public Animator vfxCorrectAnimator;

    [Tooltip("可选：拖 UIVFX_Burst_Wrong 对象上的 Animator；没有也没关系")]
    public Animator vfxWrongAnimator;

    [Tooltip("Animator 里 Burst 动画 State 的名字（不是 Trigger 名）")]
    public string vfxBurstStateName = "Burst";

    [Header("Delay")]
    [Tooltip("给震动/爆光动画留时间，再切下一题。0.1~0.25 都行")]
    public float nextQuestionDelay = 0.12f;

    [Header("FX（按钮爆光/抖动/闪色）")]
    public WordQuizFeedbackFX feedbackFX;

    // ✅ 本题只允许结算一次
    private bool _answeredThisQuestion = false;

    public WordData currentCorrectWord { get; private set; }
    public List<WordData> currentOptions { get; private set; } = new List<WordData>();

    public event Action OnAnswerCorrect;
    public event Action OnAnswerWrong;

    private System.Random rng = new System.Random();

    private void Start()
    {
        GenerateNewQuestion();
    }

    private void Update()
    {
        if (!allowHotkeys) return;

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) PressOptionButton(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) PressOptionButton(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) PressOptionButton(2);
    }

    private void PressOptionButton(int index)
    {
        if (optionButtons == null) return;
        if (index < 0 || index >= optionButtons.Length) return;

        var btn = optionButtons[index];
        if (btn == null) return;
        if (!btn.gameObject.activeInHierarchy) return;
        if (!btn.interactable) return;

        // ✅ 直接提交（不靠 EventSystem Submit，避免双触发）
        SubmitAnswer(index);
    }

    public void GenerateNewQuestion()
    {
        _answeredThisQuestion = false;
        currentOptions.Clear();

        if (wordLibrary == null)
        {
            Debug.LogWarning("WordQuizManager: 没有绑定 WordLibrary！");
            return;
        }

        currentCorrectWord = wordLibrary.GetRandomWord();
        if (currentCorrectWord == null)
        {
            Debug.LogWarning("WordQuizManager: 词库为空或无效。");
            return;
        }

        currentOptions.Add(currentCorrectWord);

        int safety = 0;
        while (currentOptions.Count < 3 && safety < 1000)
        {
            safety++;
            var candidate = wordLibrary.GetRandomWord();
            if (candidate == null) break;
            if (candidate == currentCorrectWord) continue;
            if (currentOptions.Contains(candidate)) continue;

            currentOptions.Add(candidate);
        }

        Shuffle(currentOptions);
        SetupOptionButtonsUI();
        PlayCurrentWordAudio();
    }

    private void SetupOptionButtonsUI()
    {
        if (optionButtons == null || optionButtons.Length == 0) return;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            var btn = optionButtons[i];
            if (btn == null) continue;

            bool hasOption = (i < currentOptions.Count);
            btn.interactable = hasOption;
            btn.gameObject.SetActive(hasOption);

            if (!hasOption) continue;

            if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null)
                optionTexts[i].text = currentOptions[i].wordText;

            btn.onClick.RemoveAllListeners();
            int idx = i;
            btn.onClick.AddListener(() => SubmitAnswer(idx));
        }

        if (EventSystem.current != null &&
            optionButtons.Length > 0 &&
            optionButtons[0] != null &&
            optionButtons[0].gameObject.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(optionButtons[0].gameObject);
        }
    }

    public void SubmitAnswer(int index)
    {
        if (index < 0 || index >= currentOptions.Count)
        {
            Debug.LogWarning("WordQuizManager: 提交的选项 index 越界。");
            return;
        }

        if (_answeredThisQuestion) return;
        _answeredThisQuestion = true;

        var chosen = currentOptions[index];
        bool isCorrect = (chosen == currentCorrectWord);

        // ✅ 让按钮位置播 Burst / Punch / UI 抖动
        feedbackFX?.PlaySubmitFX(index, isCorrect);

        // ✅ 你的相机抖动（已验证可用）
        if (isCorrect)
        {
            ScreenShake2D.Instance?.Shake(duration: 0.10f, amplitude: 0.22f, frequency: 35f);
            OnAnswerCorrect?.Invoke();
        }
        else
        {
            ScreenShake2D.Instance?.Shake(duration: 0.06f, amplitude: 0.14f, frequency: 45f);
            OnAnswerWrong?.Invoke();
        }

        StartCoroutine(NextQuestionAfterDelay());
    }


    private void PlayBurst(Animator animator)
    {
        if (!animator) return;

        // 关键：强制从头播，避免 Trigger/Transition 没切过去导致“不触发”
        animator.Rebind(); // 可选但很稳（有时会清理掉上一帧残留）
        animator.Update(0f);
        animator.Play(vfxBurstStateName, 0, 0f);
    }

    private System.Collections.IEnumerator NextQuestionAfterDelay()
    {
        if (nextQuestionDelay > 0f)
            yield return new WaitForSeconds(nextQuestionDelay);
        else
            yield return null;

        GenerateNewQuestion();
    }

    public void PlayCurrentWordAudio()
    {
        if (audioSource == null || currentCorrectWord == null || currentCorrectWord.audioClip == null) return;

        audioSource.clip = currentCorrectWord.audioClip;
        audioSource.Play();
    }

    private void Shuffle<T>(IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
