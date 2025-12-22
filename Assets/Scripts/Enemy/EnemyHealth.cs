using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 1;
    private int currentHealth;

    [Header("Hit Flash")]
    public SpriteRenderer spriteRenderer;
    public Color hitColor = Color.white;
    public float flashDuration = 0.08f;

    private Color originalColor;
    private Coroutine flashCoroutine;

    [Header("Audio")]
    public AudioClip deathSFX;
    public AudioSource audioSource;

    // ✅ 新增：防止 Die() 被调用两次
    private bool _isDead;

    void Awake()
    {
        currentHealth = maxHealth;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void TakeDamage(int amount)
    {
        // ✅ 已经死了就不再处理任何伤害（避免重复 Die）
        if (_isDead) return;

        currentHealth -= amount;

        if (spriteRenderer != null)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(HitFlash());
        }

        if (currentHealth <= 0)
        {
            _isDead = true;   // ✅ 关键：先锁死，再 Die
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
        // ① 死亡前先尝试掉落 Data
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

        // ③ 计入击杀（你的剧情系统）
        StoryProgressTracker.Instance?.AddKill(1);

        // ④ 销毁敌人
        Destroy(gameObject);
    }
}
