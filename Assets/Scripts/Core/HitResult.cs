using UnityEngine;

/// <summary>
/// 一次命中的"物理/交互事件"。
///
/// 与 <see cref="DamageInfo"/> 的分工：
///  - DamageInfo 描述"扣多少血"（数值结算）
///  - HitResult 描述"发生了什么"（命中点、法线、击退力、来源）——给击飞、拉拽、点燃、变形等反应使用
///
/// 武器/技能只负责产生 HitResult，不关心对面是谁；
/// 对面希望被怎样对待，由它自己实现 <see cref="IHitReactable"/> 决定。
/// </summary>
public readonly struct HitResult
{
    public readonly Collider collider;
    public readonly Vector3 point;
    public readonly Vector3 normal;
    /// <summary>击退/推力（零表示无）。</summary>
    public readonly Vector3 impulse;
    /// <summary>谁造成的这次命中。</summary>
    public readonly GameObject source;

    public HitResult(Collider collider, Vector3 point, Vector3 normal, Vector3 impulse, GameObject source)
    {
        this.collider = collider;
        this.point = point;
        this.normal = normal;
        this.impulse = impulse;
        this.source = source;
    }
}
