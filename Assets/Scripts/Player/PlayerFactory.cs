using UnityEngine;

/// <summary>
/// 玩家工厂：按标准结构创建一个可直接运行的玩家（控制器 + 移动 + 相机 + 血条 + 挂点）。
///
/// 用途：新场景（军火库/探索区）需要玩家时调用 Build()，避免每个场景手工搭一遍。
/// 与 PlayerSetup 的区别：PlayerSetup 是"改造场景里已有的玩家"（迁移用），
/// 本类是从零构建，两者共享同一套结构约定。
/// </summary>
public static class PlayerFactory
{
    /// <summary>在指定位置创建一个标准玩家。若场景中已有名为 Player 的对象则直接返回它。</summary>
    public static GameObject Build(Vector3 position, Quaternion rotation)
    {
        GameObject existing = GameObject.Find("Player");
        if (existing != null) return existing;

        var player = new GameObject("Player");
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0) player.layer = playerLayer;
        if (TagExists("Player")) player.tag = "Player";
        player.transform.SetPositionAndRotation(position, rotation);

        // 碰撞体：CharacterController 唯一权威，视觉模型不带碰撞
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        // 组件（PlayerMovement 会自动补齐 PlayerHealth）
        player.AddComponent<PlayerHealth>();
        player.AddComponent<CameraRig>();
        var movement = player.AddComponent<PlayerMovement>();

        // 模型挂点 + 占位胶囊（以后把二游模型放到 ModelRoot 下）
        Transform modelRoot = new GameObject("ModelRoot").transform;
        modelRoot.SetParent(player.transform, false);

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        SafeDestroy(body.GetComponent<Collider>());
        body.transform.SetParent(modelRoot, false);
        body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        body.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);

        // 地面检测点
        Transform groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(player.transform, false);
        groundCheck.localPosition = new Vector3(0f, 0.05f, 0f);

        // 武器挂点（拾取系统会把武器挂到这里）
        Transform socket = new GameObject("WeaponSocket").transform;
        socket.SetParent(player.transform, false);
        socket.localPosition = new Vector3(0f, 1.35f, 0.4f);

        // 相机
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        camGo.transform.SetParent(player.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 1.5f, -2.5f);

        // 连线
        var rig = player.GetComponent<CameraRig>();
        rig.modelRoot = modelRoot;

        movement.groundCheck = groundCheck;
        movement.groundMask = LayerMask.GetMask("Ground");
        if (movement.groundMask == 0)
            Debug.LogWarning("[PlayerFactory] 项目中没有 Ground 层，地面检测将失效（先运行 Tools → Setup Player & Layers）。");

        return player;
    }

    private static bool TagExists(string tag)
    {
        // 用 try/catch 探测自定义标签是否存在（运行时没有 InternalEditorUtility 可用）
        try
        {
            GameObject.FindWithTag(tag);
            return true;
        }
        catch (UnityException)
        {
            return false;
        }
    }

    private static void SafeDestroy(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Object.Destroy(obj);
        else Object.DestroyImmediate(obj);
    }
}
