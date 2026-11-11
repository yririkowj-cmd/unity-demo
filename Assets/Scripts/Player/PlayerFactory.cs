using UnityEngine;

/// <summary>
/// 玩家工厂：提供"从预制体生成玩家"与"应用角色参数"的能力。
///
/// 主路径：<c>Instantiate(playerPrefab, definition, pos, rot)</c>
///   —— 场景/运行时都走这条，玩家模板是 Assets/Prefabs/Player.prefab。
///
/// 兜底路径：<c>Build(...)</c> 用代码现场搭一个玩家，
///   —— 只在预制体缺失（首次生成）或做测试时使用，不要放进正常游戏流程，
///      否则会和预制体形成"两个真相"。
/// </summary>
public static class PlayerFactory
{
    /// <summary>
    /// 在指定位置创建一个标准玩家（程序化搭建）。
    /// 注意：这是"脚手架/兜底"路径，正常情况下请使用 <see cref="Instantiate(GameObject, CharacterDefinition, Vector3, Quaternion)"/>
    /// 配合 Assets/Prefabs/Player.prefab，这样玩家可以在 Inspector 里维护。
    /// </summary>
    /// <param name="reuseExisting">场景里已有 Player 时是否直接复用（造预制体时必须传 false）</param>
    public static GameObject Build(Vector3 position, Quaternion rotation, bool reuseExisting = true)
    {
        if (reuseExisting)
        {
            GameObject existing = GameObject.Find("Player");
            if (existing != null) return existing;
        }

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

    /// <summary>
    /// 从预制体实例化玩家，并应用角色参数（运行时/场景的主路径）。
    /// 没有传预制体时退回程序化搭建。
    /// </summary>
    public static GameObject Instantiate(GameObject playerPrefab, CharacterDefinition definition,
                                         Vector3 position, Quaternion rotation)
    {
        if (playerPrefab == null)
            return Build(position, rotation, reuseExisting: false);

        GameObject player = Object.Instantiate(playerPrefab, position, rotation);

        if (definition != null)
        {
            definition.ApplyTo(player.GetComponent<PlayerMovement>());
            definition.ApplyTo(player.GetComponent<CameraRig>());
        }

        return player;
    }

    private static void SafeDestroy(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Object.Destroy(obj);
        else Object.DestroyImmediate(obj);
    }
}
