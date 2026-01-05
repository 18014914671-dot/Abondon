// Assets/Scripts/Combat/Health/EnemyHealth.cs
using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 1;
    [SerializeField] private int currentHealth;

    [Header("Hit Flash")]
    public SpriteRenderer spriteRenderer;
    public Color hitColor = Color.white;
    public float flashDuration = 0.08f;

    private Color originalColor;
    private Coroutine flashCoroutine;

    [Header("Audio")]
    public AudioClip deathSFX;
    public AudioSource audioSource;

    // 防止 Die() 被调用两次
    private bool _isDead;

    // ✅ 给外部读取真实血量（UI 用）
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => _isDead;

    /// <summary>
    /// ✅ UI/系统可订阅：血量变化（包括 ResetHealth）
    /// (current, max)
    /// </summary>
    public event Action<int, int> OnHealthChanged;

    /// <summary>
    /// ✅ 死亡事件（给 Boss 或任务系统用）
    /// </summary>
    public event Action OnDied;

    void Awake()
    {
        currentHealth = maxHealth;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        // 通知一次初始值（方便 UI 自动同步）
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// ✅ Boss 出现/复活/重置时用：设置最大血量并重置当前血量
    /// </summary>
    public void ResetHealth(int newMaxHealth)
    {
        _isDead = false;
        maxHealth = Mathf.Max(1, newMaxHealth);
        currentHealth = maxHealth;

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = null;

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// ✅ 通用扣血入口：小怪直接扣；Boss 可由 DamageFilter 决定是否允许扣血
    /// source 参数可不填（以后你要做“不同武器/不同来源免疫”时非常有用）
    /// </summary>
    public void TakeDamage(int amount, object source = null)
    {
        if (_isDead) return;
        if (amount <= 0) return;

        // ✅ 方案A关键：如果身上有 IDamageFilter，则先问它“能不能掉血”
        // 没有 filter => 小怪行为完全不变
        var filter = GetComponent<IDamageFilter>();
        if (filter != null && !filter.CanTakeDamage(amount, source))
        {
            // 可选：你也可以在这里做“格挡火花”之类效果
            return;
        }

        currentHealth -= amount;

        if (spriteRenderer != null)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(HitFlash());
        }

        // 通知 UI
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            _isDead = true;

            // 再通知一次归零（保险）
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            Die();
        }
    }

    private System.Collections.IEnumerator HitFlash()
    {
        spriteRenderer.color = hitColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
        flashCoroutine = null;
    }

    void Die()
    {
        // ① 死亡前掉落
        EnemyDataDropper dropper = GetComponent<EnemyDataDropper>();
        if (dropper != null)
        {
            dropper.TryDropData();
        }

        // ② 播放死亡音效
        if (deathSFX != null)
        {
            GameObject sfxObj = new GameObject("EnemyDeathSFX");
            AudioSource sfxAudio = sfxObj.AddComponent<AudioSource>();
            sfxAudio.clip = deathSFX;
            sfxAudio.Play();
            Destroy(sfxObj, deathSFX.length);
        }

        // ③ 击杀计数
        StoryProgressTracker.Instance?.AddKill(1);

        // ④ 通知外部（Boss 死亡逻辑/任务等）
        OnDied?.Invoke();

        // ⑤ 销毁
        Destroy(gameObject);
    }
}
