using System.Collections;
using UnityEngine;

public class LevelBossFlowController : MonoBehaviour
{
    [Header("Spawners (2 of them)")]
    public EnemySpawner typingEnemySpawner;
    public EnemySpawner voiceEnemySpawner;

    [Header("Wave Counts (by total spawn numbers)")]
    public int wave1Count = 12;
    public int wave2Count = 16;
    public int wave3Count = 20;

    [Tooltip("每一波：打字怪占比（0-1）。例如 0.5 表示一半打字、一半语音")]
    [Range(0f, 1f)] public float typingRatio = 0.5f;

    [Header("Boss")]
    public GameObject bossPrefab;
    public Transform bossSpawnPoint; // 不填就默认相机上方一点
    public WordLibrary bossWordLibrary;

    [Header("Refs")]
    public Camera mainCamera;
    public WeaponController weaponController;
    public TypingInputPanel typingInputPanel;

    [Header("UI")]
    public BossUIController bossUI;
    public LevelClearPanelController levelClearPanel;

    [Header("VFX")]
    public CameraShake2D cameraShake;

    private BossController _boss;
    private bool _bossPhase;

    private void Start()
    {
        // 让 spawner 不要自己无限刷（你现在 Inspector 有 autoStart 勾选的话，这里强制关）
        if (typingEnemySpawner != null) typingEnemySpawner.autoStart = false;
        if (voiceEnemySpawner != null) voiceEnemySpawner.autoStart = false;

        if (bossUI != null) bossUI.SetVisible(false);
        if (levelClearPanel != null) levelClearPanel.Hide();

        StartCoroutine(RunFlow());
    }

    private IEnumerator RunFlow()
    {
        yield return RunWave(wave1Count);
        yield return RunWave(wave2Count);
        yield return RunWave(wave3Count);

        // ✅ 三波结束 → 清场确认 → 震动 → 出 Boss
        yield return WaitUntilNoEnemyAlive();

        if (cameraShake != null) cameraShake.Shake();
        yield return new WaitForSeconds(0.35f);

        EnterBossPhase();
    }

    private IEnumerator RunWave(int totalCount)
    {
        // 刷怪：按数量，同时尊重 maxAlive（场上太多就等）
        int typingTarget = Mathf.RoundToInt(totalCount * typingRatio);
        int voiceTarget = totalCount - typingTarget;

        yield return SpawnByCount(typingEnemySpawner, typingTarget);
        yield return SpawnByCount(voiceEnemySpawner, voiceTarget);

        // 等这一波被清完
        yield return WaitUntilNoEnemyAlive();
    }

    private IEnumerator SpawnByCount(EnemySpawner spawner, int count)
    {
        if (spawner == null || count <= 0) yield break;

        for (int i = 0; i < count; i++)
        {
            // 等场上敌人低于 maxAlive 再刷（你 spawner 的 maxAlive 逻辑是 Update 里用的，我们这里自己尊重一下）
            while (GetAliveEnemyCount() >= spawner.maxAlive)
                yield return null;

            spawner.SpawnOneNow();
            yield return new WaitForSeconds(spawner.spawnInterval);
        }
    }

    private int GetAliveEnemyCount()
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        return enemies != null ? enemies.Length : 0;
    }

    private IEnumerator WaitUntilNoEnemyAlive()
    {
        while (GetAliveEnemyCount() > 0)
            yield return null;
    }

    private void EnterBossPhase()
    {
        _bossPhase = true;

        // ✅ Boss 阶段不要刷小怪
        if (typingEnemySpawner != null) typingEnemySpawner.autoStart = false;
        if (voiceEnemySpawner != null) voiceEnemySpawner.autoStart = false;

        // ✅ 刷 Boss
        Vector3 spawnPos = GetBossSpawnPos();
        GameObject go = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
        go.tag = "Enemy";

        _boss = go.GetComponent<BossController>();
        if (_boss == null) _boss = go.AddComponent<BossController>();

        _boss.wordLibrary = bossWordLibrary;
        _boss.ActivateBoss();

        // ✅ UI 出现
        if (bossUI != null)
        {
            bossUI.BindBoss(_boss);
            bossUI.SetVisible(true);
        }

        // ✅ 告诉打字面板：Boss 优先
        if (typingInputPanel != null)
        {
            typingInputPanel.SetBoss(_boss, bossUI, OnBossDead);
        }

        // 监听 Boss 是否死亡：最简方式是轮询（因为 EnemyHealth 死亡会 Destroy）
        StartCoroutine(WatchBossDead());
    }

    private Vector3 GetBossSpawnPos()
    {
        if (bossSpawnPoint != null) return bossSpawnPoint.position;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return Vector3.zero;

        float halfH = mainCamera.orthographicSize;
        float y = mainCamera.transform.position.y + halfH - 1.0f;
        float x = mainCamera.transform.position.x;
        return new Vector3(x, y, 0f);
    }

    private IEnumerator WatchBossDead()
    {
        while (_boss != null)
            yield return null;

        // Boss 已被 Destroy
        OnBossDead();
    }

    private void OnBossDead()
    {
        if (!_bossPhase) return;
        _bossPhase = false;

        if (bossUI != null) bossUI.SetVisible(false);

        if (levelClearPanel != null)
            levelClearPanel.Show();
    }
}
