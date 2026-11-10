using UnityEngine;

/// <summary>
/// 内置武器行为：射线（光炮/步枪）。命中最近的非自身目标并结算伤害，同时显示激光。
/// </summary>
public sealed class HitscanBehavior : IWeaponBehavior
{
    public const string BehaviorId = "hitscan";
    public string Id => BehaviorId;

    public void Fire(WeaponController host, Vector3 direction)
    {
        Transform muzzle = host.Muzzle;
        if (host.RaycastFromMuzzle(direction, out RaycastHit hit))
        {
            host.Damage(hit.collider, hit.point, hit.normal, direction);
            host.ShowLaser(muzzle.position, hit.point);
        }
        else
        {
            host.ShowLaser(muzzle.position, muzzle.position + direction * host.Data.range);
        }
    }
}

/// <summary>
/// 内置武器行为：弹道（手枪/步枪）。生成一颗带初速度的子弹。
/// </summary>
public sealed class ProjectileBehavior : IWeaponBehavior
{
    public const string BehaviorId = "projectile";
    public string Id => BehaviorId;

    public void Fire(WeaponController host, Vector3 direction)
    {
        host.SpawnProjectile(direction, throwable: false);
    }
}

/// <summary>
/// 内置武器行为：投掷（飞刀）。生成受重力、带额外上抛与自旋的投掷物。
/// </summary>
public sealed class ThrowableBehavior : IWeaponBehavior
{
    public const string BehaviorId = "throwable";
    public string Id => BehaviorId;

    public void Fire(WeaponController host, Vector3 direction)
    {
        host.SpawnProjectile(direction, throwable: true);
    }
}
