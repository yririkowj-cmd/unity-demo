using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 能力的统一运行时：负责注册与冷却，不关心具体能力做什么。
///
/// 这是"以后加技能只写新文件、不改老代码"的那一层：
///  新能力 = 实现 IAbility → 注册进 ContentRegistry（"ability.xxx"）→
///  角色定义的 abilityIds 填 id → 由本类统一管理冷却与触发。
///
/// 当前阶段**不挂具体能力**，只把管道打通。
/// </summary>
[DisallowMultipleComponent]
public sealed class AbilityRuntime : MonoBehaviour
{
    private readonly Dictionary<string, IAbility> abilities = new Dictionary<string, IAbility>();
    private readonly Dictionary<string, float> nextReadyTime = new Dictionary<string, float>();

    /// <summary>已注册的能力 id（用于调试/HUD）。</summary>
    public IEnumerable<string> RegisteredIds => abilities.Keys;

    public void Register(IAbility ability)
    {
        if (ability == null || string.IsNullOrEmpty(ability.Id)) return;
        abilities[ability.Id] = ability;
        if (!nextReadyTime.ContainsKey(ability.Id)) nextReadyTime[ability.Id] = 0f;
    }

    public void Unregister(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        abilities.Remove(id);
        nextReadyTime.Remove(id);
    }

    public bool IsRegistered(string id)
    {
        return !string.IsNullOrEmpty(id) && abilities.ContainsKey(id);
    }

    public bool IsReady(string id)
    {
        if (!IsRegistered(id)) return false;
        return !nextReadyTime.TryGetValue(id, out float ready) || Time.time >= ready;
    }

    public float GetCooldownRemaining(string id)
    {
        if (!nextReadyTime.TryGetValue(id, out float ready)) return 0f;
        return Mathf.Max(0f, ready - Time.time);
    }

    /// <summary>尝试触发一个能力；不在冷却、且 CanTrigger 通过才执行。</summary>
    public bool TryTrigger(string id, AbilityContext context)
    {
        if (string.IsNullOrEmpty(id) || context == null) return false;
        if (!abilities.TryGetValue(id, out IAbility ability) || ability == null) return false;
        if (!IsReady(id)) return false;
        if (!ability.CanTrigger(context)) return false;

        nextReadyTime[id] = Time.time + Mathf.Max(0f, ability.Cooldown);
        ability.Trigger(context);
        return true;
    }
}
