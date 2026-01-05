using UnityEngine;

[DisallowMultipleComponent]
public class PlayerDamageFeedback : MonoBehaviour
{
    [Header("Refs")]
    public PlayerHealth playerHealth;
    public SpriteRenderer spriteRenderer;

    [Header("Flash")]
    public Color flashColor = Color.white;
    public float flashDuration = 0.08f;

    private Color _orig;
    private float _t;
    private bool _flashing;

    private void Awake()
    {
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null) _orig = spriteRenderer.color;
    }

    private void OnEnable()
    {
        if (playerHealth != null) playerHealth.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.OnDamaged -= HandleDamaged;
    }

    private void Update()
    {
        if (!_flashing || spriteRenderer == null) return;

        _t += Time.unscaledDeltaTime;
        if (_t >= flashDuration)
        {
            spriteRenderer.color = _orig;
            _flashing = false;
        }
    }

    private void HandleDamaged()
    {
        if (spriteRenderer == null) return;
        _t = 0f;
        _flashing = true;
        spriteRenderer.color = flashColor;
    }
}
