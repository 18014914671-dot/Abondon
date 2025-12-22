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

    [Header("UI：三个选项按钮")]
    public Button[] optionButtons;          // 0,1,2

    [Header("UI：按钮上的文字")]
    public TMP_Text[] optionTexts;          // 0,1,2

    [Header("Hotkeys")]
    public bool allowHotkeys = true;

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

        // ✅ 这里直接调用 SubmitAnswer，最稳定（不靠 EventSystem submit 防止双触发）
        SubmitAnswer(index);

        // 如果你非常在意 SpriteSwap 的 Pressed 状态，可以保留下面两行“选中”：
        // if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(btn.gameObject);
    }

    public void GenerateNewQuestion()
    {
        // ✅ 每次新题都必须重置锁
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

        if (EventSystem.current != null && optionButtons.Length > 0 && optionButtons[0] != null && optionButtons[0].gameObject.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(optionButtons[0].gameObject);
    }

    public void SubmitAnswer(int index)
    {
        // ✅ 先检查 index，再上锁（避免越界把锁锁死）
        if (index < 0 || index >= currentOptions.Count)
        {
            Debug.LogWarning("WordQuizManager: 提交的选项 index 越界。");
            return;
        }

        if (_answeredThisQuestion) return;
        _answeredThisQuestion = true;

        var chosen = currentOptions[index];
        bool isCorrect = (chosen == currentCorrectWord);

        if (isCorrect)
        {
            OnAnswerCorrect?.Invoke(); // ✅ 开火交给 ComboManager
        }
        else
        {
            OnAnswerWrong?.Invoke();
        }

        // ✅ 下一帧再出题：避免同帧 UI/输入系统重复触发造成的怪现象
        StartCoroutine(NextQuestionNextFrame());
    }

    private System.Collections.IEnumerator NextQuestionNextFrame()
    {
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
