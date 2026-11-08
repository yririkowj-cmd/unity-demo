#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// 一键创建 3 把示例武器(光炮/步枪/飞刀)并挂到玩家身上。
/// 菜单：Tools/Setup Weapons (3 samples)
/// 前提：先运行 Tools/Setup Player & Layers。
/// </summary>
public static class WeaponSetup
{
    [MenuItem("Tools/Setup Weapons (3 samples)")]
    public static void Run()
    {
        // 1) 确保数据目录存在
        EnsureFolder("Assets", "Data");
        EnsureFolder("Assets/Data", "Weapons");

        // 2) 找到玩家与相机
        GameObject player = GameObject.Find("Player") ?? GameObject.Find("Payer");
        if (player == null)
        {
            Debug.LogError("找不到 Player，请先运行 Tools/Setup Player & Layers。");
            return;
        }
        Camera cam = player.GetComponentInChildren<Camera>();
        if (cam == null)
        {
            Debug.LogError("找不到相机，请先运行 Tools/Setup Player & Layers。");
            return;
        }

        // 3) 子弹预制体(复用已有的 Bullet.prefab)
        GameObject bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet.prefab");

        // 4) 创建 3 个武器数据资产
        WeaponData laser = CreateWeaponData("LaserCannon", "光炮", FireType.Hitscan, FiringMode.Auto,
            300f, 60f, DamageType.Hitscan, 0f, 200f, 20, 100, 2f, null, 0f, 0f, 0f, 3);
        WeaponData rifle = CreateWeaponData("AssaultRifle", "步枪", FireType.Hitscan, FiringMode.Auto,
            600f, 25f, DamageType.Hitscan, 0.8f, 150f, 30, 120, 2f, null, 0f, 0f, 0f, 3);
        WeaponData knife = CreateWeaponData("ThrowingKnife", "飞刀", FireType.Throwable, FiringMode.Single,
            120f, 45f, DamageType.Throwable, 0.3f, 50f, 6, 24, 1.5f, bulletPrefab, 20f, 3f, 15f, 1);

        // 5) 移除旧武器(旧的 DummyWeapon)
        var oldDummy = player.transform.Find("DummyWeapon");
        if (oldDummy != null) Object.DestroyImmediate(oldDummy.gameObject);

        // 6) 创建 WeaponHolder + 3 把武器(先清空旧的，避免重复运行产生多份)
        var holder = EnsureChild(player, "WeaponHolder");
        holder.localPosition = new Vector3(0f, 1.35f, 0.4f);
        holder.localRotation = Quaternion.identity;
        for (int i = holder.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(holder.GetChild(i).gameObject);
        CreateWeapon(holder, "LaserCannon", laser, cam);
        CreateWeapon(holder, "AssaultRifle", rifle, cam);
        CreateWeapon(holder, "ThrowingKnife", knife, cam);

        // 7) 武器切换器
        var switcher = player.GetComponent<WeaponSwitcher>();
        if (switcher == null) switcher = player.AddComponent<WeaponSwitcher>();
        switcher.weapons = holder.GetComponentsInChildren<WeaponController>(true);

        // 8) 给现有靶子加 TargetHealth，便于测试伤害
        foreach (var t in GameObject.FindGameObjectsWithTag("Target"))
        {
            if (t.GetComponent<TargetHealth>() == null)
                t.AddComponent<TargetHealth>();
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("3 把示例武器已配置完成。滚轮 / 数字键切换，左键开火，R 换弹。");
    }

    private static WeaponData CreateWeaponData(string assetName, string displayName,
        FireType fireType, FiringMode mode, float rpm, float dmg, DamageType dmgType,
        float spread, float range, int mag, int reserve, float reload,
        GameObject bulletPrefab, float projSpeed, float throwUp, float spin, int burst)
    {
        string path = "Assets/Data/Weapons/" + assetName + ".asset";
        WeaponData wd = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
        if (wd == null)
        {
            wd = ScriptableObject.CreateInstance<WeaponData>();
            AssetDatabase.CreateAsset(wd, path);
        }

        wd.displayName = displayName;
        wd.fireType = fireType;
        wd.firingMode = mode;
        wd.roundsPerMinute = rpm;
        wd.damage = dmg;
        wd.damageType = dmgType;
        wd.spreadDegrees = spread;
        wd.range = range;
        wd.magazineSize = mag;
        wd.reserveAmmo = reserve;
        wd.reloadTime = reload;
        wd.bulletPrefab = bulletPrefab;
        wd.projectileSpeed = projSpeed;
        wd.throwUpSpeed = throwUp;
        wd.throwSpinSpeed = spin;
        wd.bulletsPerBurst = burst;

        EditorUtility.SetDirty(wd);
        return wd;
    }

    private static void CreateWeapon(Transform parent, string name, WeaponData data, Camera cam)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        var wc = go.AddComponent<WeaponController>();
        wc.data = data;
        wc.playerCamera = cam;

        // 枪口
        var muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(go.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0f, 0.5f);
        wc.muzzle = muzzle.transform;

        // 可视占位(一个拉伸的方块)
        var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.name = "Model";
        model.transform.SetParent(go.transform, false);
        model.transform.localPosition = new Vector3(0f, 0f, 0.25f);
        model.transform.localScale = new Vector3(0.08f, 0.08f, 0.5f);
        Object.DestroyImmediate(model.GetComponent<Collider>());

        // 命中光束(仅射线型武器)
        if (data.fireType == FireType.Hitscan)
        {
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.startWidth = 0.04f;
            lr.endWidth = 0.04f;
            lr.enabled = false;
            Material mat = GetLaserMaterial();
            if (mat != null) lr.sharedMaterial = mat;
            wc.laserLine = lr;
        }
    }

    private static Material GetLaserMaterial()
    {
        const string path = "Assets/Materials/LaserLine.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) return null;

        mat = new Material(shader);
        mat.color = Color.cyan;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Transform EnsureChild(GameObject parent, string name)
    {
        Transform t = parent.transform.Find(name);
        if (t != null) return t;
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go.transform;
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
