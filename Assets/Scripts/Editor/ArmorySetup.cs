using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 军火库白盒生成器。
/// 菜单：Tools → Armory → Build Whitebox Scene
///
/// 生成内容：
///  - 房间：地面、四面墙（右墙留门洞）、天花板
///  - 武器墙：左侧三个展位（占位枪模型，无碰撞）
///  - 靶位：正对入口的三个靶子（带 TargetHealth）
///  - 出口：右墙门洞 + 门外平台
///  - 玩家：实例化 Assets/Prefabs/Player.prefab
///
/// 房间尺寸由下面的常量控制，改完重新执行菜单即可（会覆盖场景）。
/// </summary>
public static class ArmorySetup
{
    private const string ScenePath = "Assets/Scenes/Hub.unity";

    // —— 房间尺寸（改这里）——
    private const float RoomWidth = 30f;    // X 方向
    private const float RoomDepth = 20f;    // Z 方向
    private const float RoomHeight = 6f;    // Y 方向
    private const float WallThickness = 0.3f;
    private const float DoorWidth = 3.2f;   // 门洞宽度
    private const float DoorCenterZ = 5f;   // 门洞中心的 Z（相对房间中心）

    [MenuItem("Tools/Armory/Build Whitebox Scene")]
    public static void Build()
    {
        if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog(
                "重建军火库场景",
                "Hub.unity 已存在，继续会覆盖（场景中的手动修改会丢失）。",
                "覆盖重建", "取消"))
            return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var root = new GameObject("Armory").transform;
        int groundLayer = LayerMask.NameToLayer("Ground");

        BuildRoom(root, groundLayer);
        BuildWeaponRacks(root);
        BuildTargets(root);
        BuildLighting(root);
        BuildPlayer();

