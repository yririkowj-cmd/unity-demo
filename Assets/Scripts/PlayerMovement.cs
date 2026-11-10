using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>移动模式。Ground=常规地面移动，Fly=飞行(自由上下)。</summary>
public enum MoveMode
{
    Ground,
    Fly
}

/// <summary>
/// 玩家移动：地面(走/跑/跳) + 飞行，支持外部速度注入与高速安全。
///
/// 设计要点(为后续载具/喷气背包/高速位移预留)：
///  - 模式化：<see cref="Mode"/> 决定走哪套运动逻辑，新增模式只加分支，不重写类
///  - 参数化：走/冲刺/飞行各自的速度与加速度是独立字段，便于调手感
///  - 外部速度：<see cref="ExternalVelocity"/> 可由载具/击退/风场/弹射器写入，本帧消费后清零
///  - 高速安全：单帧位移超过 <see cref="maxStepDistance"/> 时拆成多段 Move，避免穿薄墙
///  - 方向基准：始终按相机 yaw 计算移动方向(往哪看往哪走)，自由视角下也一致
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerMovement : MonoBehaviour
{
    [Header("模式")]
    public MoveMode startMode = MoveMode.Ground;
    public Key flyToggleKey = Key.F;

    [Header("地面移动")]
    public float speed = 12f;          // 最大水平速度
    public float acceleration = 60f;   // 加速
    public float deceleration = 80f;   // 松手后的减速

    [Header("冲刺")]
    public Key sprintKey = Key.LeftShift;
    public float sprintMultiplier = 1.6f;

    [Header("飞行")]
    public float flySpeed = 14f;
    public float flyAcceleration = 50f;
    public float flyVerticalSpeed = 8f;
    public Key flyUpKey = Key.Space;
    public Key flyDownKey = Key.LeftCtrl;

    [Header("跳跃与重力")]
    public float gravity = -19.62f;    // 约 2 倍重力
    public float jumpHeight = 3f;
    public float coyoteTime = 0.12f;   // 离地后仍可起跳的缓冲时间(手感)

    [Header("地面检测")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("高速安全")]
    [Tooltip("单次 Move 的最大位移；超过则拆成多段，防止高速穿墙/穿地板")]
    public float maxStepDistance = 0.1f;

    private InputSystem_Actions input;
    private CharacterController controller;
    private CameraRig cameraRig;

    private Vector3 horizontalVelocity; // 水平速度(带平滑加减速)
    private float verticalVelocity;     // 垂直速度(y 分量)
    private float coyoteTimer;

    // —— 修正器栈：技能/状态/BUFF 通过它改移动参数，不用改本类 ——
    private class MovementModifier
    {
        public string id;
        public float speed = 1f;
        public float gravity = 1f;
        public float jump = 1f;
        public float endTime; // 0 = 永久
    }

    private readonly List<MovementModifier> modifiers = new List<MovementModifier>();

    /// <summary>速度倍率（多个修正器相乘）。</summary>
    public float SpeedMultiplier { get; private set; } = 1f;
    /// <summary>重力倍率（0 = 无重力，负数 = 反向）。</summary>
    public float GravityMultiplier { get; private set; } = 1f;
    /// <summary>跳跃高度倍率。</summary>
    public float JumpMultiplier { get; private set; } = 1f;

    public MoveMode Mode { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsSprinting { get; private set; }
    public Vector3 HorizontalVelocity => horizontalVelocity;

    /// <summary>
    /// 外部速度注入(载具/击退/风场/弹射器)。
    /// 使用方每帧写入，<see cref="ApplyMovement"/> 消费后清零。
    /// </summary>
    public Vector3 ExternalVelocity { get; set; }

    void Awake()
    {
        input = new InputSystem_Actions();
        controller = GetComponent<CharacterController>();
        cameraRig = GetComponent<CameraRig>();
        Mode = startMode;
    }

    void OnEnable() => input.Player.Enable();
    void OnDisable() => input.Player.Disable();

    void Update()
    {
        UpdateModifiers();
        HandleModeSwitch();

        if (Mode == MoveMode.Fly)
        {
            HandleFlyMove();
        }
        else
        {
            UpdateGrounded();
            HandleJump();
            HandleGroundMove();
        }

        ApplyMovement();
    }

    private void HandleModeSwitch()
    {
        if (Keyboard.current == null || !Keyboard.current[flyToggleKey].wasPressedThisFrame) return;

        Mode = Mode == MoveMode.Ground ? MoveMode.Fly : MoveMode.Ground;
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        coyoteTimer = 0f;
    }

    // —— 地面 ——

    private void UpdateGrounded()
    {
        IsGrounded = groundCheck != null
            ? Physics.CheckSphere(groundCheck.position, groundDistance, groundMask)
            : controller.isGrounded;

        if (IsGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // 贴地时施加一个向下的力保持接触

        if (IsGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;
    }

    private void HandleJump()
    {
        // 修正器可改变重力(0 = 悬浮，负值反向)
        float effectiveGravity = gravity * GravityMultiplier;

        if (input.Player.Jump.triggered && coyoteTimer > 0f && effectiveGravity < 0f)
        {
            float jumpSpeed = Mathf.Sqrt(jumpHeight * JumpMultiplier * -2f * effectiveGravity);
            verticalVelocity = jumpSpeed;
            coyoteTimer = 0f; // 防止空中连跳
        }

        verticalVelocity += effectiveGravity * Time.deltaTime;
    }

    private void HandleGroundMove()
    {
        Vector2 moveInput = input.Player.Move.ReadValue<Vector2>();
        Vector3 move = GetMoveDirection(moveInput);

        IsSprinting = move.sqrMagnitude > 0.01f
                      && Keyboard.current != null
                      && Keyboard.current[sprintKey].isPressed;

        float maxSpeed = speed * (IsSprinting ? sprintMultiplier : 1f) * SpeedMultiplier;
        Vector3 target = move.normalized * maxSpeed;
        float rate = target.sqrMagnitude > 0.01f ? acceleration : deceleration;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, rate * Time.deltaTime);

        IsMoving = horizontalVelocity.sqrMagnitude > 0.01f;
    }

    // —— 飞行 ——

    private void HandleFlyMove()
    {
        Vector2 moveInput = input.Player.Move.ReadValue<Vector2>();
        Vector3 move = GetMoveDirection(moveInput);

        Vector3 target = move.normalized * flySpeed * SpeedMultiplier;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, flyAcceleration * Time.deltaTime);

        float verticalInput = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current[flyUpKey].isPressed) verticalInput += 1f;
            if (Keyboard.current[flyDownKey].isPressed) verticalInput -= 1f;
        }
        verticalVelocity = Mathf.MoveTowards(verticalVelocity, verticalInput * flyVerticalSpeed * SpeedMultiplier,
                                             flyAcceleration * Time.deltaTime);

        IsSprinting = false;
        IsGrounded = false;
        coyoteTimer = 0f;
        IsMoving = horizontalVelocity.sqrMagnitude > 0.01f || Mathf.Abs(verticalVelocity) > 0.01f;
    }

    // —— 公共 ——

    /// <summary>按相机朝向把输入转成世界方向。相机缺失时退回身体朝向。</summary>
    private Vector3 GetMoveDirection(Vector2 moveInput)
    {
        float yaw = cameraRig != null ? cameraRig.CameraYaw : transform.eulerAngles.y;
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 move = (rot * Vector3.right) * moveInput.x + (rot * Vector3.forward) * moveInput.y;
        return move.sqrMagnitude > 1f ? move.normalized : move;
    }

    // —— 修正器栈（技能/状态/载具可调用） ——

    /// <summary>
    /// 添加一个移动修正器。同 id 会覆盖旧的（便于持续刷新）。
    /// </summary>
    /// <param name="id">来源标识，如 "ability.dash"、"buff.slow"</param>
    /// <param name="speedMul">速度倍率</param>
    /// <param name="gravityMul">重力倍率（0 可做悬浮）</param>
    /// <param name="jumpMul">跳跃高度倍率</param>
    /// <param name="duration">持续时间（秒），<=0 表示永久，需手动 Remove</param>
    public void AddModifier(string id, float speedMul = 1f, float gravityMul = 1f, float jumpMul = 1f, float duration = 0f)
    {
        if (string.IsNullOrEmpty(id)) return;

        modifiers.RemoveAll(m => m.id == id);
        modifiers.Add(new MovementModifier
        {
            id = id,
            speed = speedMul,
            gravity = gravityMul,
            jump = jumpMul,
            endTime = duration > 0f ? Time.time + duration : 0f
        });
        RecalculateModifiers();
    }

    public void RemoveModifier(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        modifiers.RemoveAll(m => m.id == id);
        RecalculateModifiers();
    }

    /// <summary>把角色的运动参数应用进来（换角色/换载具时调用）。</summary>
    public void ApplyProfile(CharacterDefinition definition)
    {
        if (definition == null) return;
        MovementProfile p = definition.movement;
        if (p == null) return;

        speed = p.walkSpeed;
        sprintMultiplier = p.sprintMultiplier;
        acceleration = p.acceleration;
        deceleration = p.deceleration;
        flySpeed = p.flySpeed;
        flyAcceleration = p.flyAcceleration;
        flyVerticalSpeed = p.flyVerticalSpeed;
        jumpHeight = p.jumpHeight;
        gravity = p.gravity;
    }

    private void UpdateModifiers()
    {
        if (modifiers.Count == 0) return;
        if (modifiers.RemoveAll(m => m.endTime > 0f && Time.time >= m.endTime) > 0)
            RecalculateModifiers();
    }

    private void RecalculateModifiers()
    {
        float speedMul = 1f, gravityMul = 1f, jumpMul = 1f;
        foreach (MovementModifier m in modifiers)
        {
            speedMul *= m.speed;
            gravityMul *= m.gravity;
            jumpMul *= m.jump;
        }
        SpeedMultiplier = speedMul;
        GravityMultiplier = gravityMul;
        JumpMultiplier = jumpMul;
    }

    private void ApplyMovement()
    {
        Vector3 velocity = horizontalVelocity + ExternalVelocity + Vector3.up * verticalVelocity;
        Vector3 motion = velocity * Time.deltaTime;
        ExternalVelocity = Vector3.zero; // 消费外部速度(单帧有效，避免累积)

        // 高速安全：把一帧位移拆成多段，每段不超过 maxStepDistance
        int steps = Mathf.Clamp(Mathf.CeilToInt(motion.magnitude / Mathf.Max(0.01f, maxStepDistance)), 1, 32);
        Vector3 step = motion / steps;
        for (int i = 0; i < steps; i++)
            controller.Move(step);
    }
}
