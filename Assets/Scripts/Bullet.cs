using UnityEngine;

/// <summary>
/// 通用弹道子弹：命中后对目标结算伤害，然后销毁自己。
/// 伤害数值/类型可由武器在生成子弹时写入；默认值用于单独测试。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("伤害(武器生成时会覆盖这些值)")]
    public float damage = 10f;
    public DamageType damageType = DamageType.Projectile;

    private Rigidbody rb;
    private bool hit; // 防止同一帧多次命中

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>由武器调用：赋予方向与速度，之后子弹自行飞行。</summary>
    public void Launch(Vector3 direction, float velocity)
    {
        rb.linearVelocity = direction.normalized * velocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hit) return;
        hit = true;

        var contact = collision.GetContact(0);

        // 命中可受伤对象则结算伤害(玩家/敌人/靶子都实现了 IDamageable)
        var damageable = collision.collider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            Vector3 knockback = rb != null ? rb.linearVelocity.normalized * 1f : Vector3.zero;
            var info = new DamageInfo(damage, damageType, contact.point, contact.normal, knockback, null);
            damageable.TakeDamage(info);
        }

        // 命中任何东西都销毁(墙体可留弹孔，留待以后)
        Destroy(gameObject);
    }
}
