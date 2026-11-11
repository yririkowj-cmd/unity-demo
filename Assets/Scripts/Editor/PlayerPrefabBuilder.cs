using UnityEditor;
using UnityEngine;

/// <summary>
/// 玩家预制体的"首次生成器"。
///
/// 约定：<c>Assets/Prefabs/Player.prefab</c> 是玩家的**唯一真身**。
///  - 日常修改：直接在 Inspector 里改 prefab（或场景实例做 override）
///  - 新增场景：用 <see cref="EnsurePrefab"/> 取到它再实例化
///  - 恢复基准：手动删除 prefab，然后跑一次军火库生成器（会走这里自动重建）
///
/// 本类**不提供**"一键重建"菜单 —— 因为那会覆盖你在 Inspector 里做的手动修改，
/// 制造"代码结构"和"预制体结构"两个真相。
/// </summary>
public static class PlayerPrefabBuilder
{
    public const string PrefabPath = "Assets/Prefabs/Player.prefab";
    private const string PrefabFolder = "Assets/Prefabs";

    /// <summary>确保预制体存在；不存在则用代码生成一份（仅首次/恢复时发生）。</summary>
    public static GameObject EnsurePrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null) return existing;

        Debug.LogWarning($"[Player] 未找到 {PrefabPath}，正在用代码生成一份初始模板。之后请直接在 Inspector 里修改它。");
        return BuildFromCode();
    }

    /// <summary>
    /// 用 PlayerFactory 程序化搭一份并保存为预制体。
    /// 仅供 <see cref="EnsurePrefab"/> 在预制体缺失时调用；不要在日常开发中调用。
    /// </summary>
    private static GameObject BuildFromCode()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

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
