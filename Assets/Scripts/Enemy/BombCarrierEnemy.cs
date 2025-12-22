using UnityEngine;
using TMPro;
using System.Text;

public class BombCarrierEnemy : MonoBehaviour
{
    [Header("Word Bomb")]
    public WordLibrary wordLibrary;
    public WordData bombWord;

    [Header("UI (optional)")]
    public TMP_Text bombWordText;
    public string prefix = "";

    [Header("Explosion")]
    public float explosionRadius = 2.8f;
    public int explosionDamage = 1;
    public LayerMask enemyLayerMask;
    public bool killSelfOnExplode = true;

    [Header("Optional VFX")]
    public GameObject explosionVFXPrefab;

    private void OnEnable()
    {
        AssignRandomWordIfNeeded();
        RefreshText();
    }

    public void AssignRandomWordIfNeeded()
    {
        if (bombWord != null) return;
        if (wordLibrary == null) return;

        bombWord = wordLibrary.GetRandomWord();
    }

    public void RefreshText()
    {
        if (bombWordText == null) return;

        if (bombWord == null || string.IsNullOrEmpty(bombWord.wordText))
            bombWordText.text = "";
        else
            bombWordText.text = prefix + bombWord.wordText;
    }

    /// <summary>
    /// 给 Manager 用：判断识别到的词是否匹配这个炸弹的词
    /// </summary>
    public bool IsWordMatch(string normalizedSpeechWord)
    {
        if (bombWord == null || string.IsNullOrWhiteSpace(bombWord.wordText)) return false;

        string my = NormalizeWord(bombWord.wordText);
        return my == normalizedSpeechWord;
    }

    /// <summary>
    /// 统一 Normalize 规则：小写 + 去空格 + 去标点
    /// </summary>
    public static string NormalizeWord(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";

        s = s.Trim().ToLowerInvariant();

        // 去掉空格与常见标点（防止 whisper 输出 "canada." 这种）
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsWhiteSpace(c)) continue;
            if (char.IsPunctuation(c)) continue;
            sb.Append(c);
        }
        return sb.ToString();
    }

    public void TriggerBomb()
    {
        Explode();
    }

    private void Explode()
    {
        if (explosionVFXPrefab != null)
            Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, enemyLayerMask);
        foreach (var h in hits)
        {
            if (h == null) continue;
            if (h.transform == transform) continue;

            var enemyHealth = h.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
                enemyHealth.TakeDamage(explosionDamage);
        }

        if (killSelfOnExplode)
        {
            var selfHealth = GetComponent<EnemyHealth>();
            if (selfHealth != null) selfHealth.TakeDamage(9999);
            else Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
