using UnityEngine;

/// <summary>
/// 可受伤对象接口。玩家 / 敌人 / 可破坏物 / 靶子都实现它。
/// 以后做 PVP/PVE 时，命中只调用 TakeDamage，无需知道对方具体是谁。
/// </summary>
public interface IDamageable
{
    bool TakeDamage(DamageInfo info);
}
