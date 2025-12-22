using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2D 正交相机刷怪：在相机上边界之上生成，支持多个 Prefab、最大存活数量、可选父物体收纳。
/// 放置：Assets/Scripts/Systems/Spawning/EnemySpawner.cs
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn")]
    [Tooltip("至少拖 1 个 prefab 进来，否则不会刷怪")]
    public List<GameObject> enemyPrefabs = new List<GameObject>();

    [Tooltip("刷怪间隔（秒）")]
    public float spawnInterval = 1f;

    [Tooltip("场上最多同时存活数量（用 spawnedParent 子物体数量统计；没填 parent 就用全场 Enemy 数量统计）")]
    public int maxAlive = 12;

    [Tooltip("生成点在相机上边界再往上偏移多少")]
    public float spawnYOffSet = 1f;

    [Tooltip("左右边界留边（防止刷在边缘外）")]
    public float xPadding = 0.5f;

    [Header("References")]
    public Camera targetCamera;
    [Tooltip("可选：用于收纳刷出来的敌人，建议创建一个空物体 Enemies 拖进来")]
    public Transform spawnedParent;

    [Header("Debug")]
    public bool autoStart = true;
    public bool logSpawn = false;

    private float _timer;

    private void Start()
    {
        if (!targetCamera) targetCamera = Camera.main;
        if (autoStart) _timer = spawnInterval; // 让它开局就尽快刷一只
    }

    private void Update()
    {
        if (!autoStart) return;
        if (!targetCamera) return;

        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
        {
            // 你现在不刷怪的最常见原因就在这：enemyPrefabs 空的
            return;
        }

        if (GetAliveCount() >= maxAlive) return;

        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            SpawnOne();
        }
    }

    private int GetAliveCount()
    {
        if (spawnedParent != null)
        {
            // 只统计 parent 下的（最稳定）
            int count = 0;
            for (int i = 0; i < spawnedParent.childCount; i++)
            {
                var c = spawnedParent.GetChild(i);
                if (c != null && c.gameObject.activeInHierarchy) count++;
            }
            return count;
        }

        // 没有 parent：退化为统计场景里 Tag=Enemy 的物体（你敌人 prefab 里 Tag=Enemy 就行）
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        return enemies != null ? enemies.Length : 0;
    }

    private void SpawnOne()
    {
        if (!targetCamera) return;

        // 2D 正交相机边界
        float halfH = targetCamera.orthographicSize;
        float halfW = halfH * targetCamera.aspect;

        float leftX = targetCamera.transform.position.x - halfW + xPadding;
        float rightX = targetCamera.transform.position.x + halfW - xPadding;
        float topY = targetCamera.transform.position.y + halfH;

        float x = Random.Range(leftX, rightX);
        float y = topY + spawnYOffSet;

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        if (!prefab) return;

        var go = Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity, spawnedParent);

        // 确保每个敌人都有 DespawnBelowCamera（没挂就自动补）
        var despawn = go.GetComponent<DespawnBelowCamera>();
        if (!despawn) despawn = go.AddComponent<DespawnBelowCamera>();
        despawn.targetCamera = targetCamera;

        if (logSpawn) Debug.Log($"[EnemySpawner] Spawned: {go.name} at ({x:F2},{y:F2})");
    }

    // 让你可以在 Inspector 里临时手动刷一只（点齿轮 -> Debug / 或右键脚本）
    [ContextMenu("Spawn One Now")]
    public void SpawnOneNow()
    {
        if (!targetCamera) targetCamera = Camera.main;
        _timer = 0f;
        SpawnOne();
    }
}
