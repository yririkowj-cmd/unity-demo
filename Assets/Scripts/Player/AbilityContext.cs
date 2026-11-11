using UnityEngine;

/// <summary>
/// 触发一次能力时能给到的全部上下文。
/// 以后要加"体力/资源/连招计数"，只改这里，不用改各个能力实现。
/// </summary>
public sealed class AbilityContext
{
    /// <summary>能力拥有者（玩家根节点）。</summary>
    public GameObject Owner;

    /// <summary>发射/出手位置（可为空：空手技能）。</summary>
    public Transform Muzzle;

    /// <summary>用于瞄准的相机（可为空）。</summary>
    public Camera Camera;

    /// <summary>准星方向（世界空间单位向量）。</summary>
    public Vector3 AimDirection;

    /// <summary>准星命中点（无命中时为射线远端点）。</summary>
    public Vector3 AimPoint;

    /// <summary>当前武器（可为空：近战/空手技能）。</summary>
    public WeaponController Weapon;
}
