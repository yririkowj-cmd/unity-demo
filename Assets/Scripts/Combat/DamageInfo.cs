using UnityEngine;

/// <summary>
/// 伤害类型：区分不同武器的伤害表现，方便以后做抗性/护甲。
/// </summary>
public enum DamageType
{
    Projectile,  // 弹道子弹(手枪/步枪)
    Hitscan,     // 射线(枪械/光炮)
    Throwable,   // 投掷(飞刀)
    Explosion,   // 爆炸(预留)
    Melee        // 近战(预留)
}

/// <summary>
/// 一次伤害的完整描述。所有可受伤对象(玩家/敌人/靶子)都通过它接收伤害。
/// </summary>
public readonly struct DamageInfo
{
    public readonly float amount;
    public readonly DamageType type;
    public readonly Vector3 point;      // 命中点
    public readonly Vector3 normal;     // 命中面法线
    public readonly Vector3 knockback;  // 击退方向与力度
    public readonly GameObject source;  // 谁造成的伤害(玩家/敌人)

    public DamageInfo(float amount, DamageType type, Vector3 point, Vector3 normal,
                      Vector3 knockback, GameObject source)
    {
        this.amount = amount;
        this.type = type;
        this.point = point;
        this.normal = normal;
        this.knockback = knockback;
        this.source = source;
    }
}
