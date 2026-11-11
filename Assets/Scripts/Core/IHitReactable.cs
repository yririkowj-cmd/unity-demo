/// <summary>
/// "能被命中反应"的对象：靶子、NPC、可破坏物、可交互物。
///
/// 这是通往"高度自由/超现实世界"的那条路：
/// 如果世界里的东西只能"受伤"，那就永远做不出击飞、拉拽、点燃、变形。
/// 实现本接口的对象自行决定收到命中后怎么反应，武器/技能完全不用知道。
///
/// 与 <see cref="IDamageable"/> 并列且可选：只想要伤害的对象不必实现它。
/// </summary>
public interface IHitReactable
{
    void React(in HitResult hit);
}
