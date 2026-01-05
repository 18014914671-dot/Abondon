using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Refs")]
    public Transform firePoint;
    public GameObject bulletPrefab;

    [Header("Bullet Settings")]
    public float bulletSpeed = 15f;

    [Header("Double Shot")]
    public float doubleShotAngleOffset = 10f;

    [Header("Spread Shot")]
    public int spreadShotBulletCount = 8;
    public float spreadShotTotalAngle = 60f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shootSFX;

    // =====================
    // ✅ 对外接口（【不要删】）
    // =====================

    public void FireAtNearestEnemy()
    {
        FireSingleShot();
    }

    public void FireSingleShot()
    {
        Transform target = FindNearestEnemy();
        if (target == null) return;

        Vector2 dir = (target.position - firePoint.position).normalized;
        SpawnBullet(dir, vfxOnly: false);
    }

    public void FireDoubleShot()
    {
        Transform target = FindNearestEnemy();
        if (target == null)
        {
            FireSingleShot();
            return;
        }

        Vector2 baseDir = (target.position - firePoint.position).normalized;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        float half = doubleShotAngleOffset * 0.5f;
        SpawnBullet(AngleToDir(baseAngle - half), false);
        SpawnBullet(AngleToDir(baseAngle + half), false);
    }

    public void FireSpreadShot()
    {
        Transform target = FindNearestEnemy();
        if (target == null)
        {
            FireSingleShot();
            return;
        }

        if (spreadShotBulletCount <= 1)
        {
            FireSingleShot();
            return;
        }

        Vector2 baseDir = (target.position - firePoint.position).normalized;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        float half = spreadShotTotalAngle * 0.5f;
        float step = spreadShotTotalAngle / (spreadShotBulletCount - 1);

        for (int i = 0; i < spreadShotBulletCount; i++)
        {
            float ang = baseAngle - half + step * i;
            SpawnBullet(AngleToDir(ang), false);
        }
    }

    // ✅ 给 Typing / Boss 用的“纯表现子弹”
    public void FireVfxAtTarget(Transform target)
    {
        if (target == null || firePoint == null) return;

        Vector2 dir = (target.position - firePoint.position).normalized;
        SpawnBullet(dir, vfxOnly: true);
    }

    // =====================
    // 内部实现（安全区）
    // =====================

    private Transform FindNearestEnemy()
    {
        if (firePoint == null) return null;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies == null || enemies.Length == 0) return null;

        Transform nearest = null;
        float minSqr = float.MaxValue;
        Vector3 origin = firePoint.position;

        foreach (var e in enemies)
        {
            if (e == null) continue;
            float d = (e.transform.position - origin).sqrMagnitude;
            if (d < minSqr)
            {
                minSqr = d;
                nearest = e.transform;
            }
        }

        return nearest;
    }

    private void SpawnBullet(Vector2 dir, bool vfxOnly)
    {
        if (firePoint == null || bulletPrefab == null) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        // ✅ vfxOnly：双保险，绝不造成二次伤害
        if (vfxOnly)
        {
            var col = bullet.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            var b = bullet.GetComponent<Bullet>();
            if (b != null) b.dealDamage = false;
        }

        var bulletComp = bullet.GetComponent<Bullet>();
        if (bulletComp != null)
            bulletComp.Init(dir.normalized, bulletSpeed);
            bulletComp.dealDamage = !vfxOnly; // ✅ 关键：表现子弹不造成伤害

        if (audioSource != null && shootSFX != null)
            audioSource.PlayOneShot(shootSFX);
    }

    private Vector2 AngleToDir(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
}
