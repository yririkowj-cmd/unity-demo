/// <summary>
/// 能力/动作的唯一契约：开枪、挥砍、冲刺、技能、大招都是它。
///
/// 注意：现在**只做入口，不做内容**。
/// 具体有哪些能力，等设计定了再实现，避免把还没确定的东西写死。
/// </summary>
public interface IAbility
{
    /// <summary>唯一标识，例如 "ability.dash"。CharacterDefinition.abilityIds 里填它。</summary>
    string Id { get; }

    /// <summary>冷却时间（秒），0 表示无冷却。</summary>
    float Cooldown { get; }

    /// <summary>当前是否允许触发（资源、状态、面向等条件）。</summary>
    bool CanTrigger(AbilityContext context);

    /// <summary>执行能力。</summary>
    void Trigger(AbilityContext context);
}
