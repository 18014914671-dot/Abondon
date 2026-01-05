using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class WordBombProjectile : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 4.5f;
    public float hitDistance = 0.35f;

    [Header("Damage")]
    public int damage = 1;

    [Header("Ring Timing")]
    public float ringDuration = 1.2f;
    [Range(0f, 1f)] public float windowStart01 = 0.35f;
    [Range(0f, 1f)] public float windowEnd01 = 0.55f;

    [Header("World UI (Optional)")]
    public TMP_Text worldWordText;

    [Header("Director (auto from BossBattleDirector)")]
    public BossBattleDirector director;
    public bool useTimingRing = true;
    [Header("Timing Ring")]
 
    private Transform _player;
    private PlayerHealth _playerHealth;
    private TypingInputPanel _typingInput;
    private TimingRingPanel _ring;
    private WordData _word;

    private bool _armed;
    private bool _done;

    public void SetDirector(BossBattleDirector d) => director = d;

    public void Init(
        Transform player,
        PlayerHealth playerHealth,
        TypingInputPanel typingInput,
        TimingRingPanel ring,
        WordData word)
    {
        _player = player;
        _playerHealth = playerHealth;
        _typingInput = typingInput;
        _ring = ring;
        _word = word;

        if (worldWordText != null && _word != null)
            worldWordText.text = _word.wordText;

        // 抢占唯一输入出口
        if (_typingInput != null)
            _typingInput.SetActiveChallenge(new BombChallenge(this));

        // 启动圆环
        if (_ring != null && _word != null)
            _ring.Show(_word.wordText, ringDuration, windowStart01, windowEnd01);

        _armed = true;
    }

    private void Update()
    {
        if (!_armed || _done) return;
        if (_player == null) return;

        // 飞向玩家
        Vector3 dir = (_player.position - transform.position);
        float dist = dir.magnitude;
        if (dist <= hitDistance)
        {
            ExplodeFail();
            return;
        }

        transform.position += dir.normalized * moveSpeed * Time.deltaTime;

        // 圆环结束仍未成功 => 失败
        if (_ring != null && !_ring.Running)
        {
            ExplodeFail();
        }
    }

    private void ExplodeFail()
    {
        if (_done) return;
        _done = true;

        if (_playerHealth != null)
            _playerHealth.TakeDamage(damage);

        CleanupChallenge();

        // 通知 Director：失败
        if (director != null)
            director.NotifyBombResolved(success: false, perfect: false);

        Destroy(gameObject);
    }

    private void ExplodeSuccess(bool perfect)
    {
        if (_done) return;
        _done = true;

        CleanupChallenge();

        // 通知 Director：成功（且 perfect 由窗口判定）
        if (director != null)
            director.NotifyBombResolved(success: true, perfect: perfect);

        Destroy(gameObject);
    }

    private void CleanupChallenge()
    {
        if (_ring != null) _ring.Hide();
        if (_typingInput != null) _typingInput.ClearActiveChallengeByOwner(this);
    }

    // ---------------- Active Challenge ----------------
    private class BombChallenge : IActiveTypingChallenge
    {
        private readonly WordBombProjectile _owner;

        public BombChallenge(WordBombProjectile owner) { _owner = owner; }

        public bool TryConsumeInput(string normalizedLettersOnly)
        {
            if (_owner == null || _owner._done) return true;
            if (_owner._word == null) return false;

            string want = TypingTextUtil.NormalizeLettersOnly(_owner._word.wordText);
            if (normalizedLettersOnly != want) return false;

            // 单词对，但必须在窗口内
            bool inWindow = (_owner._ring == null) ? true : _owner._ring.IsInWindow();
            if (!inWindow)
            {
                _owner.ExplodeFail();
                return true;
            }

            // 在窗口内输入正确 => 记为 perfect
            _owner.ExplodeSuccess(perfect: true);
            return true;
        }
    }
}
