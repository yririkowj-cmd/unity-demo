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
///  - 出口：右墙门洞 + 门外平台 + Exit_To_Exploration 标记
///  - 玩家：用 PlayerFactory 按标准结构创建
///
/// 白盒是程序化生成的，参数都在本文件里，改完重新执行菜单即可；
/// 不要手动改生成出来的场景，否则下次重建会被覆盖。
/// </summary>
public static class ArmorySetup
{
    private const string ScenePath = "Assets/Scenes/Hub.unity";

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
        Debug.Log($"[Armory] 白盒场景已生成：{ScenePath}");
    }

    private static void EnsureInBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == ScenePath)) return;
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // —— 房间 ——

    private static void BuildRoom(Transform root, int groundLayer)
    {
        // 地面 / 天花板
        var floor = Box("Floor", new Vector3(0f, -0.1f, 0f), new Vector3(20f, 0.2f, 12f), root);
        if (groundLayer >= 0) floor.layer = groundLayer;
        Box("Ceiling", new Vector3(0f, 4f, 0f), new Vector3(20f, 0.2f, 12f), root);

        // 后墙 / 左墙 / 前墙
        Box("Wall_Back", new Vector3(0f, 2f, -5.85f), new Vector3(20f, 4f, 0.3f), root);
        Box("Wall_Left", new Vector3(-9.85f, 2f, 0f), new Vector3(0.3f, 4f, 12f), root);
        Box("Wall_Front", new Vector3(0f, 2f, 5.85f), new Vector3(20f, 4f, 0.3f), root);

        // 右墙：留 z=1.5~4.5 的门洞
        Box("Wall_Right_A", new Vector3(9.85f, 2f, -2.25f), new Vector3(0.3f, 4f, 7.5f), root);
        Box("Wall_Right_B", new Vector3(9.85f, 2f, 5.25f), new Vector3(0.3f, 4f, 1.5f), root);
        Box("Door_Lintel", new Vector3(9.85f, 3.3f, 3f), new Vector3(0.3f, 1.4f, 3f), root);

        // 门外平台（通向探索区的占位）
        var outside = Box("Exit_Platform", new Vector3(11.5f, -0.1f, 3f), new Vector3(4f, 0.2f, 4f), root);
        if (groundLayer >= 0) outside.layer = groundLayer;
    }

    // —— 武器墙 ——

    private static void BuildWeaponRacks(Transform root)
    {
        string[] names = { "Rack_Rifle", "Rack_Pistol", "Rack_Shotgun" };
        for (int i = 0; i < names.Length; i++)
        {
            float z = -3f + i * 3f;
            // 展台
            Box(names[i], new Vector3(-9.2f, 0.5f, z), new Vector3(0.8f, 1f, 0.8f), root);
            // 展台上的占位枪（纯视觉）
            Box(names[i] + "_Display", new Vector3(-9.2f, 1.15f, z), new Vector3(0.2f, 0.15f, 1.2f), root, false);
        }
    }

    // —— 靶位 ——

    private static void BuildTargets(Transform root)
    {
        float[] xs = { -4f, 0f, 4f };
        for (int i = 0; i < xs.Length; i++)
        {
            float x = xs[i];
            Box($"Target_{i}_Post", new Vector3(x, 0.5f, 5.2f), new Vector3(0.15f, 1f, 0.15f), root);
            GameObject board = Box($"Target_{i}", new Vector3(x, 1.4f, 5.2f), new Vector3(0.9f, 0.9f, 0.12f), root);
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
        sunLight.intensity = 0.8f;

        Vector3[] points =
        {
            new Vector3(-5f, 3.4f, -3f), new Vector3(5f, 3.4f, -3f),
            new Vector3(-5f, 3.4f, 3f),  new Vector3(5f, 3.4f, 3f)
        };
        for (int i = 0; i < points.Length; i++)
        {
            var go = new GameObject($"Lamp_{i}");
            go.transform.SetParent(root, false);
            go.transform.position = points[i];
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 12f;
            l.intensity = 2f;
        }
    }

    // —— 玩家 ——

    private static void BuildPlayer()
    {
        // 主路径：实例化玩家预制体（可在 Inspector 里改，也可作为 mod 的基准）
        GameObject prefab = PlayerPrefabBuilder.EnsurePrefab();
        var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        player.name = "Player";
        player.transform.SetPositionAndRotation(new Vector3(0f, 0.1f, -4f), Quaternion.identity);
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
