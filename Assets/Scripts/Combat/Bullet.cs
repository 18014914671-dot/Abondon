using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float lifeTime = 3f;   // 子弹存在时间（秒）

    [Header("Damage")]
    public bool dealDamage = true; // ✅ 新增：是否造成伤害（表现子弹 = false）

    private Vector2 direction;
    private float speed;

    public void Init(Vector2 dir, float spd)
    {
        direction = dir.normalized;
        speed = spd;
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!dealDamage) return;

        if (other.CompareTag("Enemy"))
        {
            EnemyHealth hp = other.GetComponent<EnemyHealth>();
            if (hp != null)
            {
                hp.TakeDamage(1);
            }

            Destroy(gameObject);
        }
    }
}
