using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 装备在手上的武器：读取 WeaponData 数据，处理开火(单发/连发/自动)、弹药与换弹。
/// 三种开火方式(射线/弹道/投掷)都在这里，新增武器类型只需在 Fire 里加一个分支。
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("武器数据")]
    public WeaponData data;

    [Header("挂点")]
    public Transform muzzle;        // 子弹/射线发射点(通常是枪口)
    public Camera playerCamera;     // 用于射线瞄准(命中点计算)

    [Header("射线表现(光炮/步枪)")]
    public LineRenderer laserLine;  // 命中时短暂显示的光束
    public float laserShowTime = 0.06f;

    // 运行时状态
    private float nextFireTime;
    private int currentMagazine;
    private int currentReserve;
    private bool isReloading;
    private Coroutine laserRoutine;

    private InputSystem_Actions input;

    public bool IsReloading => isReloading;
    public int Magazine => currentMagazine;
    public int Reserve => currentReserve;

    void Awake()
    {
        input = new InputSystem_Actions();
        if (data != null)
        {
            currentMagazine = data.magazineSize;
            currentReserve = data.reserveAmmo;
        }
    }

    void OnEnable()  => input.Player.Enable();
    void OnDisable() => input.Player.Disable();

    void Update()
    {
        if (data == null) return;

        // 换弹(R)
        if (Keyboard.current != null && Keyboard.current[Key.R].wasPressedThisFrame)
            TryReload();

        if (isReloading) return;

        // 是否要开火(按模式判断)
        bool wantFire;
        switch (data.firingMode)
        {
            case FiringMode.Auto: wantFire = input.Player.Fire.IsPressed(); break;
            default:              wantFire = input.Player.Fire.WasPressedThisFrame(); break;
        }

        if (!wantFire) return;
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + 60f / data.roundsPerMinute;

        // 连发一次打多发
        int shots = (data.firingMode == FiringMode.Burst) ? data.bulletsPerBurst : 1;
        for (int i = 0; i < shots; i++)
        {
            if (currentMagazine <= 0)
            {
                TryReload();
                break;
            }
            Fire();
        }
    }

    /// <summary>发射一枪(消耗一颗子弹并按 fireType 结算)。</summary>
    private void Fire()
    {
        currentMagazine--;
        Vector3 dir = CalculateSpread(out Vector3 hitPoint);

        switch (data.fireType)
        {
            case FireType.Hitscan:    FireHitscan(dir, hitPoint); break;
            case FireType.Projectile: FireProjectile(dir); break;
            case FireType.Throwable:  FireThrowable(dir); break;
        }
    }

    // —— 射线(光炮/步枪)：一帧判定 ——
    private void FireHitscan(Vector3 dir, Vector3 aimPoint)
    {
        Ray ray = new Ray(muzzle.position, dir);
        if (Physics.Raycast(ray, out RaycastHit hit, data.range))
        {
            ApplyDamage(hit.collider, hit.point, hit.normal, dir);
            ShowLaser(muzzle.position, hit.point);
        }
        else
        {
            ShowLaser(muzzle.position, muzzle.position + dir * data.range);
        }
    }

    // —— 弹道(手枪/步枪)：生成子弹 ——
    private void FireProjectile(Vector3 dir)
    {
        SpawnProjectile(dir, data.projectileSpeed, Vector3.zero);
    }

    // —— 投掷(飞刀)：受重力 + 翻滚 ——
    private void FireThrowable(Vector3 dir)
    {
        SpawnProjectile(dir, data.projectileSpeed, Vector3.up * data.throwUpSpeed);
    }

    private void SpawnProjectile(Vector3 dir, float speed, Vector3 extraVelocity)
    {
        if (data.bulletPrefab == null)
        {
            Debug.LogWarning("WeaponData.bulletPrefab 未设置，无法发射弹道/投掷物。", this);
            return;
        }

        GameObject go = Instantiate(data.bulletPrefab, muzzle.position, Quaternion.LookRotation(dir));
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = dir.normalized * speed + extraVelocity;
            if (data.throwSpinSpeed > 0f)
                rb.angularVelocity = transform.right * data.throwSpinSpeed;
        }

        // 伤害写入子弹
        var bullet = go.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.damage = data.damage;
            bullet.damageType = data.damageType;
        }
    }

    private void ApplyDamage(Collider col, Vector3 point, Vector3 normal, Vector3 dir)
    {
        var damageable = col.GetComponentInParent<IDamageable>();
        if (damageable == null) return;
        var info = new DamageInfo(data.damage, data.damageType, point, normal, dir.normalized, gameObject);
        damageable.TakeDamage(info);
    }

    /// <summary>根据散布和相机瞄准计算射击方向(准星指哪打哪)。</summary>
    private Vector3 CalculateSpread(out Vector3 hitPoint)
    {
        Ray ray = playerCamera != null
            ? playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0))
            : new Ray(muzzle.position, muzzle.forward);

        Vector3 target;
        if (Physics.Raycast(ray, out RaycastHit hit, data.range))
            target = hit.point;
        else
            target = ray.GetPoint(data.range);

        hitPoint = target;
        Vector3 dir = (target - muzzle.position).normalized;

        if (data.spreadDegrees > 0f)
        {
            dir = Quaternion.Euler(
                Random.Range(-data.spreadDegrees, data.spreadDegrees),
                Random.Range(-data.spreadDegrees, data.spreadDegrees),
                0f) * dir;
        }
        return dir.normalized;
    }

    private void ShowLaser(Vector3 from, Vector3 to)
    {
        if (laserLine == null) return;
        laserLine.SetPosition(0, from);
        laserLine.SetPosition(1, to);
        if (laserRoutine != null) StopCoroutine(laserRoutine);
        laserRoutine = StartCoroutine(HideLaser());
    }

    private IEnumerator HideLaser()
    {
        laserLine.enabled = true;
        yield return new WaitForSeconds(laserShowTime);
        laserLine.enabled = false;
    }

    // —— 弹药/换弹 ——
    private void TryReload()
    {
        if (isReloading) return;
        if (currentMagazine >= data.magazineSize) return;
        if (currentReserve <= 0) return;
        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        yield return new WaitForSeconds(data.reloadTime);

        int need = data.magazineSize - currentMagazine;
        int take = Mathf.Min(need, currentReserve);
        currentMagazine += take;
        currentReserve -= take;

        isReloading = false;
    }
}
