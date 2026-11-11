using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 玩家预制体生成器。
/// 菜单：Tools → Player → Rebuild Player Prefab
///
/// 生成 Assets/Prefabs/Player.prefab —— 这是玩家的"唯一模板"：
///  - 场景里放它的实例，可以在 Inspector 里直接改
///  - 改预制体本体，所有实例同步更新
///  - 以后 mod 作者做新角色，只需基于它做变体或替换 ModelRoot 下的模型
///
/// 预制体由 PlayerFactory 程序化搭出后保存，所以代码与模板不会漂移。
/// </summary>
public static class PlayerPrefabBuilder
{
    public const string PrefabPath = "Assets/Prefabs/Player.prefab";
    private const string PrefabFolder = "Assets/Prefabs";

    [MenuItem("Tools/Player/Rebuild Player Prefab")]
    public static void RebuildMenu()
    {
        Rebuild();
    }

    /// <summary>确保预制体存在；不存在则生成一份。</summary>
    public static GameObject EnsurePrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        return existing != null ? existing : Rebuild();
    }

    /// <summary>重新生成玩家预制体（会覆盖同路径文件）。</summary>
    public static GameObject Rebuild()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // 在当前场景里搭一份临时玩家 → 存成预制体 → 立刻删掉
        GameObject temp = PlayerFactory.Build(Vector3.zero, Quaternion.identity, reuseExisting: false);
        temp.name = "Player";

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath);

        Object.DestroyImmediate(temp);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Player] 玩家预制体已生成：{PrefabPath}");
        return prefab;
    }
}
