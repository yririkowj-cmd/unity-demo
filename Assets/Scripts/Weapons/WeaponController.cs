using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 武器宿主：处理输入、冷却、弹药与换弹，并把"这一发怎么打出去"委托给 <see cref="IWeaponBehavior"/>。
///
/// 职责边界：
///  - 何时开火、消耗弹药、冷却、换弹 —— 本类
///  - 射线/弹道/投掷/未来的光束/霰弹/钩爪 —— 各自的 IWeaponBehavior 实现
///
/// 下面标注"稳定 API"的成员同时是内置行为与将来 mod 行为的调用面，改动需谨慎。
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

    // 自身引用：射线要跳过自己，否则第三人称时相机在身后，
    // 瞄准射线会先打中自己的胶囊，导致"瞄准点在自己身上 → 朝后开枪 → 自伤"。
    private Transform ownerRoot;
    private Health ownerHealth;
    private readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    public bool IsReloading => isReloading;
    public int Magazine => currentMagazine;
    public int Reserve => currentReserve;

    // —————— 稳定 API：供 WeaponBehavior 使用 ——————

    public WeaponData Data => data;
    public Transform Muzzle => muzzle;
    public Transform OwnerRoot => ownerRoot;

    /// <summary>从枪口沿 direction 找最近的非自身命中。</summary>
    public bool RaycastFromMuzzle(Vector3 direction, out RaycastHit hit)
    {
        return RaycastIgnoringSelf(muzzle.position, direction, data.range, out hit);
    }

    /// <summary>对命中对象结算伤害（内部会再做一次自身判断）。</summary>
    public void Damage(Collider col, Vector3 point, Vector3 normal, Vector3 direction)
    {
        ApplyDamage(col, point, normal, direction);
    }

    /// <summary>显示一条短暂激光。</summary>
    public void ShowLaser(Vector3 from, Vector3 to)
    {
        if (laserLine == null) return;
        laserLine.SetPosition(0, from);
        laserLine.SetPosition(1, to);
        if (laserRoutine != null) StopCoroutine(laserRoutine);
        laserRoutine = StartCoroutine(HideLaser());
    }

    /// <summary>生成抛射物（弹道/投掷共用）。</summary>
    public void SpawnProjectile(Vector3 direction, bool throwable)
    {
        if (data.bulletPrefab == null)
        {
            Debug.LogWarning("WeaponData.bulletPrefab 未设置，无法发射弹道/投掷物。", this);
            return;
        }

        Vector3 extraVelocity = throwable ? Vector3.up * data.throwUpSpeed : Vector3.zero;
        float speed = data.projectileSpeed;

        GameObject go = Instantiate(data.bulletPrefab, muzzle.position, Quaternion.LookRotation(direction));
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * speed + extraVelocity;
            if (data.throwSpinSpeed > 0f)
                rb.angularVelocity = transform.right * data.throwSpinSpeed;
        }

        var bullet = go.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.damage = data.damage;
            bullet.damageType = data.damageType;
            bullet.SetOwner(ownerRoot);
        }
    }

    // ————————————————————————————————

    void Awake()
    {
        // 统一输入出口，避免每个组件各自 new 一份（见 PlayerInputRouter）
        input = PlayerInputRouter.GetOrCreate(gameObject).Input;
        ownerRoot = transform.root;
        ownerHealth = GetComponentInParent<Health>();
        if (data != null)
        {
            currentMagazine = data.magazineSize;
            currentReserve = data.reserveAmmo;
        }
    }

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

    /// <summary>发射一枪：扣弹、算方向、交给行为实现。</summary>
    private void Fire()
    {
        currentMagazine--;
        Vector3 dir = CalculateSpread(out _);

        IWeaponBehavior behavior = WeaponBehaviorRegistry.Resolve(data.behaviorId, data.fireType);
        if (behavior == null)
        {
            Debug.LogError("没有可用的武器行为，开火被忽略。", this);
            return;
        }

        behavior.Fire(this, dir);
    }

    /// <summary>根据散布和相机瞄准计算射击方向(准星指哪打哪)。</summary>
    private Vector3 CalculateSpread(out Vector3 hitPoint)
    {
        Ray camRay = playerCamera != null
            ? playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0))
            : new Ray(muzzle.position, muzzle.forward);

        // 关键：忽略自身命中，否则第三人称下瞄准点会落在自己身上
        Vector3 target = RaycastIgnoringSelf(camRay.origin, camRay.direction, data.range, out RaycastHit hit)
            ? hit.point
            : camRay.GetPoint(data.range);

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

    private void ApplyDamage(Collider col, Vector3 point, Vector3 normal, Vector3 dir)
    {
        if (col == null || IsSelf(col)) return; // 双保险：绝不伤害自己
        var damageable = col.GetComponentInParent<IDamageable>();
        if (damageable == null) return;
        var info = new DamageInfo(data.damage, data.damageType, point, normal, dir.normalized, gameObject, data);
        damageable.TakeDamage(info);
    }

    /// <summary>判断碰撞体是否属于自己(玩家/持枪者)。</summary>
    private bool IsSelf(Collider col)
    {
        if (col == null) return false;
        if (ownerRoot != null && col.transform.root == ownerRoot) return true;
        if (ownerHealth != null && col.GetComponentInParent<Health>() == ownerHealth) return true;
        return false;
    }

    /// <summary>
    /// 沿射线找最近的"非自己"命中。第三人称相机在身后，视线会先穿过自己的胶囊，
    /// 必须跳过自身才能得到正确的瞄准点/命中目标。
    /// </summary>
    private bool RaycastIgnoringSelf(Vector3 origin, Vector3 dir, float maxDistance, out RaycastHit closest)
    {
        closest = default;
        int count = Physics.RaycastNonAlloc(new Ray(origin, dir), hitBuffer, maxDistance);
        float bestDistance = float.MaxValue;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit h = hitBuffer[i];
            if (h.collider == null || IsSelf(h.collider)) continue;
            if (h.distance < bestDistance)
            {
                bestDistance = h.distance;
                closest = h;
                found = true;
            }
        }
        return found;
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
