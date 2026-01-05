using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHP = 5;
    [SerializeField] private int currentHP;

    [Header("I-Frames")]
    public float invincibleTime = 0.35f;

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public bool IsDead => currentHP <= 0;

    public event Action<int, int> OnHPChanged;   // (cur, max)
    public event Action OnDamaged;
    public event Action OnDead;

    private float _invTimer;

    private void Awake()
    {
        currentHP = Mathf.Clamp(currentHP <= 0 ? maxHP : currentHP, 0, maxHP);
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    private void Update()
    {
        if (_invTimer > 0f) _invTimer -= Time.deltaTime;
    }

    public void ResetHP()
    {
        currentHP = maxHP;
        _invTimer = 0f;
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        if (IsDead) return;
        if (_invTimer > 0f) return;

        currentHP = Mathf.Clamp(currentHP - amount, 0, maxHP);
        _invTimer = invincibleTime;

        OnHPChanged?.Invoke(currentHP, maxHP);
        OnDamaged?.Invoke();

        if (currentHP <= 0)
        {
            OnDead?.Invoke();
        }
    }
}
