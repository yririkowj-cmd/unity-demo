using System;
using System.Collections.Generic;

/// <summary>
/// 内容注册表：所有可扩展内容（武器行为、角色定义、技能、载具……）统一登记在这里。
///
/// 设计原则：**内置内容与 mod 内容走同一条通道**。
/// 将来 Lua mod 只需要调用同样的 Register 接口，就能注册自己的行为/角色/武器，
/// 不需要修改任何核心 C# 类。
///
/// 约定：id 使用 "类别.名称" 的形式，例如：
///   weapon.behavior.hitscan
///   character.agent_frost
///   ability.frost_grenade
/// </summary>
public static class ContentRegistry
{
    private static readonly Dictionary<string, object> Items =
        new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    /// <summary>注册/覆盖一个内容。同 id 后注册的覆盖先注册的（方便 mod 覆盖内置）。</summary>
    public static void Register(string id, object value)
    {
        if (string.IsNullOrEmpty(id) || value == null) return;
        Items[id] = value;
    }

    public static bool TryGet<T>(string id, out T value) where T : class
    {
        value = null;
        if (string.IsNullOrEmpty(id)) return false;
        return Items.TryGetValue(id, out object raw) && raw is T typed && (value = typed) != null;
    }

    public static T GetOrDefault<T>(string id) where T : class
    {
        return TryGet(id, out T value) ? value : null;
    }

    public static bool Contains(string id)
    {
        return !string.IsNullOrEmpty(id) && Items.ContainsKey(id);
    }

    public static void Unregister(string id)
    {
        if (!string.IsNullOrEmpty(id)) Items.Remove(id);
    }

    /// <summary>已注册的所有 id（可用于编辑器/调试面板展示）。</summary>
    public static IEnumerable<string> Ids => Items.Keys;

    /// <summary>清空注册表（重载内容时使用；内置内容会在静态构造时重新注册）。</summary>
    public static void Clear()
    {
        Items.Clear();
    }
}
