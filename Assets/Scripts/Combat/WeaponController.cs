using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Refs")]
    public Transform firePoint;           // 子弹发射位置
    public GameObject bulletPrefab;       // 子弹 Prefab

    [Header("Bullet Settings")]
    public float bulletSpeed = 15f;       // 子弹速度

    [Header("Double Shot 设置")]
    [Tooltip("双发子弹左右张开的角度（度）")]
    public float doubleShotAngleOffset = 10f;

    [Header("Spread Shot 设置")]
    [Tooltip("范围弹幕一共发多少颗子弹")]
    public int spreadShotBulletCount = 8;
    [Tooltip("范围弹幕总张角（度），中心对准最近敌人")]
    public float spreadShotTotalAngle = 60f;

    [Header("Audio")]
    public AudioSource audioSource;       // 播放射击音效的 AudioSource（挂在玩家身上即可）
    public AudioClip shootSFX;            // 射击音效

    // ----------------- 对外接口（给 ComboManager / Quiz 调用） -----------------

    /// <summary>
    /// 旧接口：单发射击，向最近敌人。为了兼容旧代码保留。
    /// </summary>
    public void FireAtNearestEnemy()
    {
        FireSingleShot();
    }

    /// <summary>
    /// 单发射击（基础形态）
    /// </summary>
    public void FireSingleShot()
    {
        Transform target = FindNearestEnemy();
        if (target == null)
        {
            Debug.Log("WeaponController: 场景中没有敌人，SingleShot 取消。");
            return;
        }

        Vector2 dir = (target.position - firePoint.position).normalized;
        SpawnBullet(dir);
    }

    /// <summary>
    /// 双发射击：向最近敌人方向，两发略微张开
    /// </summary>
    public void FireDoubleShot()
    {
        Transform target = FindNearestEnemy();
        if (target == null)
        {
            Debug.Log("WeaponController: 场景中没有敌人，DoubleShot 降级为单发。");
            FireSingleShot();
            return;
        }

        Vector2 baseDir = (target.position - firePoint.position).normalized;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        // 两颗子弹左右对称
        float halfOffset = doubleShotAngleOffset * 0.5f;
        float a1 = baseAngle - halfOffset;
        float a2 = baseAngle + halfOffset;

        Vector2 dir1 = AngleToDir(a1);
        Vector2 dir2 = AngleToDir(a2);

        SpawnBullet(dir1);
        SpawnBullet(dir2);
    }

    /// <summary>
    /// 范围弹幕：围绕最近敌人的方向扇形撒开
    /// </summary>
    public void FireSpreadShot()
    {
        Transform target = FindNearestEnemy();
        if (target == null)
        {
            Debug.Log("WeaponController: 场景中没有敌人，SpreadShot 降级为单发。");
            FireSingleShot();
            return;
        }

        if (spreadShotBulletCount <= 1)
        {
            // 退化为单发
            FireSingleShot();
            return;
        }

        Vector2 baseDir = (target.position - firePoint.position).normalized;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        float halfTotal = spreadShotTotalAngle * 0.5f;
        float startAngle = baseAngle - halfTotal;
        float step = spreadShotTotalAngle / (spreadShotBulletCount - 1);

        for (int i = 0; i < spreadShotBulletCount; i++)
        {
            float ang = startAngle + step * i;
            Vector2 dir = AngleToDir(ang);
            SpawnBullet(dir);
        }
    }

    // ----------------- 内部工具函数 -----------------

    /// <summary>
    /// 找到场景中最近的敌人（要求敌人 Tag = "Enemy"）
    /// </summary>
    private Transform FindNearestEnemy()
    {
        if (firePoint == null)
        {
            Debug.LogWarning("WeaponController: firePoint 没有设置！");
            return null;
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0)
        {
            return null;
        }

        Transform nearest = null;
        float minSqrDist = float.MaxValue;
        Vector3 origin = firePoint.position;

        foreach (var e in enemies)
        {
            float d = (e.transform.position - origin).sqrMagnitude;
            if (d < minSqrDist)
            {
                minSqrDist = d;
                nearest = e.transform;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 根据方向向量生成一颗子弹
    /// </summary>
    private void SpawnBullet(Vector2 dir)
    {
        if (firePoint == null || bulletPrefab == null)
        {
            Debug.LogWarning("WeaponController: firePoint 或 bulletPrefab 没有设置！");
            return;
        }

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        Bullet bulletComp = bullet.GetComponent<Bullet>();
        if (bulletComp != null)
        {
            bulletComp.Init(dir.normalized, bulletSpeed);
        }

        // 播放射击音效
        if (audioSource != null && shootSFX != null)
        {
            audioSource.PlayOneShot(shootSFX);
        }
    }

    /// <summary>
    /// 角度(度) → 单位方向向量
    /// </summary>
    private Vector2 AngleToDir(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
}
