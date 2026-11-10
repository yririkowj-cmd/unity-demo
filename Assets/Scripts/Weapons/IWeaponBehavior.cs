using UnityEngine;

/// <summary>
/// 武器"发射行为"契约。
///
/// 一次开火分成两件事：
///  1) 何时开火、消耗弹药、冷却 —— 由 <see cref="WeaponController"/> 负责
///  2) 这一发怎么打出去 —— 由 IWeaponBehavior 负责
///
/// 内置实现：hitscan / projectile / throwable。
/// 扩展方式（包括将来的 mod）：实现本接口 → 在 <see cref="WeaponBehaviorRegistry"/> 注册 →
/// 在 WeaponData.behaviorId 填对应的 id。**不需要修改 WeaponController**。
/// </summary>
public interface IWeaponBehavior
{
    /// <summary>唯一标识，例如 "hitscan"。WeaponData.behaviorId 用它来查找。</summary>
    string Id { get; }

    /// <summary>
    /// 执行一次开火。
    /// </summary>
    /// <param name="host">武器宿主，提供枪口、相机、射线查询、伤害结算、生成子弹等稳定 API。</param>
    /// <param name="direction">已经算好散布的射击方向（世界空间单位向量）。</param>
    void Fire(WeaponController host, Vector3 direction);
}
