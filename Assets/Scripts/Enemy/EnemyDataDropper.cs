using UnityEngine;

/// <summary>
/// 敌人死亡时随机掉落一个 DataItem，并实例化 DataPickup 预制体。
/// 掉落是否成功由 dropChance 决定，可能重复掉落同一个 DataItem。
/// </summary>
public class EnemyDataDropper : MonoBehaviour
{
    [Header("Drop Settings")]
    [Range(0f, 1f)]
    public float dropChance = 0.3f;          // 掉落几率（0~1）

    [Tooltip("用于实例化的 DataPickup 预制体（Prefab 上必须挂 DataPickup 脚本）")]
    public GameObject dataPickupPrefab;

    [Tooltip("本关可能掉落的所有 DataItem（可重复）")]
    public DataItem[] possibleDrops;

    /// <summary>
    /// 敌人死亡时由 EnemyHealth 调用。
    /// </summary>
    public void TryDropData()
    {
        if (dataPickupPrefab == null)
        {
            Debug.LogWarning("[EnemyDataDropper] dataPickupPrefab 没有设置。");
            return;
        }

        if (possibleDrops == null || possibleDrops.Length == 0)
        {
            Debug.LogWarning("[EnemyDataDropper] possibleDrops 为空，不会掉任何东西。");
            return;
        }

        // 掉落判定
        if (Random.value > dropChance)
            return;

        // 随机选一个 DataItem
        DataItem picked = possibleDrops[Random.Range(0, possibleDrops.Length)];
        if (picked == null)
        {
            Debug.LogWarning("[EnemyDataDropper] possibleDrops 中存在空项（null）。");
            return;
        }

        // 生成拾取物
        GameObject pickupObj = Instantiate(
            dataPickupPrefab,
            transform.position,
            Quaternion.identity
        );

        // 把 DataItem 填给 DataPickup 脚本
        DataPickup pickup = pickupObj.GetComponent<DataPickup>();
        if (pickup == null)
        {
            Debug.LogError("[EnemyDataDropper] dataPickupPrefab 上没有 DataPickup 组件！");
            return;
        }

        pickup.dataItem = picked; // ✅ 新字段名：dataItem
    }
}
