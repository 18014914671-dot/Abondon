using System;
using System.Collections;
using TMPro;
using UnityEngine;

public interface IActiveTypingChallenge
{
    // 返回 true = 这次输入被 challenge 消费了（无论成败），外层不再继续走 Boss/普通逻辑
    bool TryConsumeInput(string normalizedLettersOnly);
}

public class TypingInputPanel : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField inputField;
    public RectTransform inputRootToShake;

    [Header("Refs")]
    public WeaponController weaponController;
    public Transform playerTransform;

    [Header("Shake")]
    public float shakeDuration = 0.12f;
    public float shakeStrength = 10f;

    // ---------------- Boss Mode ----------------
    private BossController _boss;
    private BossUIController _bossUI;
    private Action _onBossDead;

    // ---------------- Challenge Mode (唯一输入出口抢占) ----------------
    private IActiveTypingChallenge _activeChallenge;
    private UnityEngine.Object _activeOwner;

    private Vector2 _originalAnchoredPos;
    private Coroutine _shakeCo;

    private void Awake()
    {
        if (inputRootToShake != null)
            _originalAnchoredPos = inputRootToShake.anchoredPosition;

        if (playerTransform == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (inputField != null)
        {
            inputField.onSubmit.RemoveAllListeners();
            inputField.onSubmit.AddListener(OnSubmit);
        }
    }

    private void OnEnable()
    {
        if (inputField != null)
        {
            inputField.Select();
            inputField.ActivateInputField();
        }
    }

    // ✅ 给 LevelBossFlowController 调用
    public void SetBoss(BossController boss, BossUIController bossUI, Action onBossDead)
    {
        _boss = boss;
        _bossUI = bossUI;
        _onBossDead = onBossDead;
    }

    public void ClearBoss()
    {
        _boss = null;
        _bossUI = null;
        _onBossDead = null;
    }

    // ---------- Challenge API ----------
    public void SetActiveChallenge(IActiveTypingChallenge challenge, UnityEngine.Object owner = null)
    {
        _activeChallenge = challenge;
        _activeOwner = owner;
    }

    public void ClearActiveChallenge(IActiveTypingChallenge challenge)
    {
        if (_activeChallenge == challenge)
        {
            _activeChallenge = null;
            _activeOwner = null;
        }
    }

    public void ClearActiveChallengeByOwner(UnityEngine.Object owner)
    {
        if (_activeOwner == owner)
        {
            _activeChallenge = null;
            _activeOwner = null;
        }
    }

    private void OnSubmit(string rawText)
    {
        if (inputField == null) return;

        string normalized = TypingTextUtil.NormalizeLettersOnly(rawText);

        // ✅ 最高优先级：Challenge（炸弹 / 弱点）
        if (_activeChallenge != null)
        {
            bool consumed = _activeChallenge.TryConsumeInput(normalized);
            ClearInput();
            if (!consumed)
            {
                FailFeedback();
            }
            return;
        }

        // ✅ Boss 优先
        if (_boss != null)
        {
            Debug.Log($"[Typing] Boss match! input={normalized}, bossWord={_boss.CurrentWordText}, phase={_boss.GetPhase()} active={_boss.isActive}");
            HandleBossSubmit(normalized);
            return;
        }

        // -------- 普通打字怪模式 --------
        if (string.IsNullOrEmpty(normalized))
        {
            FailFeedback();
            ClearInput();
            return;
        }

        TypingEnemy target = FindNearestMatchedTypingEnemy(normalized);
        if (target == null)
        {
            FailFeedback();
            ClearInput();
            return;
        }

        if (weaponController != null)
        {
            weaponController.FireVfxAtTarget(target.transform);
        }

        EnemyHealth hp = target.GetComponent<EnemyHealth>();
        if (hp != null) hp.TakeDamage(999);
        else Destroy(target.gameObject);

        ClearInput();
    }

    private void HandleBossSubmit(string normalized)
    {
        if (string.IsNullOrEmpty(normalized))
        {
            FailFeedback();
            ClearInput();
            return;
        }

        // Boss 匹配成功 -> 走 BossController.OnCorrectHit()（它现在会判断 vulnerable 才扣血）
        if (_boss.IsMatch(normalized))
        {
            if (weaponController != null) weaponController.FireVfxAtTarget(_boss.transform);

            _boss.OnCorrectHit();

            if (_bossUI != null) _bossUI.RefreshFromBoss();
        }
        else
        {
            FailFeedback();
        }

        ClearInput();
    }

    private TypingEnemy FindNearestMatchedTypingEnemy(string normalized)
    {
        var enemies = GameObject.FindObjectsOfType<TypingEnemy>();
        if (enemies == null || enemies.Length == 0) return null;

        TypingEnemy best = null;
        float bestDist = float.MaxValue;
        Vector3 origin = playerTransform != null ? playerTransform.position : Vector3.zero;

        foreach (var e in enemies)
        {
            if (e == null) continue;
            if (!e.IsMatch(normalized)) continue;

            float d = (e.transform.position - origin).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = e;
            }
        }

        return best;
    }

    private void FailFeedback()
    {
        if (inputRootToShake == null) return;
        if (_shakeCo != null) StopCoroutine(_shakeCo);
        _shakeCo = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float x = UnityEngine.Random.Range(-1f, 1f) * shakeStrength;
            inputRootToShake.anchoredPosition = _originalAnchoredPos + new Vector2(x, 0f);
            yield return null;
        }
        inputRootToShake.anchoredPosition = _originalAnchoredPos;
        _shakeCo = null;
    }

    private void ClearInput()
    {
        if (inputField == null) return;
        inputField.text = "";
        inputField.ActivateInputField();
        inputField.Select();
    }
}
