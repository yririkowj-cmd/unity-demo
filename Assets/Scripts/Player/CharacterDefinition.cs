using System;
using UnityEngine;

/// <summary>角色的运动参数集（一套参数 = 一种手感）。</summary>
[Serializable]
public class MovementProfile
{
    [Header("地面")]
    public float walkSpeed = 12f;
    public float sprintMultiplier = 1.6f;
    public float acceleration = 60f;
    public float deceleration = 80f;

    [Header("飞行")]
    public float flySpeed = 14f;
    public float flyAcceleration = 50f;
    public float flyVerticalSpeed = 8f;

    [Header("跳跃与重力")]
    public float jumpHeight = 3f;
    public float gravity = -19.62f;
}

/// <summary>角色的相机参数集。</summary>
[Serializable]
public class CameraProfile
{
    public float eyeHeight = 1.7f;
    public float shoulderHeight = 1.5f;
    public float shoulderDistance = 2.5f;
}

/// <summary>
/// 角色定义（数据资产）。
///
/// 角色 = 参数 + 技能槽 + 模型。技能目前只存 id，真正的技能系统在下一阶段实现：
/// 届时 <see cref="abilityIds"/> 会通过 <see cref="ContentRegistry"/> 解析成 AbilityDefinition。
///
/// 新增角色 = 新建一个该资产并填参数，不需要写代码、不需要改核心类。
/// 加载后调用 <c>ContentRegistry.Register("character." + id, def)</c> 即可被查询/引用。
/// </summary>
[CreateAssetMenu(fileName = "NewCharacter", menuName = "Game/Character Definition")]
public class CharacterDefinition : ScriptableObject
{
    [Header("标识")]
    public string id = "agent_default";
    public string displayName = "默认角色";

    [Header("运动参数")]
    public MovementProfile movement = new MovementProfile();

    [Header("相机参数")]
    public CameraProfile cameraProfile = new CameraProfile();

    [Header("技能槽(存 id，技能系统后续实现)")]
    public string[] abilityIds = new string[3];

    [Header("表现(预留)")]
    public GameObject modelPrefab;

    /// <summary>把参数应用到玩家的移动组件。</summary>
    public void ApplyTo(PlayerMovement movementComponent)
    {
        if (movementComponent == null) return;
        movementComponent.ApplyProfile(this);
    }

    /// <summary>把参数应用到相机组件。</summary>
    public void ApplyTo(CameraRig cameraRig)
    {
        if (cameraRig == null) return;
        cameraRig.ApplyProfile(this);
    }

    /// <summary>
    /// 解析技能槽里的 id 并注册到运行时。
    /// 当前阶段还没有具体技能，所以通常注册 0 个——这是预期的；
    /// 以后新增技能只需实现 IAbility 并在 ContentRegistry 注册 "ability.xxx"。
    /// </summary>
    public void ApplyAbilities(AbilityRuntime runtime)
    {
        if (runtime == null || abilityIds == null) return;

        foreach (string id in abilityIds)
        {
            if (string.IsNullOrEmpty(id)) continue;

            if (ContentRegistry.TryGet("ability." + id, out IAbility ability) && ability != null)
                runtime.Register(ability);
            else
                Debug.LogWarning($"[Character] 未找到技能 '{id}'（ContentRegistry 中没有 ability.{id}）");
        }
    }

    /// <summary>注册到全局内容表（加载角色时调用）。</summary>
    public void Register()
    {
        ContentRegistry.Register("character." + id, this);
    }
}
