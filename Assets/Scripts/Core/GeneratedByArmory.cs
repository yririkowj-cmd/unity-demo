using UnityEngine;

/// <summary>
/// 标记"本场景由生成器维护"。
///
/// 挂在生成器创建的根节点（Armory）上。看到它就意味着：
/// 这个场景不要手动改，下次执行 Tools → Armory → Build Whitebox Scene 会整体重建，
/// 手动改动会丢失；要改布局请改 ArmorySetup.cs 里的参数。
/// </summary>
[DisallowMultipleComponent]
public class GeneratedByArmory : MonoBehaviour
{
    [Tooltip("生成时间，由生成器写入")]
    public string generatedAt;

    [Tooltip("生成器来源")]
    public string generator = "ArmorySetup";
}
