#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// 一键配置：Tags/Layers + 把玩家重构为标准结构(方便以后导入二游角色模型)。
/// 菜单：Tools/Setup Player & Layers
/// </summary>
public static class PlayerSetup
{
    [MenuItem("Tools/Setup Player & Layers")]
    public static void Run()
    {
        AddTag("Player");
        AddTag("Enemy");
        AddTag("Projectile");
        AddTag("Hitbox");

        int playerLayer = AddLayer("Player");
        AddLayer("Enemy");

        // 复用场景里已有的玩家(旧名 "Payer" 是拼写错误)
        GameObject player = GameObject.Find("Payer");
        if (player == null) player = GameObject.Find("Player");
        if (player == null) player = new GameObject("Player");

        player.name = "Player";
        player.layer = playerLayer;
        player.tag = "Player";
        player.transform.localScale = Vector3.one; // 控制器根节点保持 1:1，模型缩放放 ModelRoot

        // 角色控制器
        var cc = player.GetComponent<CharacterController>();
        if (cc == null) cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        // 脚本
        if (player.GetComponent<PlayerMovement>() == null) player.AddComponent<PlayerMovement>();
        if (player.GetComponent<CameraRig>() == null) player.AddComponent<CameraRig>();
        if (player.GetComponent<PlayerHealth>() == null) player.AddComponent<PlayerHealth>();

        // 模型挂点 + 占位胶囊(以后把二游模型放到 ModelRoot 下即可)
        Transform modelRoot = EnsureChild(player, "ModelRoot");
        Transform body = modelRoot.Find("Body");
        if (body == null)
        {
            // 旧的 Body 可能直接挂在玩家根下，先搬进来
            Transform oldBody = player.transform.Find("Body");
            if (oldBody != null && oldBody.parent == player.transform)
                oldBody.SetParent(modelRoot, false);

            body = modelRoot.Find("Body");
        }
        if (body == null)
        {
            body = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
            body.name = "Body";
            body.SetParent(modelRoot, false);
        }
        // 视觉占位胶囊不参与碰撞(碰撞交给 CharacterController)，无论新建还是沿用都要移除
        var bodyCol = body.GetComponent<Collider>();
        if (bodyCol != null) Object.DestroyImmediate(bodyCol);
        body.localPosition = new Vector3(0f, 0.9f, 0f);
        body.localScale = new Vector3(0.6f, 0.9f, 0.6f);

        // 武器挂点(右手，以后武器模型/发射点挂这里)
        EnsureChild(player, "WeaponSocket");

        // 地面检测点
        Transform groundCheck = EnsureChild(player, "GroundCheck");
        groundCheck.localPosition = new Vector3(0f, 0.05f, 0f);

        // 相机(复用已有的 Main Camera，没有则创建)
        Camera cam = player.GetComponentInChildren<Camera>();
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.transform.SetParent(player.transform, false);
        }

        // 连线引用
        var rig = player.GetComponent<CameraRig>();
        rig.modelRoot = modelRoot;

        var pm = player.GetComponent<PlayerMovement>();
        pm.groundCheck = groundCheck;
        pm.groundMask = LayerMask.GetMask("Ground");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Player 结构 + Layers/Tags 配置完成。");
    }

    private static Transform EnsureChild(GameObject parent, string name)
    {
        Transform t = parent.transform.Find(name);
        if (t != null) return t;
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go.transform;
    }

    private static void AddTag(string tag)
    {
        var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        var so = new SerializedObject(asset);
        var prop = so.FindProperty("tags");
        for (int i = 0; i < prop.arraySize; i++)
            if (prop.GetArrayElementAtIndex(i).stringValue == tag) return;
        prop.InsertArrayElementAtIndex(prop.arraySize);
        prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = tag;
        so.ApplyModifiedProperties();
    }

    private static int AddLayer(string layer)
    {
        var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        var so = new SerializedObject(asset);
        var prop = so.FindProperty("layers");
        // 已存在则返回其索引
        for (int i = 0; i < prop.arraySize; i++)
            if (prop.GetArrayElementAtIndex(i).stringValue == layer) return i;
        // 找第一个空的用户层(8 起，避开保留位)
        for (int i = 8; i < prop.arraySize; i++)
        {
            var el = prop.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(el.stringValue))
            {
                el.stringValue = layer;
                so.ApplyModifiedProperties();
                return i;
            }
        }
        Debug.LogWarning("没有空的 Layer 位，请手动添加: " + layer);
        return -1;
    }
}
#endif
