using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家输入的**唯一出口**。
///
/// 为什么要有它：之前 `PlayerMovement` 和 `WeaponController` 各自 new 一份
/// `InputSystem_Actions` 并各自 Enable/Disable。目前能跑，但一旦加入
/// 拾取(E)、放下(Q)、技能键、检视(滚轮)，就会出现"同一个按键被两个系统同时消费"、
/// "一个组件 Disable 把所有人的输入关掉"这类问题。
///
/// 规则：任何需要读输入的组件都通过 <see cref="GetOrCreate"/> 取得同一个实例，
/// 不要再 `new InputSystem_Actions()`，也不要自己调用 Enable/Disable。
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlayerInputRouter : MonoBehaviour
{
    /// <summary>最近一次创建的实例（单玩家场景够用；将来多人应改为按玩家查）。</summary>
    public static PlayerInputRouter Instance { get; private set; }

    public InputSystem_Actions Input { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        Input = new InputSystem_Actions();
    }

    void OnEnable()
    {
        if (Input != null) Input.Player.Enable();
    }

    void OnDisable()
    {
        if (Input != null) Input.Player.Disable();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 取得 owner 所在玩家根节点上的输入路由；没有就补一个。
    /// 这样不需要修改 Player 预制体，旧场景也能直接用。
    /// </summary>
    public static PlayerInputRouter GetOrCreate(GameObject owner)
    {
        if (Instance != null) return Instance;

        Transform root = owner.transform.root;
        var router = root.GetComponent<PlayerInputRouter>();
        if (router == null) router = root.gameObject.AddComponent<PlayerInputRouter>();
        return router;
    }
}