        EditorSceneManager.MarkSceneDirty(scene);
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EnsureInBuildSettings();
        Debug.Log($"[Armory] 白盒场景已生成：{ScenePath}（{RoomWidth} × {RoomDepth} × {RoomHeight}）");
    }

    // —— 房间 ——

    private static void BuildRoom(Transform root, int groundLayer)
    {
        float halfW = RoomWidth * 0.5f;
        float halfD = RoomDepth * 0.5f;

        // 地面 / 天花板
        var floor = Box("Floor", new Vector3(0f, -0.1f, 0f), new Vector3(RoomWidth, 0.2f, RoomDepth), root);
        if (groundLayer >= 0) floor.layer = groundLayer;
        Box("Ceiling", new Vector3(0f, RoomHeight, 0f), new Vector3(RoomWidth, 0.2f, RoomDepth), root);

        // 后墙 / 左墙 / 前墙
        Box("Wall_Back", new Vector3(0f, RoomHeight * 0.5f, -halfD + WallThickness * 0.5f),
            new Vector3(RoomWidth, RoomHeight, WallThickness), root);
        Box("Wall_Front", new Vector3(0f, RoomHeight * 0.5f, halfD - WallThickness * 0.5f),
            new Vector3(RoomWidth, RoomHeight, WallThickness), root);
        Box("Wall_Left", new Vector3(-halfW + WallThickness * 0.5f, RoomHeight * 0.5f, 0f),
            new Vector3(WallThickness, RoomHeight, RoomDepth), root);

        // 右墙：留出门洞
        float doorTop = Mathf.Min(2.6f, RoomHeight - 1f);
        float segmentAEnd = DoorCenterZ - DoorWidth * 0.5f;
        float segmentBStart = DoorCenterZ + DoorWidth * 0.5f;

        float aLength = segmentAEnd - (-halfD);
        if (aLength > 0.01f)
            Box("Wall_Right_A", new Vector3(halfW - WallThickness * 0.5f, RoomHeight * 0.5f, -halfD + aLength * 0.5f),
                new Vector3(WallThickness, RoomHeight, aLength), root);

        float bLength = halfD - segmentBStart;
        if (bLength > 0.01f)
            Box("Wall_Right_B", new Vector3(halfW - WallThickness * 0.5f, RoomHeight * 0.5f, halfD - bLength * 0.5f),
                new Vector3(WallThickness, RoomHeight, bLength), root);

        // 门洞上方的墙（门楣）
        float lintelHeight = RoomHeight - doorTop;
        if (lintelHeight > 0.01f)
            Box("Door_Lintel", new Vector3(halfW - WallThickness * 0.5f, doorTop + lintelHeight * 0.5f, DoorCenterZ),
                new Vector3(WallThickness, lintelHeight, DoorWidth), root);

        // 门外平台（通向探索区的占位）
        var outside = Box("Exit_Platform", new Vector3(halfW + 2f, -0.1f, DoorCenterZ),
            new Vector3(4f, 0.2f, DoorWidth + 1f), root);
        if (groundLayer >= 0) outside.layer = groundLayer;
    }

    // —— 武器墙 ——

    private static void BuildWeaponRacks(Transform root)
    {
        float wallX = -(RoomWidth * 0.5f - 1f);
        string[] names = { "Rack_Rifle", "Rack_Pistol", "Rack_Shotgun" };

        for (int i = 0; i < names.Length; i++)
        {
            float z = (i - 1) * 6f; // -6 / 0 / 6
            // 展台
            Box(names[i], new Vector3(wallX, 0.5f, z), new Vector3(0.8f, 1f, 0.8f), root);
            // 展台上的占位枪（纯视觉）
            Box(names[i] + "_Display", new Vector3(wallX, 1.15f, z), new Vector3(0.2f, 0.15f, 1.4f), root, false);
        }
    }

    // —— 靶位 ——

    private static void BuildTargets(Transform root)
    {
        float z = RoomDepth * 0.5f - 1.5f;
        float spacing = RoomWidth * 0.2f; // 30 → 6 米间距

        for (int i = -1; i <= 1; i++)
        {
            float x = i * spacing;
            string id = (i + 1).ToString();
            Box($"Target_{id}_Post", new Vector3(x, 0.5f, z), new Vector3(0.15f, 1f, 0.15f), root);
            GameObject board = Box($"Target_{id}", new Vector3(x, 1.4f, z), new Vector3(0.9f, 0.9f, 0.12f), root);
            if (TagExists("Target")) board.tag = "Target";
            if (board.GetComponent<TargetHealth>() == null) board.AddComponent<TargetHealth>();
        }
    }

    // —— 灯光 ——

    private static void BuildLighting(Transform root)
    {
        var sun = new GameObject("Directional Light");
        sun.transform.SetParent(root, false);
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var sunLight = sun.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.intensity = 0.6f;
        sunLight.shadows = LightShadows.Soft;

        float lampX = RoomWidth * 0.25f;
        float lampZ = RoomDepth * 0.25f;
        float lampY = RoomHeight - 0.6f;
        Vector3[] points =
        {
            new Vector3(-lampX, lampY, -lampZ), new Vector3(lampX, lampY, -lampZ),
            new Vector3(-lampX, lampY, lampZ),  new Vector3(lampX, lampY, lampZ)
        };

        for (int i = 0; i < points.Length; i++)
        {
            var go = new GameObject($"Lamp_{i}");
            go.transform.SetParent(root, false);
            go.transform.position = points[i];
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = Mathf.Max(RoomWidth, RoomDepth) * 0.8f;
            l.intensity = 3f;
        }
    }

    // —— 玩家 ——

    private static void BuildPlayer()
    {
        GameObject prefab = PlayerPrefabBuilder.EnsurePrefab();
        var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        player.name = "Player";
        player.transform.SetPositionAndRotation(
            new Vector3(0f, 0.1f, -RoomDepth * 0.5f + 2f), Quaternion.identity);
    }

    // —— 构建设置 ——

    /// <summary>把 Hub 注册为启动场景（Build Settings 第 0 位），保留其他场景。</summary>
    [MenuItem("Tools/Armory/Register Hub In Build Settings")]
    public static void RegisterHubInBuildSettings()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            Debug.LogError($"[Armory] 找不到 {ScenePath}，请先执行 Build Whitebox Scene。");
            return;
        }

        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == ScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();

        Debug.Log("[Armory] 已把 Hub 注册为启动场景（Build Settings 第 0 位）。");
    }

    private static void EnsureInBuildSettings()
    {
        RegisterHubInBuildSettings();
    }

    // —— 工具 ——

    private static GameObject Box(string name, Vector3 position, Vector3 size, Transform parent, bool withCollider = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = size;
        if (!withCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    private static bool TagExists(string tag)
    {
        foreach (string t in UnityEditorInternal.InternalEditorUtility.tags)
            if (t == tag) return true;
        return false;
    }
}
