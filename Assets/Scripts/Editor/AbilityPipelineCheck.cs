using UnityEditor;
using UnityEngine;

/// <summary>
/// 能力管道的自检工具（只在编辑器里跑，不会进入游戏内容）。
///
/// 为什么需要：AbilityRuntime 是"入口"，但入口如果从来没被调用过，
/// 就等于没验证过的 API。这个工具用一个临时的假能力跑一遍
/// "注册 → 触发 → 冷却拦截 → 未注册拦截"，打印结果后销毁临时对象，
/// 不往项目里留任何假技能。
///
/// 菜单：Tools → Debug → Verify Ability Pipeline
/// </summary>
public static class AbilityPipelineCheck
{
    [MenuItem("Tools/Debug/Verify Ability Pipeline")]
    public static void Run()
    {
        var host = new GameObject("~AbilityPipelineCheck");
        try
        {
            var runtime = host.AddComponent<AbilityRuntime>();
            var testAbility = new TestAbility();
            runtime.Register(testAbility);

            var context = new AbilityContext
            {
                Owner = host,
                AimDirection = Vector3.forward,
                AimPoint = Vector3.forward * 10f
            };

            bool first = runtime.TryTrigger("ability.test", context);      // 应成功
            bool second = runtime.TryTrigger("ability.test", context);     // 冷却中，应失败
            bool unknown = runtime.TryTrigger("ability.none", context);    // 未注册，应失败

            bool pass = first && !second && !unknown && testAbility.TriggerCount == 1;

            if (pass)
                Debug.Log("[AbilityCheck] 通过：注册 / 触发 / 冷却拦截 / 未注册拦截 均正常。");
            else
                Debug.LogError($"[AbilityCheck] 失败：first={first}, second={second}, unknown={unknown}, count={testAbility.TriggerCount}");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    /// <summary>只存在于编辑器内存里的假能力，用来验证管道。</summary>
    private class TestAbility : IAbility
    {
        public string Id => "ability.test";
        public float Cooldown => 1f;
        public int TriggerCount { get; private set; }

        public bool CanTrigger(AbilityContext context) => context != null;

        public void Trigger(AbilityContext context)
        {
            TriggerCount++;
        }
    }
}
