using UnityEngine;

/// <summary>
/// 武器行为注册表：把 <see cref="IWeaponBehavior"/> 注册进 <see cref="ContentRegistry"/>。
///
/// 查找顺序：
///  1) WeaponData.behaviorId（推荐，可指向 mod 注册的行为）
///  2) 为空时按旧的 fireType 枚举回退（兼容已有武器资产）
///  3) 都找不到时回退到 hitscan，并打印警告
/// </summary>
public static class WeaponBehaviorRegistry
{
    private const string Prefix = "weapon.behavior.";

    /// <summary>注册一个行为（内置或 mod）。同 id 覆盖。</summary>
    public static void Register(IWeaponBehavior behavior)
    {
        if (behavior == null || string.IsNullOrEmpty(behavior.Id)) return;
        ContentRegistry.Register(Prefix + behavior.Id, behavior);
    }

    public static bool TryGet(string id, out IWeaponBehavior behavior)
    {
        EnsureBuiltIns();
        return ContentRegistry.TryGet(Prefix + id, out behavior);
    }

    /// <summary>按 behaviorId → fireType → hitscan 的顺序解析出可用的行为。</summary>
    public static IWeaponBehavior Resolve(string behaviorId, FireType fallback = FireType.Hitscan)
    {
        EnsureBuiltIns();

        string id = string.IsNullOrEmpty(behaviorId) ? fallback.ToString().ToLowerInvariant() : behaviorId;

        if (TryGet(id, out IWeaponBehavior behavior) && behavior != null)
            return behavior;

        Debug.LogWarning($"[Weapon] 未注册的 behaviorId '{id}'，已回退为 {HitscanBehavior.BehaviorId}。");

        return TryGet(HitscanBehavior.BehaviorId, out IWeaponBehavior fallbackBehavior)
            ? fallbackBehavior
            : null;
    }

    /// <summary>
    /// 惰性注册内置行为。用"逐个检查是否存在"而不是静态构造，
    /// 这样 ContentRegistry.Clear() 之后也能自动补回。
    /// </summary>
    private static void EnsureBuiltIns()
    {
        if (!ContentRegistry.Contains(Prefix + HitscanBehavior.BehaviorId))
            Register(new HitscanBehavior());
        if (!ContentRegistry.Contains(Prefix + ProjectileBehavior.BehaviorId))
            Register(new ProjectileBehavior());
        if (!ContentRegistry.Contains(Prefix + ThrowableBehavior.BehaviorId))
            Register(new ThrowableBehavior());
    }
}
