using UnityEngine;

/// <summary>开火方式：射线(一帧判定) / 弹道(飞行子弹) / 投掷(受重力)。</summary>
public enum FireType
{
    Hitscan,
    Projectile,
    Throwable
}

/// <summary>射击模式：单发 / 连发 / 自动。</summary>
public enum FiringMode
{
    Single,
    Burst,
    Auto
}

/// <summary>
/// 武器数据(数据资产)：所有参数数据化，新增一种武器 = 新建一个该资产并配置数值。
/// 菜单：Assets/Create/Weapon/Weapon Data
/// </summary>
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapon/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("基本信息")]
    public string displayName = "Weapon";
    [Tooltip("发射行为 id（如 hitscan / projectile / throwable，或 mod 注册的自定义 id）。留空则按 fireType 回退。")]
    public string behaviorId = "";
    public FireType fireType = FireType.Hitscan;
    public FiringMode firingMode = FiringMode.Auto;

    [Header("射速与伤害")]
    [Tooltip("每分钟射速(发/分钟)")]
    public float roundsPerMinute = 600f;
    public float damage = 25f;
    public DamageType damageType = DamageType.Hitscan;

    [Header("精度与射程")]
    [Tooltip("散布(度)，越大越不准")]
    public float spreadDegrees = 0.5f;
    public float range = 100f;

    [Header("弹道/投掷物(Projectile/Throwable 用)")]
    public GameObject bulletPrefab;
    public float projectileSpeed = 50f;
    [Tooltip("投掷物额外向上抛的初速度(飞刀抛物线用)")]
    public float throwUpSpeed = 0f;
    [Tooltip("投掷物翻滚速度(飞刀转圈用)")]
    public float throwSpinSpeed = 0f;

    [Header("弹药")]
    public int magazineSize = 30;
    public int reserveAmmo = 120;
    public float reloadTime = 2f;

    [Header("连发模式(Burst)")]
    public int bulletsPerBurst = 3;
}
