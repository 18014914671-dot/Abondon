using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HitFlash2D : MonoBehaviour
{
    [Header("闪白持续时间")]
    public float flashDuration = 0.08f;

    [Header("闪白颜色")]
    public Color flashColor = Color.white;

    private SpriteRenderer sr;
    private Color originalColor;
    private Coroutine flashRoutine;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
    }

    public void Flash()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(DoFlash());
    }

    private IEnumerator DoFlash()
    {
        originalColor = sr.color;
        sr.color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        sr.color = originalColor;
        flashRoutine = null;
    }
}
