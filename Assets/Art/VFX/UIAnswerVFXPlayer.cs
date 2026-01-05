using UnityEngine;

public class UIAnswerVFXPlayer : MonoBehaviour
{
    [Header("Animator on this object")]
    public Animator animator;

    [Tooltip("Animator 里用来播放的 Trigger 名")]
    public string playTrigger = "Play";

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
    }

    public void Play()
    {
        if (!animator) return;

        // 关键：强制重新播（避免上一次还在同一个状态导致不进过渡）
        gameObject.SetActive(true);
        animator.Rebind();
        animator.Update(0f);

        animator.ResetTrigger(playTrigger);
        animator.SetTrigger(playTrigger);
    }
}
